namespace Rooznama.Services;

public record CalendarEvent(string Title, DateTime Start, DateTime End)
{
    public string TimeText => PersianDate.Digits($"{Start:HH:mm} – {End:HH:mm}");
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
