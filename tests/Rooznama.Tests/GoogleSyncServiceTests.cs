using System.Net;
using System.Text.Json;
using Rooznama.Services;
using Xunit;

namespace Rooznama.Tests;

/// <summary>A tiny in-memory Google Calendar: ids and etags, patching, deletion and the expansion of yearly rules into occurrences.</summary>
sealed class FakeGoogle : IGoogleCalendarApi
{
    public readonly Dictionary<string, GoogleEvent> Events = new();
    public readonly List<string> Calls = new();
    public Func<string, Exception?>? Fail;
    int _next = 1, _version = 1;

    static GoogleEvent Copy(GoogleEvent g) => JsonSerializer.Deserialize<GoogleEvent>(JsonSerializer.Serialize(g))!;
    static string Now() => DateTime.UtcNow.ToString("o");

    void Check(string call)
    {
        Calls.Add(call);
        if (Fail?.Invoke(call) is { } ex) throw ex;
    }

    static DateTime Parse(GoogleEventTime t) => t.Date is { } d ? DateTime.Parse(d) : DateTime.Parse(t.DateTime![..19]);

    public Task<IReadOnlyList<GoogleEvent>> ListAsync(DateTimeOffset min, DateTimeOffset max, CancellationToken ct = default)
    {
        Check("list");
        var result = new List<GoogleEvent>();
        foreach (var g in Events.Values)
        {
            var (s, e) = (Parse(g.Start!), Parse(g.End!));
            if (g.Recurrence is { Count: > 0 })
            {
                for (int y = min.Year; y <= max.Year; y++)
                {
                    int shift = y - s.Year;
                    var (os, oe) = (s.AddYears(shift), e.AddYears(shift));
                    if (shift < 0 || os >= max.UtcDateTime || oe <= min.UtcDateTime) continue;
                    var occurrence = Copy(g);
                    occurrence.Id = g.Id + "_" + os.ToString("yyyyMMdd");
                    occurrence.RecurringEventId = g.Id;
                    occurrence.Recurrence = null;
                    if (g.Start!.Date is not null) { occurrence.Start = new GoogleEventTime { Date = os.ToString("yyyy-MM-dd") }; occurrence.End = new GoogleEventTime { Date = oe.ToString("yyyy-MM-dd") }; }
                    else { occurrence.Start = new GoogleEventTime { DateTime = os.ToString("yyyy-MM-ddTHH:mm:ss"), TimeZone = g.Start.TimeZone }; occurrence.End = new GoogleEventTime { DateTime = oe.ToString("yyyy-MM-ddTHH:mm:ss"), TimeZone = g.Start.TimeZone }; }
                    result.Add(occurrence);
                }
            }
            else if (s < max.UtcDateTime && e > min.UtcDateTime) result.Add(Copy(g));
        }
        return Task.FromResult<IReadOnlyList<GoogleEvent>>(result);
    }

    public Task<GoogleEvent> InsertAsync(GoogleEvent e, CancellationToken ct = default)
    {
        Check("insert:" + e.Summary);
        var c = Copy(e);
        c.Id = "g" + _next++;
        c.ETag = "v" + _version++;
        c.Status = "confirmed";
        c.Updated = Now();
        if (c.Recurrence is { Count: 0 }) c.Recurrence = null;
        Events[c.Id] = c;
        return Task.FromResult(Copy(c));
    }

    public Task<GoogleEvent> PatchAsync(string id, GoogleEvent e, CancellationToken ct = default)
    {
        Check("patch:" + id);
        if (!Events.TryGetValue(id, out var cur)) throw new GoogleApiException(HttpStatusCode.NotFound, "gone");
        if (e.Summary is not null) cur.Summary = e.Summary;
        if (e.Description is not null) cur.Description = e.Description;
        if (e.Location is not null) cur.Location = e.Location;
        if (e.Start is not null) cur.Start = e.Start;
        if (e.End is not null) cur.End = e.End;
        if (e.Recurrence is not null) cur.Recurrence = e.Recurrence.Count == 0 ? null : e.Recurrence;
        if (e.ExtendedProperties is not null) cur.ExtendedProperties = e.ExtendedProperties;
        cur.ETag = "v" + _version++;
        cur.Updated = Now();
        return Task.FromResult(Copy(cur));
    }

