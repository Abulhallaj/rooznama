using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rooznama.Services;

// Personal (user-made) events. The record is shaped like a Google Calendar / iCalendar event on purpose, so a Google
// provider later only has to connect and translate: ids, times and the all-day convention already match, deletions leave a
// tombstone until they are sent, and every change leaves a sync state. Nothing here depends on the UI.

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EventSyncState { LocalOnly, PendingUpload, Synced, PendingDelete }

/// <summary>
/// How an event repeats. Google can only repeat by the Gregorian calendar (RRULE); a Jalali or Hijri yearly repetition stays
/// a local rule and is sent as separate events when syncing.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EventRecurrence { None, YearlyJalali, YearlyHijri, YearlyGregorian }

public sealed class PersonalEvent
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.NewGuid().ToString("N");

    // --- link to a remote calendar (empty until a Google provider exists) ---
    [JsonPropertyName("externalId")] public string? ExternalId { get; set; }
    [JsonPropertyName("calendarId")] public string? CalendarId { get; set; }
    [JsonPropertyName("etag")] public string? ETag { get; set; }
    [JsonPropertyName("syncState")] public EventSyncState SyncState { get; set; } = EventSyncState.LocalOnly;

    // --- content ---
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("location")] public string? Location { get; set; }

    /// <summary>An all-day event starts at 00:00 of its day and ends at 00:00 of the NEXT day (exclusive end, as Google does).</summary>
    [JsonPropertyName("isAllDay")] public bool IsAllDay { get; set; } = true;
    /// <summary>Gregorian local wall-clock time; the calendar a person sees it in (Jalali, Hijri) is only a display choice.</summary>
    [JsonPropertyName("start")] public DateTime Start { get => _start; set => _start = Wall(value); }
    [JsonPropertyName("end")] public DateTime End { get => _end; set => _end = Wall(value); }
    /// <summary>Windows time zone id; mapped to an IANA id when syncing.</summary>
    [JsonPropertyName("timeZone")] public string TimeZone { get; set; } = TimeZoneInfo.Local.Id;

    // --- repetition ---
    [JsonPropertyName("recurrence")] public EventRecurrence Recurrence { get; set; } = EventRecurrence.None;
    /// <summary>Month and day of the yearly repetition in the calendar named by <see cref="Recurrence"/> (fixed when saved).</summary>
    [JsonPropertyName("recurrenceMonth")] public int RecurrenceMonth { get; set; }
    [JsonPropertyName("recurrenceDay")] public int RecurrenceDay { get; set; }

    /// <summary>The iCalendar rule for what Google understands (Gregorian yearly); null for everything else.</summary>
    [JsonIgnore] public string? Rrule => Recurrence == EventRecurrence.YearlyGregorian ? "RRULE:FREQ=YEARLY" : null;

    // --- bookkeeping ---
    [JsonPropertyName("createdUtc")] public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    [JsonPropertyName("updatedUtc")] public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    /// <summary>A tombstone: removed locally but not yet removed from the remote calendar.</summary>
    [JsonPropertyName("isDeleted")] public bool IsDeleted { get; set; }

    // --- remote calendar details ---
    /// <summary>"local" (made in Rooznama) or "google" (imported from Google Calendar).</summary>
    [JsonPropertyName("origin")] public string Origin { get; set; } = "local";
    /// <summary>An imported occurrence of a repeating Google event: the id of the repeating event.</summary>
    [JsonPropertyName("recurringEventId")] public string? RecurringEventId { get; set; }
    /// <summary>A Jalali or Hijri yearly event is sent to Google as one event per year: "yyyyMMdd" of the day -> Google event id.</summary>
    [JsonPropertyName("remoteInstances")] public Dictionary<string, string>? RemoteInstances { get; set; }

    DateTime _start = Wall(DateTime.Today);
    DateTime _end = Wall(DateTime.Today.AddDays(1));

    // wall-clock time without a UTC offset, so a file moved to another time zone still shows the same times
    static DateTime Wall(DateTime d) => DateTime.SpecifyKind(d, DateTimeKind.Unspecified);

    public PersonalEvent Clone()
    {
        var c = (PersonalEvent)MemberwiseClone();
        if (RemoteInstances is not null) c.RemoteInstances = new Dictionary<string, string>(RemoteInstances);
        return c;
    }
}

public sealed class PersonalEventFile
{
    [JsonPropertyName("schemaVersion")] public int SchemaVersion { get; set; } = PersonalEventStore.SupportedSchemaVersion;
    [JsonPropertyName("events")] public List<PersonalEvent> Events { get; set; } = new();
}

/// <summary>One occurrence of an event (a repeating event has one per year).</summary>
public sealed record PersonalEventInstance(PersonalEvent Source, DateTime Start, DateTime End);

public static class EventRecurrenceFields
{
    static readonly PersianCalendar Pc = new();

