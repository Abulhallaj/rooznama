using Rooznama.Services;
using Xunit;

namespace Rooznama.Tests;

public sealed class PersonalEventStoreTests : IDisposable
{
    readonly string _dir = Path.Combine(Path.GetTempPath(), "rooznama-events-" + Guid.NewGuid().ToString("N"));
    readonly List<string> _log = new();

    string FilePath => Path.Combine(_dir, "personal-events.json");
    PersonalEventStore NewStore() => new(FilePath, _log.Add);

    public void Dispose() { try { Directory.Delete(_dir, true); } catch { } }

    static readonly Func<DateTime, HijriDate?> NoHijri = _ => null;

    static PersonalEvent AllDay(string title, DateTime day) =>
        new() { Title = title, IsAllDay = true, Start = day.Date, End = day.Date.AddDays(1) };

    static PersonalEvent Yearly(string title, DateTime start, EventRecurrence kind, Func<DateTime, HijriDate?>? hijri = null)
    {
        var f = EventRecurrenceFields.Of(start, kind, hijri ?? NoHijri)!.Value;
        var e = AllDay(title, start);
        e.Recurrence = kind;
        e.RecurrenceMonth = f.Month;
        e.RecurrenceDay = f.Day;
        return e;
    }

    // ---------- saving and loading ----------
    [Fact]
    public void Saved_events_survive_a_restart_with_every_field()
    {
        var store = NewStore();
        var e = new PersonalEvent
        {
            Title = "جلسه", Description = "اتاق ۲", Location = "دفتر", IsAllDay = false,
            Start = new DateTime(2026, 11, 5, 9, 30, 0, DateTimeKind.Local), End = new DateTime(2026, 11, 5, 10, 30, 0)
        };
        Assert.True(store.Save(e));

        var back = Assert.Single(NewStore().All());
        Assert.Equal(e.Id, back.Id);
        Assert.Equal("جلسه", back.Title);
        Assert.Equal("اتاق ۲", back.Description);
        Assert.Equal("دفتر", back.Location);
        Assert.False(back.IsAllDay);
        Assert.Equal(new DateTime(2026, 11, 5, 9, 30, 0), back.Start);
        Assert.Equal(DateTimeKind.Unspecified, back.Start.Kind);   // wall-clock time, no UTC offset in the file
        Assert.Equal(EventSyncState.LocalOnly, back.SyncState);
        Assert.Null(back.ExternalId);
        Assert.Contains("\"syncState\": \"LocalOnly\"", File.ReadAllText(FilePath));
    }

    [Fact]
    public void Saving_again_replaces_the_event_and_keeps_its_creation_time_and_remote_link()
    {
        var store = NewStore();
        var e = AllDay("الف", new DateTime(2026, 11, 5));
        store.Save(e);
        var created = store.Get(e.Id)!.CreatedUtc;

        var edit = store.Get(e.Id)!;
        edit.Title = "ب";
        edit.ExternalId = "google-123";
        Assert.True(store.Save(edit));

        var again = store.Get(e.Id)!;
        Assert.Single(store.All());
        Assert.Equal("ب", again.Title);
        Assert.Equal(created, again.CreatedUtc);
        Assert.Equal("google-123", again.ExternalId);
        Assert.Equal(EventSyncState.PendingUpload, again.SyncState);   // it exists remotely, so the change has to be sent

        var edit2 = store.Get(e.Id)!;
        edit2.ExternalId = null;                                       // a save that does not carry the id must not lose it
        store.Save(edit2);
        Assert.Equal("google-123", store.Get(e.Id)!.ExternalId);
    }

    [Fact]
    public void Get_returns_a_copy_so_cancelling_an_edit_changes_nothing()
    {
        var store = NewStore();
        var e = AllDay("الف", new DateTime(2026, 11, 5));
        store.Save(e);

        var copy = store.Get(e.Id)!;
        copy.Title = "تغییر نکرده";
        Assert.Equal("الف", store.Get(e.Id)!.Title);
    }

    [Fact]
    public void Changed_is_raised_after_save_and_delete()
    {
        var store = NewStore();
        int n = 0;
        store.Changed += () => n++;
        var e = AllDay("الف", new DateTime(2026, 11, 5));
        store.Save(e);
        store.Delete(e.Id);
        Assert.Equal(2, n);
    }

    // ---------- deleting ----------
    [Fact]
    public void An_event_that_was_never_synced_is_removed_completely()
    {
        var store = NewStore();
        var e = AllDay("الف", new DateTime(2026, 11, 5));
        store.Save(e);
        Assert.True(store.Delete(e.Id));

        Assert.Empty(store.All());
        Assert.Empty(NewStore().All());
        Assert.DoesNotContain(e.Id, File.ReadAllText(FilePath));
        Assert.False(store.Delete(e.Id));   // already gone
    }