    public Task DeleteAsync(string id, CancellationToken ct = default)
    {
        Check("delete:" + id);
        Events.Remove(id);
        return Task.CompletedTask;
    }

    public int Count(string prefix) => Calls.Count(c => c.StartsWith(prefix, StringComparison.Ordinal));

    /// <summary>Something made at Google by someone else (no Rooznama marker).</summary>
    public string AddForeign(string title, string day)
    {
        var g = new GoogleEvent { Summary = title, Start = new GoogleEventTime { Date = day }, End = new GoogleEventTime { Date = DateTime.Parse(day).AddDays(1).ToString("yyyy-MM-dd") } };
        return InsertAsync(g).Result.Id!;
    }
}

public sealed class GoogleSyncServiceTests : IDisposable
{
    readonly string _dir = Path.Combine(Path.GetTempPath(), "rooznama-gsync-" + Guid.NewGuid().ToString("N"));
    readonly PersonalEventStore _store;
    readonly FakeGoogle _api = new();
    readonly DateTime _today = new(2026, 11, 5);

    public GoogleSyncServiceTests() => _store = new PersonalEventStore(Path.Combine(_dir, "personal-events.json"));

    public void Dispose() { try { Directory.Delete(_dir, true); } catch { } }

    GoogleSyncService Service() => new(_api, _store, _ => null, () => _today, zone: TimeZoneInfo.Utc);

    string AddLocal(string title, DateTime day, Action<PersonalEvent>? tweak = null)
    {
        var e = new PersonalEvent { Title = title, IsAllDay = true, Start = day.Date, End = day.Date.AddDays(1), TimeZone = "Asia/Tehran" };
        tweak?.Invoke(e);
        Assert.True(_store.Save(e));
        return e.Id;
    }

    PersonalEvent Stored(string id) => _store.Snapshot().Single(e => e.Id == id);

    // ---------- sending ----------
    [Fact]
    public async Task A_new_local_event_is_inserted_and_recorded_as_synced()
    {
        var id = AddLocal("تولد", new DateTime(2026, 11, 10));
        var r = await Service().SyncAsync();

        Assert.Equal(1, r.Uploaded);
        var remote = Assert.Single(_api.Events.Values);
        Assert.Equal("تولد", remote.Summary);
        Assert.Equal(id, remote.RooznamaId);

        var local = Stored(id);
        Assert.Equal(remote.Id, local.ExternalId);
        Assert.Equal(remote.ETag, local.ETag);
        Assert.Equal(EventSyncState.Synced, local.SyncState);
        Assert.Equal("primary", local.CalendarId);
    }

    [Fact]
    public async Task A_second_sync_with_nothing_new_does_nothing()
    {
        AddLocal("الف", new DateTime(2026, 11, 10));
        await Service().SyncAsync();
        _api.Calls.Clear();

        var r = await Service().SyncAsync();

        Assert.False(r.AnythingChanged);
        Assert.Equal(0, r.Failed);
        Assert.Equal(new[] { "list", "list" }, _api.Calls);   // only the two reads
    }

    [Fact]
    public async Task An_edit_is_patched_not_inserted_again()
    {
        var id = AddLocal("قدیمی", new DateTime(2026, 11, 10));
        await Service().SyncAsync();

        var edit = _store.Get(id)!;
        edit.Title = "جدید";
        _store.Save(edit);
        Assert.Equal(EventSyncState.PendingUpload, Stored(id).SyncState);

        await Service().SyncAsync();

        Assert.Single(_api.Events);
        Assert.Equal("جدید", _api.Events.Values.Single().Summary);
        Assert.Equal(1, _api.Count("insert:"));
        Assert.Equal(EventSyncState.Synced, Stored(id).SyncState);
    }