    /// <summary>The month and day a yearly repetition is anchored to, in the chosen calendar (null when the day has no date there).</summary>
    public static (int Month, int Day)? Of(DateTime date, EventRecurrence kind, Func<DateTime, HijriDate?> hijri) => kind switch
    {
        EventRecurrence.YearlyGregorian => (date.Month, date.Day),
        EventRecurrence.YearlyJalali => (Pc.GetMonth(date), Pc.GetDayOfMonth(date)),
        EventRecurrence.YearlyHijri => hijri(date) is { } h ? (h.Month, h.Day) : null,
        _ => null
    };
}

/// <summary>The personal events in one JSON file. Writes are atomic; a file written by a newer version is never overwritten.</summary>
public sealed class PersonalEventStore
{
    public const int SupportedSchemaVersion = 1;

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    readonly string _path;
    readonly Action<string>? _log;
    readonly object _gate = new();
    List<PersonalEvent> _items = new();
    bool _readOnly;

    /// <summary>Raised (on the calling thread) after anything in the store changed: a save, a delete or a sync. Refresh the display on this.</summary>
    public event Action? Changed;

    /// <summary>Raised after the user's own save or delete (not after a sync); this is what asks for a sync.</summary>
    public event Action? Edited;

    public PersonalEventStore(string path, Action<string>? log = null)
    {
        _path = path;
        _log = log;
        Load();
    }

