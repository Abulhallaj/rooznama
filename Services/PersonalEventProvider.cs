namespace Rooznama.Services;

/// <summary>Feeds the personal events (a local JSON file) to the calendar through ICalendarEventProvider, the same door a Google provider will use.</summary>
public sealed class PersonalEventProvider : ICalendarEventProvider
{
    public PersonalEventStore Store { get; }

    public PersonalEventProvider(PersonalEventStore store) => Store = store;

    public Task<IReadOnlyList<CalendarEvent>> GetEventsAsync(DateTime from, DateTime to)
    {
        IReadOnlyList<CalendarEvent> list = Store.GetInstances(from, to, d => HijriService.Get(d))
            .Select(i => new CalendarEvent(i.Source.Title, i.Start, i.End)
            {
                Id = i.Source.Id,
                Source = "personal",
                IsAllDay = i.Source.IsAllDay,
                IsReadOnly = false,
                Repeats = i.Source.Recurrence != EventRecurrence.None,
                Description = i.Source.Description
            })
            .ToList();
        return Task.FromResult(list);
    }
}
