using System.Globalization;
using System.Net;

namespace Rooznama.Services;

public sealed record GoogleSyncResult(int Uploaded, int DeletedRemotely, int Imported, int UpdatedLocally, int RemovedLocally,
                                      int Failed, string? FirstError, bool SkippedMassDeletion)
{
    public bool AnythingChanged => Uploaded + DeletedRemotely + Imported + UpdatedLocally + RemovedLocally > 0;
}

/// <summary>
/// Two-way sync of the personal events with one Google calendar, over a window (one year back, two years ahead):
/// 1. deletions made here are sent, 2. new and changed events are sent, 3. the window is read back: events made elsewhere
/// come in, remote edits and remote deletions are applied. A step that fails is counted and left pending; the next run retries it.
/// Independent of the UI and of the network (the API is injected).
/// </summary>
public sealed class GoogleSyncService
{
    readonly IGoogleCalendarApi _api;
    readonly PersonalEventStore _store;
    readonly Func<DateTime, HijriDate?> _hijri;
    readonly Func<DateTime> _today;
    readonly Action<string>? _log;
    readonly string _calendarId;
    readonly TimeZoneInfo? _zone;
    readonly string _untitled;

    sealed class Counters
    {
        public int Uploaded, DeletedRemotely, Imported, UpdatedLocally, RemovedLocally, Failed;
        public string? FirstError;
        public bool SkippedMassDeletion;
    }

    public GoogleSyncService(IGoogleCalendarApi api, PersonalEventStore store, Func<DateTime, HijriDate?> hijri, Func<DateTime> today,
                             Action<string>? log = null, string calendarId = "primary", TimeZoneInfo? zone = null, string untitled = "(no title)")
    {
        _api = api;
        _store = store;
        _hijri = hijri;
        _today = today;
        _log = log;
        _calendarId = calendarId;
        _zone = zone;
        _untitled = untitled;
    }

    public async Task<GoogleSyncResult> SyncAsync(CancellationToken ct = default)
    {
        var c = new Counters();
        var today = _today().Date;
        var from = today.AddYears(-1);
        var to = today.AddYears(2);
        var min = new DateTimeOffset(DateTime.SpecifyKind(from, DateTimeKind.Utc));
        var max = new DateTimeOffset(DateTime.SpecifyKind(to, DateTimeKind.Utc));

        // what is at Google now: lets a step that failed half-way be repeated without making duplicates
        var before = await _api.ListAsync(min, max, ct).ConfigureAwait(false);
        var marked = before.Where(g => g.RooznamaId is not null).ToLookup(g => g.RooznamaId!);

        var (_, deletions) = _store.Pending();
        foreach (var e in deletions)
            await StepAsync(c, ct, $"delete {e.Id}", async () =>
            {
                foreach (var id in RemoteIds(e)) await _api.DeleteAsync(id, ct).ConfigureAwait(false);
                foreach (var g in marked[e.Id]) if (g.Id is not null) await _api.DeleteAsync(g.RecurringEventId ?? g.Id, ct).ConfigureAwait(false);
                _store.Purge(e.Id);
                c.DeletedRemotely++;
            }).ConfigureAwait(false);

        var (uploads, _) = _store.Pending();
        foreach (var e in uploads)
            await StepAsync(c, ct, $"upload {e.Id}", () => UploadAsync(e, from, to, marked[e.Id].ToList(), c, ct)).ConfigureAwait(false);

        await ReconcileAsync(min, max, from, to, c, ct).ConfigureAwait(false);

        var result = new GoogleSyncResult(c.Uploaded, c.DeletedRemotely, c.Imported, c.UpdatedLocally, c.RemovedLocally, c.Failed, c.FirstError, c.SkippedMassDeletion);
        if (result.AnythingChanged) _store.NotifyChanged();
        return result;
    }

    static IEnumerable<string> RemoteIds(PersonalEvent e)
    {
        if (e.ExternalId is not null) yield return e.ExternalId;
        if (e.RemoteInstances is not null)
            foreach (var id in e.RemoteInstances.Values) yield return id;
    }