    void Load()
    {
        try
        {
            if (!File.Exists(_path)) return;
            var file = JsonSerializer.Deserialize<PersonalEventFile>(File.ReadAllText(_path), JsonOptions);
            if (file is null) return;
            if (file.SchemaVersion > SupportedSchemaVersion)
            {
                _readOnly = true;   // written by a newer Rooznama: read nothing, change nothing
                _log?.Invoke($"personal events: {_path} has a newer schemaVersion {file.SchemaVersion}; left untouched");
                return;
            }
            _items = file.Events ?? new List<PersonalEvent>();
        }
        catch (JsonException ex)
        {
            _log?.Invoke($"personal events: {_path} is damaged ({ex.Message}); kept as .bad and started fresh");
            try { File.Move(_path, _path + ".bad", overwrite: true); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { _readOnly = true; }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _readOnly = true;
            _log?.Invoke($"personal events: cannot read {_path}: {ex.Message}");
        }
    }

    public bool CanWrite => !_readOnly;

    /// <summary>Every event that has not been deleted. The objects are read-only snapshots: change a copy from <see cref="Get"/> and Save it.</summary>
    public IReadOnlyList<PersonalEvent> All()
    {
        lock (_gate) return _items.Where(e => !e.IsDeleted).ToList();
    }

    /// <summary>A copy of one event, safe to edit.</summary>
    public PersonalEvent? Get(string id)
    {
        lock (_gate) return _items.FirstOrDefault(e => e.Id == id && !e.IsDeleted)?.Clone();
    }

    /// <summary>Adds the event or replaces the one with the same id. False when the file cannot be written.</summary>
    public bool Save(PersonalEvent ev)
    {
        lock (_gate)
        {
            if (_readOnly) return false;
            var now = DateTime.UtcNow;
            int i = _items.FindIndex(x => x.Id == ev.Id);
            if (i >= 0)
            {
                var old = _items[i];
                ev.CreatedUtc = old.CreatedUtc;
                ev.ExternalId ??= old.ExternalId;
                ev.CalendarId ??= old.CalendarId;
                ev.ETag ??= old.ETag;
                ev.IsDeleted = false;
                _items[i] = ev;
            }
            else
            {
                ev.CreatedUtc = now;
                _items.Add(ev);
            }
            ev.UpdatedUtc = now;
            ev.SyncState = ev.ExternalId is null ? EventSyncState.LocalOnly : EventSyncState.PendingUpload;
            if (!Write()) return false;
        }
        Edited?.Invoke();
        Changed?.Invoke();
        return true;
    }

    /// <summary>Removes an event. One that already exists remotely (one event or its yearly occurrences) stays as a tombstone until the deletion has been sent.</summary>
    public bool Delete(string id)
    {
        lock (_gate)
        {
            if (_readOnly) return false;
            int i = _items.FindIndex(x => x.Id == id);
            if (i < 0) return false;
            if (_items[i].ExternalId is null && _items[i].RemoteInstances is not { Count: > 0 })
            {
                _items.RemoveAt(i);   // never sent anywhere: nothing to tell the remote calendar
            }
            else
            {
                var tomb = _items[i].Clone();
                tomb.IsDeleted = true;
                tomb.SyncState = EventSyncState.PendingDelete;
                tomb.UpdatedUtc = DateTime.UtcNow;
                _items[i] = tomb;
            }
            if (!Write()) return false;
        }
        Edited?.Invoke();
        Changed?.Invoke();
        return true;
    }

    bool Write()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var tmp = _path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(new PersonalEventFile { Events = _items }, JsonOptions));
            File.Move(tmp, _path, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _log?.Invoke($"personal events: cannot write {_path}: {ex.Message}");
            return false;
        }
    }

    /// <summary>The occurrences that overlap [from, to). <paramref name="hijri"/> is only used by Hijri-repeating events.</summary>
    public IReadOnlyList<PersonalEventInstance> GetInstances(DateTime from, DateTime to, Func<DateTime, HijriDate?> hijri)
    {
        List<PersonalEvent> snapshot;
        lock (_gate) snapshot = _items.Where(e => !e.IsDeleted).ToList();

        var result = new List<PersonalEventInstance>();
        foreach (var e in snapshot) result.AddRange(Expand(e, from, to, hijri));
        result.Sort((a, b) => a.Start.CompareTo(b.Start));
        return result;
    }

    /// <summary>The occurrences of one event that overlap [from, to).</summary>
    public static IReadOnlyList<PersonalEventInstance> Expand(PersonalEvent e, DateTime from, DateTime to, Func<DateTime, HijriDate?> hijri)
    {
        var result = new List<PersonalEventInstance>();
        if (e.Recurrence == EventRecurrence.None)
        {
            if (e.Start < to && (e.End > from || e.Start >= from)) result.Add(new PersonalEventInstance(e, e.Start, e.End));
            return result;
        }

        var duration = e.End - e.Start;
        var last = (to > from ? to.AddTicks(-1) : from).Date;
        int guard = 0;
        for (var d = from.Date; d <= last && guard++ < 4000; d = d.AddDays(1))
        {
            if (d < e.Start.Date) continue;
            if (EventRecurrenceFields.Of(d, e.Recurrence, hijri) is not { } f) continue;
            if (f.Month != e.RecurrenceMonth || f.Day != e.RecurrenceDay) continue;
            var start = d + e.Start.TimeOfDay;
            result.Add(new PersonalEventInstance(e, start, start + duration));
        }
        return result;
    }

    // ---------- used by the Google sync (they never count as the user's own edits) ----------

    /// <summary>Everything including tombstones, as copies.</summary>
    public IReadOnlyList<PersonalEvent> Snapshot()
    {
        lock (_gate) return _items.Select(e => e.Clone()).ToList();
    }

    /// <summary>What still has to be sent: new or changed events, and deletions of events that exist remotely.</summary>
    public (IReadOnlyList<PersonalEvent> Upload, IReadOnlyList<PersonalEvent> Delete) Pending()
    {
        lock (_gate)
            return (_items.Where(e => !e.IsDeleted && e.SyncState is EventSyncState.LocalOnly or EventSyncState.PendingUpload).Select(e => e.Clone()).ToList(),
                    _items.Where(e => e.IsDeleted && e.SyncState == EventSyncState.PendingDelete).Select(e => e.Clone()).ToList());
    }

    /// <summary>
    /// Records what a sync step did to an event (remote id, etag ...). The event becomes Synced, unless the user changed it
    /// after <paramref name="expectedUpdatedUtc"/>: then that newer edit stays pending. Silent: call <see cref="NotifyChanged"/> afterwards.
    /// </summary>
    public bool ApplySync(string id, DateTime expectedUpdatedUtc, Action<PersonalEvent> change)
    {
        lock (_gate)
        {
            if (_readOnly) return false;
            int i = _items.FindIndex(x => x.Id == id);
            if (i < 0) return false;
            var updated = _items[i].Clone();
            bool editedMeanwhile = updated.UpdatedUtc != expectedUpdatedUtc;
            change(updated);
            updated.SyncState = editedMeanwhile ? EventSyncState.PendingUpload : EventSyncState.Synced;
            _items[i] = updated;
            return Write();
        }
    }

    /// <summary>Adds an event that came from the remote calendar, or replaces the one with the same id (its creation time is kept). Silent.</summary>
    public bool Import(PersonalEvent ev)
    {
        lock (_gate)
        {
            if (_readOnly) return false;
            int i = _items.FindIndex(x => x.Id == ev.Id);
            if (i >= 0) { ev.CreatedUtc = _items[i].CreatedUtc; _items[i] = ev; }
            else _items.Add(ev);
            return Write();
        }
    }

    /// <summary>Removes an event completely, a tombstone included. Silent.</summary>
    public bool Purge(string id)
    {
        lock (_gate)
        {
            if (_readOnly) return false;
            if (_items.RemoveAll(x => x.Id == id) == 0) return false;
            return Write();
        }
    }

    /// <summary>
    /// For a different Google account: events imported from the old account go, everything made here forgets its remote ids and
    /// becomes new again. Silent.
    /// </summary>
    public bool ResetRemoteLinks()
    {
        lock (_gate)
        {
            if (_readOnly) return false;
            _items.RemoveAll(e => e.Origin == "google" || e.IsDeleted);
            foreach (var e in _items)
            {
                e.ExternalId = null;
                e.CalendarId = null;
                e.ETag = null;
                e.RemoteInstances = null;
                e.SyncState = EventSyncState.LocalOnly;
            }
            return Write();
        }
    }

    /// <summary>Raises <see cref="Changed"/> after a sync has finished changing things.</summary>
    public void NotifyChanged() => Changed?.Invoke();
}