    [Fact]
    public void An_event_that_exists_remotely_stays_as_a_tombstone_until_the_deletion_is_sent()
    {
        var store = NewStore();
        var e = AllDay("الف", new DateTime(2026, 11, 5));
        e.ExternalId = "google-123";
        store.Save(e);
        Assert.True(store.Delete(e.Id));

        Assert.Empty(store.All());
        Assert.Empty(store.GetInstances(new DateTime(2026, 11, 5), new DateTime(2026, 11, 6), NoHijri));
        var text = File.ReadAllText(FilePath);
        Assert.Contains("\"isDeleted\": true", text);
        Assert.Contains("PendingDelete", text);
        Assert.Empty(NewStore().All());
    }

    // ---------- instances ----------
    [Fact]
    public void An_all_day_event_belongs_to_its_day_only()
    {
        var store = NewStore();
        store.Save(AllDay("الف", new DateTime(2026, 11, 5)));

        Assert.Single(store.GetInstances(new DateTime(2026, 11, 5), new DateTime(2026, 11, 6), NoHijri));
        Assert.Empty(store.GetInstances(new DateTime(2026, 11, 4), new DateTime(2026, 11, 5), NoHijri));
        Assert.Empty(store.GetInstances(new DateTime(2026, 11, 6), new DateTime(2026, 11, 7), NoHijri));
        Assert.Single(store.GetInstances(new DateTime(2026, 11, 1), new DateTime(2026, 12, 1), NoHijri));
    }

    [Fact]
    public void A_timed_event_overlaps_by_time_and_the_end_is_exclusive()
    {
        var store = NewStore();
        store.Save(new PersonalEvent { Title = "ب", IsAllDay = false, Start = new DateTime(2026, 11, 5, 9, 0, 0), End = new DateTime(2026, 11, 5, 10, 0, 0) });

        Assert.Single(store.GetInstances(new DateTime(2026, 11, 5), new DateTime(2026, 11, 6), NoHijri));
        Assert.Single(store.GetInstances(new DateTime(2026, 11, 5, 9, 30, 0), new DateTime(2026, 11, 5, 9, 45, 0), NoHijri));
        Assert.Empty(store.GetInstances(new DateTime(2026, 11, 5, 10, 0, 0), new DateTime(2026, 11, 5, 11, 0, 0), NoHijri));
    }

    [Fact]
    public void Instances_are_sorted_by_start()
    {
        var store = NewStore();
        store.Save(new PersonalEvent { Title = "دوم", IsAllDay = false, Start = new DateTime(2026, 11, 5, 15, 0, 0), End = new DateTime(2026, 11, 5, 16, 0, 0) });
        store.Save(new PersonalEvent { Title = "اول", IsAllDay = false, Start = new DateTime(2026, 11, 5, 8, 0, 0), End = new DateTime(2026, 11, 5, 9, 0, 0) });

        var titles = store.GetInstances(new DateTime(2026, 11, 5), new DateTime(2026, 11, 6), NoHijri).Select(i => i.Source.Title);
        Assert.Equal(new[] { "اول", "دوم" }, titles);
    }

    // ---------- yearly repetition in each calendar ----------
    [Fact]
    public void A_Gregorian_yearly_event_repeats_on_the_same_Gregorian_day_and_maps_to_a_Google_RRULE()
    {
        var store = NewStore();
        var e = Yearly("تولد", new DateTime(2026, 3, 8), EventRecurrence.YearlyGregorian);
        store.Save(e);

        var hit = Assert.Single(store.GetInstances(new DateTime(2028, 3, 8), new DateTime(2028, 3, 9), NoHijri));
        Assert.Equal(new DateTime(2028, 3, 8), hit.Start);
        Assert.Equal(new DateTime(2028, 3, 9), hit.End);
        Assert.Empty(store.GetInstances(new DateTime(2028, 3, 9), new DateTime(2028, 3, 10), NoHijri));
        Assert.Empty(store.GetInstances(new DateTime(2025, 3, 8), new DateTime(2025, 3, 9), NoHijri));   // not before the first one
        Assert.Equal("RRULE:FREQ=YEARLY", e.Rrule);
    }

