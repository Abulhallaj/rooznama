using System.Globalization;
using System.Text.Json.Serialization;

namespace Rooznama.Services;

// The parts of the Google Calendar v3 "Event" resource Rooznama uses, and the translation to and from PersonalEvent.
// Nothing here depends on the UI or the network.

public sealed class GoogleEvent
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("etag")] public string? ETag { get; set; }
    /// <summary>confirmed | tentative | cancelled</summary>
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("summary")] public string? Summary { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("location")] public string? Location { get; set; }
    [JsonPropertyName("start")] public GoogleEventTime? Start { get; set; }
    [JsonPropertyName("end")] public GoogleEventTime? End { get; set; }
    [JsonPropertyName("recurrence")] public List<string>? Recurrence { get; set; }
    [JsonPropertyName("recurringEventId")] public string? RecurringEventId { get; set; }
    /// <summary>RFC 3339 time of the last change.</summary>
    [JsonPropertyName("updated")] public string? Updated { get; set; }
    [JsonPropertyName("extendedProperties")] public GoogleExtendedProperties? ExtendedProperties { get; set; }

    [JsonIgnore] public bool IsCancelled => string.Equals(Status, "cancelled", StringComparison.OrdinalIgnoreCase);

    /// <summary>The Rooznama id stamped on an event this app sent (null for events made elsewhere).</summary>
    [JsonIgnore]
    public string? RooznamaId =>
        ExtendedProperties?.Private is { } p && p.TryGetValue(GoogleEventMapper.RooznamaIdKey, out var v) ? v : null;
}

public sealed class GoogleEventTime
{
    /// <summary>All-day events: yyyy-MM-dd (the end date is exclusive).</summary>
    [JsonPropertyName("date")] public string? Date { get; set; }
    /// <summary>Timed events: RFC 3339; with a timeZone it may be a wall-clock time without an offset.</summary>
    [JsonPropertyName("dateTime")] public string? DateTime { get; set; }
    /// <summary>IANA time zone name.</summary>
    [JsonPropertyName("timeZone")] public string? TimeZone { get; set; }
}

public sealed class GoogleExtendedProperties
{
    [JsonPropertyName("private")] public Dictionary<string, string>? Private { get; set; }
}

public static class GoogleEventMapper
{
    /// <summary>Private extended property holding the Rooznama event id (set on every event this app sends).</summary>
    public const string RooznamaIdKey = "rooznamaId";
    /// <summary>Private extended property holding the day (yyyyMMdd) of one materialized Jalali/Hijri occurrence.</summary>
    public const string InstanceKey = "rooznamaInstance";

    const string Wall = "yyyy-MM-dd'T'HH:mm:ss";