    [Fact]
    public async Task A_deleted_event_is_removed_at_Google_and_forgotten_here()
    {
        var id = AddLocal("الف", new DateTime(2026, 11, 10));
        await Service().SyncAsync();
        Assert.True(_store.Delete(id));
        Assert.Equal(EventSyncState.PendingDelete, Stored(id).SyncState);   // a tombstone waits for the sync

        var r = await Service().SyncAsync();

        Assert.Equal(1, r.DeletedRemotely);
        Assert.Empty(_api.Events);
        Assert.Empty(_store.Snapshot());
    }

    [Fact]
    public async Task A_local_edit_wins_over_a_remote_edit_made_since_the_last_sync()
    {
        var id = AddLocal("اصلی", new DateTime(2026, 11, 10));
        await Service().SyncAsync();

        _api.Events.Values.Single().Summary = "ویرایش در گوگل";
        _api.Events.Values.Single().ETag = "remote-edit";
        var edit = _store.Get(id)!;
        edit.Title = "ویرایش اینجا";
        _store.Save(edit);

        await Service().SyncAsync();

        Assert.Equal("ویرایش اینجا", _api.Events.Values.Single().Summary);
        Assert.Equal("ویرایش اینجا", Stored(id).Title);
    }

    [Fact]
    public async Task An_event_that_exists_at_Google_from_a_half_finished_upload_is_adopted_not_duplicated()
    {
        var id = AddLocal("الف", new DateTime(2026, 11, 10));
        // the insert reached Google but the answer was lost, so the local event never learned its remote id
        _api.InsertAsync(GoogleEventMapper.ToGoogle(_store.Get(id)!)).Wait();

        var r = await Service().SyncAsync();

        Assert.Single(_api.Events);
        Assert.Equal(1, r.Uploaded);
        Assert.Equal(_api.Events.Keys.Single(), Stored(id).ExternalId);
        Assert.Equal(1, _api.Count("insert:"));   // only the simulated one
    }

    // ---------- repetition ----------
    [Fact]
    public async Task A_Gregorian_yearly_event_is_one_event_with_a_rule_and_its_occurrences_are_not_imported_back()
    {
        var id = AddLocal("سالگرد", new DateTime(2026, 11, 20), e =>
        {
            e.Recurrence = EventRecurrence.YearlyGregorian;
            e.RecurrenceMonth = 11;
            e.RecurrenceDay = 20;
        });

        await Service().SyncAsync();
        await Service().SyncAsync();

        var master = Assert.Single(_api.Events.Values);
        Assert.Equal(new[] { "RRULE:FREQ=YEARLY" }, master.Recurrence);
        Assert.Equal(master.Id, Stored(id).ExternalId);
        Assert.Single(_store.Snapshot());   // the occurrences that Google lists were recognised as ours
    }

    [Fact]
    public async Task A_Jalali_yearly_event_becomes_one_Google_event_per_occurrence()
    {
        var id = AddLocal("نوروز من", new DateTime(2026, 3, 21), e =>
        {
            e.Recurrence = EventRecurrence.YearlyJalali;
            e.RecurrenceMonth = 1;
            e.RecurrenceDay = 1;
        });

        var r = await Service().SyncAsync();

        var local = Stored(id);
        Assert.Equal(1, r.Uploaded);
        Assert.Null(local.ExternalId);
        Assert.NotNull(local.RemoteInstances);
        Assert.Contains("20270321", local.RemoteInstances!.Keys);   // 1 Farvardin 1406
        Assert.Equal(local.RemoteInstances.Count, _api.Events.Count);
        Assert.All(_api.Events.Values, g => Assert.Null(g.Recurrence));
        Assert.Equal(1, Stored(id).RemoteInstances!.Count(kv => kv.Key == "20270321"));

        // nothing new: no duplicates, nothing imported back
        _api.Calls.Clear();
        await Service().SyncAsync();
        Assert.Equal(0, _api.Count("insert:"));
        Assert.Single(_store.Snapshot());
    }