    [Fact]
    public void A_Jalali_yearly_event_follows_the_Jalali_date_and_has_no_RRULE()
    {
        var store = NewStore();
        var e = Yearly("نوروز من", new DateTime(2026, 3, 21), EventRecurrence.YearlyJalali);   // 1 Farvardin 1405
        store.Save(e);

        Assert.Single(store.GetInstances(new DateTime(2027, 3, 21), new DateTime(2027, 3, 22), NoHijri));   // 1 Farvardin 1406
        Assert.Empty(store.GetInstances(new DateTime(2027, 3, 22), new DateTime(2027, 3, 23), NoHijri));
        Assert.Null(e.Rrule);
    }

    [Fact]
    public void A_Hijri_yearly_event_follows_the_Hijri_date_it_was_saved_with()
    {
        // a fake Hijri calendar: only these two days are 10/1, everything else is 5/5
        static HijriDate? Fake(DateTime d) =>
            d.Date == new DateTime(2026, 6, 25) || d.Date == new DateTime(2027, 6, 14) ? new HijriDate(1448, 1, 10) : new HijriDate(1448, 5, 5);

        var store = NewStore();
        var e = Yearly("عاشورا", new DateTime(2026, 6, 25), EventRecurrence.YearlyHijri, Fake);
        Assert.Equal((1, 10), (e.RecurrenceMonth, e.RecurrenceDay));
        store.Save(e);

        Assert.Single(store.GetInstances(new DateTime(2027, 6, 14), new DateTime(2027, 6, 15), Fake));
        Assert.Empty(store.GetInstances(new DateTime(2027, 6, 25), new DateTime(2027, 6, 26), Fake));
        Assert.Empty(store.GetInstances(new DateTime(2026, 6, 24), new DateTime(2026, 6, 25), Fake));
    }

    [Fact]
    public void A_timed_yearly_event_keeps_its_time_and_duration()
    {
        var store = NewStore();
        var e = new PersonalEvent { Title = "ج", IsAllDay = false, Start = new DateTime(2026, 3, 8, 18, 0, 0), End = new DateTime(2026, 3, 8, 19, 30, 0),
                                    Recurrence = EventRecurrence.YearlyGregorian, RecurrenceMonth = 3, RecurrenceDay = 8 };
        store.Save(e);

        var hit = Assert.Single(store.GetInstances(new DateTime(2027, 3, 8), new DateTime(2027, 3, 9), NoHijri));
        Assert.Equal(new DateTime(2027, 3, 8, 18, 0, 0), hit.Start);
        Assert.Equal(new DateTime(2027, 3, 8, 19, 30, 0), hit.End);
    }

    [Fact]
    public void Recurrence_fields_are_null_when_the_day_has_no_date_in_that_calendar()
    {
        Assert.Null(EventRecurrenceFields.Of(new DateTime(2026, 6, 25), EventRecurrence.YearlyHijri, NoHijri));
        Assert.Null(EventRecurrenceFields.Of(new DateTime(2026, 6, 25), EventRecurrence.None, NoHijri));
        var jalali = EventRecurrenceFields.Of(new DateTime(2026, 6, 25), EventRecurrence.YearlyJalali, NoHijri)!.Value;   // 4 Tir 1405
        Assert.Equal((4, 4), (jalali.Month, jalali.Day));
    }

    // ---------- the file ----------
    [Fact]
    public void A_damaged_file_is_kept_as_bad_and_the_store_starts_fresh()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(FilePath, "{ this is not json");

        var store = NewStore();
        Assert.Empty(store.All());
        Assert.True(File.Exists(FilePath + ".bad"));
        Assert.True(store.Save(AllDay("الف", new DateTime(2026, 11, 5))));
        Assert.Single(NewStore().All());
    }

    [Fact]
    public void A_file_from_a_newer_version_is_never_overwritten()
    {
        Directory.CreateDirectory(_dir);
        const string newer = """{ "schemaVersion": 99, "events": [ { "id": "x", "title": "future" } ] }""";
        File.WriteAllText(FilePath, newer);

        var store = NewStore();
        Assert.False(store.CanWrite);
        Assert.Empty(store.All());
        Assert.False(store.Save(AllDay("الف", new DateTime(2026, 11, 5))));
        Assert.False(store.Delete("x"));
        Assert.Equal(newer, File.ReadAllText(FilePath));
    }

    [Fact]
    public void A_missing_file_and_a_missing_folder_are_fine()
    {
        var store = NewStore();
        Assert.Empty(store.All());
        Assert.True(store.CanWrite);
        Assert.True(store.Save(AllDay("الف", new DateTime(2026, 11, 5))));
        Assert.True(File.Exists(FilePath));
        Assert.False(File.Exists(FilePath + ".tmp"));
    }
}