    /// <summary>
    /// The Google event for a PersonalEvent. With <paramref name="instance"/> the event is one yearly occurrence (a Jalali or Hijri
    /// repetition is sent as separate events because Google can only repeat by the Gregorian calendar); without it, a
    /// Gregorian yearly event keeps its RRULE.
    /// </summary>
    public static GoogleEvent ToGoogle(PersonalEvent e, PersonalEventInstance? instance = null)
    {
        var start = instance?.Start ?? e.Start;
        var end = instance?.End ?? e.End;
        if (end <= start) end = e.IsAllDay ? start.Date.AddDays(1) : start.AddHours(1);

        var g = new GoogleEvent
        {
            Summary = e.Title,
            Description = e.Description ?? "",     // "" clears the field when patching
            Location = e.Location ?? ""
        };

        if (e.IsAllDay)
        {
            g.Start = new GoogleEventTime { Date = start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) };
            g.End = new GoogleEventTime { Date = end.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) };
        }
        else
        {
            var zone = ToIana(e.TimeZone);
            if (zone is not null)
            {
                g.Start = new GoogleEventTime { DateTime = start.ToString(Wall, CultureInfo.InvariantCulture), TimeZone = zone };
                g.End = new GoogleEventTime { DateTime = end.ToString(Wall, CultureInfo.InvariantCulture), TimeZone = zone };
            }
            else
            {
                // a zone Google cannot be told about: send exact instants instead
                g.Start = new GoogleEventTime { DateTime = Instant(start, e.TimeZone) };
                g.End = new GoogleEventTime { DateTime = Instant(end, e.TimeZone) };
            }
        }

        // an empty list clears an earlier repetition when patching; an event imported from Google keeps whatever Google has
        if (instance is null && e.Origin == "local") g.Recurrence = e.Rrule is { } rrule ? new List<string> { rrule } : new List<string>();

        if (e.Origin == "local")
        {
            var props = new Dictionary<string, string> { [RooznamaIdKey] = e.Id };
            if (instance is not null) props[InstanceKey] = start.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            g.ExtendedProperties = new GoogleExtendedProperties { Private = props };
        }
        return g;
    }

    /// <summary>
    /// A PersonalEvent for an event made elsewhere (null when it has no usable start). Times are shown in <paramref name="target"/>
    /// (the local zone by default).
    /// </summary>
    public static PersonalEvent? FromGoogle(GoogleEvent g, TimeZoneInfo? target = null, string untitled = "(no title)")
    {
        if (string.IsNullOrEmpty(g.Id) || g.Start is null) return null;
        target ??= TimeZoneInfo.Local;

        DateTime start, end;
        bool allDay;
        if (g.Start.Date is { } sd)
        {
            if (!TryDay(sd, out start)) return null;
            allDay = true;
            end = g.End?.Date is { } ed && TryDay(ed, out var e1) && e1 > start ? e1 : start.AddDays(1);
        }
        else if (g.Start.DateTime is not null)
        {
            if (!TryWall(g.Start, target, out start)) return null;
            allDay = false;
            end = g.End is { DateTime: not null } ge && TryWall(ge, target, out var e2) && e2 >= start ? e2 : start.AddHours(1);
        }
        else return null;

        var updated = DateTime.UtcNow;
        if (g.Updated is { } u && DateTimeOffset.TryParse(u, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var uo))
            updated = uo.UtcDateTime;

        return new PersonalEvent
        {
            Origin = "google",
            ExternalId = g.Id,
            ETag = g.ETag,
            RecurringEventId = g.RecurringEventId,
            SyncState = EventSyncState.Synced,
            Title = string.IsNullOrWhiteSpace(g.Summary) ? untitled : g.Summary!,
            Description = string.IsNullOrWhiteSpace(g.Description) ? null : g.Description,
            Location = string.IsNullOrWhiteSpace(g.Location) ? null : g.Location,
            IsAllDay = allDay,
            Start = start,
            End = end,
            TimeZone = target.Id,
            Recurrence = EventRecurrence.None,
            UpdatedUtc = updated
        };
    }

    /// <summary>The fields a remote edit changes, copied onto the local event (everything else about it stays).</summary>
    public static void CopyContent(PersonalEvent from, PersonalEvent to)
    {
        to.Title = from.Title;
        to.Description = from.Description;
        to.Location = from.Location;
        to.IsAllDay = from.IsAllDay;
        to.Start = from.Start;
        to.End = from.End;
        to.TimeZone = from.TimeZone;
        to.ETag = from.ETag;
        to.UpdatedUtc = from.UpdatedUtc;
    }

    // ---------- time zones ----------
    /// <summary>The IANA name Google wants for a Windows or IANA zone id; null when it cannot be worked out.</summary>
    public static string? ToIana(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        if (id == "UTC" || id.Contains('/')) return id;   // already IANA
        return TimeZoneInfo.TryConvertWindowsIdToIanaId(id, out var iana) ? iana : null;
    }

    static string Instant(DateTime wall, string zoneId)
    {
        TimeZoneInfo tz;
        try { tz = TimeZoneInfo.FindSystemTimeZoneById(zoneId); } catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException) { tz = TimeZoneInfo.Local; }
        var local = DateTime.SpecifyKind(wall, DateTimeKind.Unspecified);
        var offset = tz.GetUtcOffset(tz.IsInvalidTime(local) ? local.AddHours(1) : local);
        return new DateTimeOffset(local, offset).ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
    }

    static bool TryDay(string s, out DateTime day) =>
        DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out day);

    // an RFC 3339 time (with an offset) is converted; a bare wall-clock time is read in the event's own zone and converted
    static bool TryWall(GoogleEventTime t, TimeZoneInfo target, out DateTime wall)
    {
        wall = default;
        if (t.DateTime is not { } s) return false;

        if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto) && HasOffset(s))
        {
            wall = DateTime.SpecifyKind(TimeZoneInfo.ConvertTime(dto, target).DateTime, DateTimeKind.Unspecified);
            return true;
        }
        if (!DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var bare)) return false;

        TimeZoneInfo source = target;
        if (t.TimeZone is { Length: > 0 } name)
        {
            try { source = TimeZoneInfo.FindSystemTimeZoneById(name); } catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException) { }
        }
        bare = DateTime.SpecifyKind(bare, DateTimeKind.Unspecified);
        var instant = new DateTimeOffset(bare, source.GetUtcOffset(source.IsInvalidTime(bare) ? bare.AddHours(1) : bare));
        wall = DateTime.SpecifyKind(TimeZoneInfo.ConvertTime(instant, target).DateTime, DateTimeKind.Unspecified);
        return true;
    }

    static bool HasOffset(string s) =>
        s.EndsWith("Z", StringComparison.OrdinalIgnoreCase) || (s.Length > 6 && (s[^6] is '+' or '-') && s[^3] == ':');
}