    [Fact]
    public async Task Editing_a_Jalali_yearly_event_patches_each_occurrence_and_deleting_it_removes_them_all()
    {
        var id = AddLocal("الف", new DateTime(2026, 3, 21), e =>
        {
            e.Recurrence = EventRecurrence.YearlyJalali;
            e.RecurrenceMonth = 1;
            e.RecurrenceDay = 1;
        });
        await Service().SyncAsync();
        int count = _api.Events.Count;
        Assert.True(count >= 2);

        var edit = _store.Get(id)!;
        edit.Title = "ب";
        _store.Save(edit);
        _api.Calls.Clear();
        await Service().SyncAsync();
        Assert.Equal(count, _api.Count("patch:"));
        Assert.Equal(0, _api.Count("insert:"));
        Assert.All(_api.Events.Values, g => Assert.Equal("ب", g.Summary));

        _store.Delete(id);
        await Service().SyncAsync();
        Assert.Empty(_api.Events);
        Assert.Empty(_store.Snapshot());
    }

    // ---------- reading back ----------
    [Fact]
    public async Task An_event_made_at_Google_is_imported_once()
    {
        var gid = _api.AddForeign("جلسه گوگل", "2026-11-12");

        var first = await Service().SyncAsync();
        var second = await Service().SyncAsync();

        Assert.Equal(1, first.Imported);
        Assert.Equal(0, second.Imported);
        var e = Assert.Single(_store.Snapshot());
        Assert.Equal("google", e.Origin);
        Assert.Equal(gid, e.ExternalId);
        Assert.Equal("جلسه گوگل", e.Title);
        Assert.Equal(new DateTime(2026, 11, 12), e.Start);
        Assert.Equal(EventSyncState.Synced, e.SyncState);
    }

    [Fact]
    public async Task An_edit_made_at_Google_is_applied_here()
    {
        var gid = _api.AddForeign("قدیمی", "2026-11-12");
        await Service().SyncAsync();

        _api.Events[gid].Summary = "جدید";
        _api.Events[gid].ETag = "changed";
        var r = await Service().SyncAsync();

        Assert.Equal(1, r.UpdatedLocally);
        Assert.Equal("جدید", Assert.Single(_store.Snapshot()).Title);
    }

    [Fact]
    public async Task Editing_an_imported_event_here_is_sent_back_without_a_Rooznama_marker()
    {
        var gid = _api.AddForeign("از گوگل", "2026-11-12");
        await Service().SyncAsync();

        var imported = _store.Snapshot().Single();
        var edit = _store.Get(imported.Id)!;
        edit.Title = "ویرایش‌شده";
        _store.Save(edit);
        await Service().SyncAsync();

        Assert.Equal("ویرایش‌شده", _api.Events[gid].Summary);
        Assert.Null(_api.Events[gid].RooznamaId);
        Assert.Single(_store.Snapshot());
    }

    [Fact]
    public async Task An_event_deleted_at_Google_is_removed_here()
    {
        var gid = _api.AddForeign("الف", "2026-11-12");
        await Service().SyncAsync();
        _api.Events.Remove(gid);

        var r = await Service().SyncAsync();

        Assert.Equal(1, r.RemovedLocally);
        Assert.Empty(_store.Snapshot());
    }

    [Fact]
    public async Task An_event_that_looks_like_it_was_deleted_for_everyone_is_not_mass_removed()
    {
        for (int i = 0; i < 8; i++) _api.AddForeign("رویداد " + i, "2026-11-" + (10 + i));
        await Service().SyncAsync();
        Assert.Equal(8, _store.Snapshot().Count);

        _api.Events.Clear();   // the wrong account or a broken answer, not eight deletions
        var r = await Service().SyncAsync();

        Assert.True(r.SkippedMassDeletion);
        Assert.Equal(0, r.RemovedLocally);
        Assert.Equal(8, _store.Snapshot().Count);
    }

