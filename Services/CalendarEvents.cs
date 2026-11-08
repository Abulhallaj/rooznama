namespace Rooznama.Services;

public record CalendarEvent(string Title, DateTime Start, DateTime End)
{
    /// <summary>Id inside the provider that made the event (null for providers without editing).</summary>
    public string? Id { get; init; }
    /// <summary>"personal" today; later for example "google".</summary>
    public string Source { get; init; } = "";
    public bool IsAllDay { get; init; }
    public bool IsReadOnly { get; init; } = true;
    public bool Repeats { get; init; }
    public string? Description { get; init; }

    public string TimeText
    {
        get
        {
            var t = IsAllDay ? Loc.T("event.allday") : PersianDate.Digits($"{Start:HH:mm} – {End:HH:mm}");
            if (Repeats) t = $"{t} · {Loc.T("event.yearly")}";
            return Source == "google" ? $"{t} · Google" : t;
        }
    }
}

// Extension point: implement this for Google Calendar / Outlook / ICS and assign to App.Events.
public interface ICalendarEventProvider
{
    Task<IReadOnlyList<CalendarEvent>> GetEventsAsync(DateTime from, DateTime to);
}

public sealed class NullEventProvider : ICalendarEventProvider
{
    public Task<IReadOnlyList<CalendarEvent>> GetEventsAsync(DateTime from, DateTime to) =>
        Task.FromResult<IReadOnlyList<CalendarEvent>>(Array.Empty<CalendarEvent>());
}