    // one failing event must not stop the others; a sign-in problem and cancellation do stop everything
    async Task StepAsync(Counters c, CancellationToken ct, string what, Func<Task> step)
    {
        try { await step().ConfigureAwait(false); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (GoogleAuthException) { throw; }
        catch (Exception ex)
        {
            c.Failed++;
            c.FirstError ??= ex.Message;
            _log?.Invoke($"google sync: {what} failed: {ex.Message}");
        }
    }

    // ---------- sending ----------
    async Task UploadAsync(PersonalEvent e, DateTime from, DateTime to, List<GoogleEvent> alreadyThere, Counters c, CancellationToken ct)
    {
        var expected = e.UpdatedUtc;
        bool yearlyByOtherCalendar = e.Origin == "local" && e.Recurrence is EventRecurrence.YearlyJalali or EventRecurrence.YearlyHijri;

        if (yearlyByOtherCalendar)
        {
            // Google can only repeat by the Gregorian calendar: one event per occurrence inside the window
            var desired = PersonalEventStore.Expand(e, from, to, _hijri).ToDictionary(i => i.Start.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
            var existing = new Dictionary<string, string>(e.RemoteInstances ?? new Dictionary<string, string>());
            foreach (var g in alreadyThere)
                if (g.ExtendedProperties?.Private is { } p && p.TryGetValue(GoogleEventMapper.InstanceKey, out var day) && g.Id is not null)
                    existing.TryAdd(day, g.Id);

            if (e.ExternalId is not null) await _api.DeleteAsync(e.ExternalId, ct).ConfigureAwait(false);   // it used to be one event or a Gregorian repetition

            var map = new Dictionary<string, string>();
            foreach (var (day, instance) in desired)
            {
                var g = GoogleEventMapper.ToGoogle(e, instance);
                var sentInstance = await SendOneAsync(existing.GetValueOrDefault(day), g, ct).ConfigureAwait(false);
                map[day] = sentInstance.Id!;
            }
            foreach (var (day, id) in existing)
                if (!desired.ContainsKey(day)) await _api.DeleteAsync(id, ct).ConfigureAwait(false);

            _store.ApplySync(e.Id, expected, p => { p.ExternalId = null; p.ETag = null; p.CalendarId = _calendarId; p.RemoteInstances = map; });
            c.Uploaded++;
            return;
        }

        // one event (or a Gregorian repetition, which Google keeps as one event with a rule)
        var id0 = e.ExternalId
                  ?? alreadyThere.Where(g => g.ExtendedProperties?.Private?.ContainsKey(GoogleEventMapper.InstanceKey) != true)
                                 .Select(g => g.RecurringEventId ?? g.Id).FirstOrDefault();
        var sent = await SendOneAsync(id0, GoogleEventMapper.ToGoogle(e), ct).ConfigureAwait(false);

        // per-year events left over from when this was a Jalali or Hijri repetition
        if (e.RemoteInstances is { Count: > 0 })
            foreach (var old in e.RemoteInstances.Values) await _api.DeleteAsync(old, ct).ConfigureAwait(false);

        _store.ApplySync(e.Id, expected, p => { p.ExternalId = sent.Id; p.ETag = sent.ETag; p.CalendarId = _calendarId; p.RemoteInstances = null; });
        c.Uploaded++;
    }

    // patch when the event exists at Google (it may have been deleted there meanwhile), otherwise insert
    async Task<GoogleEvent> SendOneAsync(string? id, GoogleEvent g, CancellationToken ct)
    {
        if (id is not null)
        {
            try { return await _api.PatchAsync(id, g, ct).ConfigureAwait(false); }
            catch (GoogleApiException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone) { }
        }
        return await _api.InsertAsync(g, ct).ConfigureAwait(false);
    }

    // ---------- reading back ----------
    async Task ReconcileAsync(DateTimeOffset min, DateTimeOffset max, DateTime from, DateTime to, Counters c, CancellationToken ct)
    {
        var remote = await _api.ListAsync(min, max, ct).ConfigureAwait(false);
        var remoteIds = new HashSet<string>(remote.Where(g => g.Id is not null).Select(g => g.Id!));
        var local = _store.Snapshot();

        var byExternal = new Dictionary<string, PersonalEvent>();
        var owned = new HashSet<string>();                      // Google ids that belong to an event made here
        var localIds = new HashSet<string>();
        foreach (var e in local)
        {
            localIds.Add(e.Id);
            if (e.IsDeleted) continue;
            if (e.ExternalId is not null) { byExternal.TryAdd(e.ExternalId, e); owned.Add(e.ExternalId); }
            if (e.RemoteInstances is not null) foreach (var id in e.RemoteInstances.Values) owned.Add(id);
        }

        foreach (var g in remote)
        {
            if (g.Id is null || g.IsCancelled) continue;

            bool ours = owned.Contains(g.Id)
                        || (g.RecurringEventId is not null && owned.Contains(g.RecurringEventId))
                        || (g.RooznamaId is not null && localIds.Contains(g.RooznamaId));
            byExternal.TryGetValue(g.Id, out var mine);

            if (mine is not null)
            {
                // an edit made at Google; only single events are compared (a repeating one is seen as many occurrences)
                bool single = mine.Recurrence == EventRecurrence.None;
                if (single && mine.SyncState == EventSyncState.Synced && mine.ETag != g.ETag && GoogleEventMapper.FromGoogle(g, _zone, _untitled) is { } fresh)
                {
                    if (_store.ApplySync(mine.Id, mine.UpdatedUtc, p => GoogleEventMapper.CopyContent(fresh, p))) c.UpdatedLocally++;
                }
                continue;
            }
            if (ours) continue;

            if (GoogleEventMapper.FromGoogle(g, _zone, _untitled) is { } imported && _store.Import(imported)) c.Imported++;
        }

        // events deleted at Google: only single synced events that lie in the window can be judged
        var linked = local.Where(e => !e.IsDeleted && e.ExternalId is not null && e.Recurrence == EventRecurrence.None
                                      && e.SyncState == EventSyncState.Synced && e.Start < to && e.End > from).ToList();
        var gone = linked.Where(e => !remoteIds.Contains(e.ExternalId!)).ToList();
        if (gone.Count > 5 && gone.Count * 2 > linked.Count)
        {
            c.SkippedMassDeletion = true;   // looks like the wrong calendar or a partial answer, not a clean-up: keep everything
            _log?.Invoke($"google sync: {gone.Count} of {linked.Count} events seem deleted at Google; not removing them here");
            return;
        }
        foreach (var e in gone)
            if (_store.Purge(e.Id)) c.RemovedLocally++;
    }
}