    [Fact]
    public async Task An_event_that_another_computer_marked_is_imported_here()
    {
        var g = new GoogleEvent
        {
            Summary = "از کامپیوتر دیگر", Start = new GoogleEventTime { Date = "2026-11-14" }, End = new GoogleEventTime { Date = "2026-11-15" },
            ExtendedProperties = new GoogleExtendedProperties { Private = new Dictionary<string, string> { [GoogleEventMapper.RooznamaIdKey] = "id-from-another-pc" } }
        };
        await _api.InsertAsync(g);

        var r = await Service().SyncAsync();

        Assert.Equal(1, r.Imported);
        Assert.Equal("از کامپیوتر دیگر", Assert.Single(_store.Snapshot()).Title);
    }

    [Fact]
    public async Task Events_outside_the_window_are_left_alone()
    {
        _api.AddForeign("دور", "2031-01-01");
        var local = AddLocal("قدیمی", new DateTime(2020, 1, 1));
        var r = await Service().SyncAsync();

        Assert.Equal(0, r.Imported);   // five years ahead is not read
        Assert.Equal(1, r.Uploaded);   // but what the person made is still sent
        Assert.Equal(EventSyncState.Synced, Stored(local).SyncState);
    }

    // ---------- failures ----------
    [Fact]
    public async Task A_failing_event_stays_pending_and_the_others_still_go()
    {
        var bad = AddLocal("بد", new DateTime(2026, 11, 10));
        var good = AddLocal("خوب", new DateTime(2026, 11, 11));
        _api.Fail = call => call == "insert:بد" ? new GoogleApiException(HttpStatusCode.BadRequest, "no") : null;

        var r = await Service().SyncAsync();

        Assert.Equal(1, r.Failed);
        Assert.NotNull(r.FirstError);
        Assert.Equal(1, r.Uploaded);
        Assert.Equal(EventSyncState.LocalOnly, Stored(bad).SyncState);
        Assert.Equal(EventSyncState.Synced, Stored(good).SyncState);

        _api.Fail = null;
        var again = await Service().SyncAsync();
        Assert.Equal(0, again.Failed);
        Assert.Equal(EventSyncState.Synced, Stored(bad).SyncState);
    }

    [Fact]
    public async Task A_failed_deletion_keeps_the_tombstone_for_the_next_run()
    {
        var id = AddLocal("الف", new DateTime(2026, 11, 10));
        await Service().SyncAsync();
        _store.Delete(id);
        _api.Fail = call => call.StartsWith("delete:", StringComparison.Ordinal) ? new GoogleApiException(HttpStatusCode.InternalServerError, "x") : null;

        var r = await Service().SyncAsync();
        Assert.Equal(1, r.Failed);
        Assert.Single(_store.Snapshot());

        _api.Fail = null;
        await Service().SyncAsync();
        Assert.Empty(_store.Snapshot());
        Assert.Empty(_api.Events);
    }

    [Fact]
    public async Task A_sign_in_problem_stops_the_whole_sync()
    {
        AddLocal("الف", new DateTime(2026, 11, 10));
        _api.Fail = _ => new GoogleAuthException("withdrawn");
        await Assert.ThrowsAsync<GoogleAuthException>(() => Service().SyncAsync());
    }

    [Fact]
    public async Task A_network_error_while_reading_stops_before_anything_is_changed()
    {
        AddLocal("الف", new DateTime(2026, 11, 10));
        _api.Fail = call => call == "list" ? new HttpRequestException("offline") : null;
        await Assert.ThrowsAsync<HttpRequestException>(() => Service().SyncAsync());
        Assert.Equal(EventSyncState.LocalOnly, _store.Snapshot().Single().SyncState);
        Assert.Equal(0, _api.Count("insert:"));
    }

    [Fact]
    public async Task The_store_is_told_once_when_a_sync_changed_something()
    {
        _api.AddForeign("الف", "2026-11-12");
        int changed = 0, edited = 0;
        _store.Changed += () => changed++;
        _store.Edited += () => edited++;

        await Service().SyncAsync();

        Assert.Equal(1, changed);
        Assert.Equal(0, edited);   // a sync is not an edit by the person
    }
}
