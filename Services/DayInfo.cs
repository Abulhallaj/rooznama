using System.Globalization;

namespace Rooznama.Services;

/// <summary>A holiday or observance line shown under the calendar.</summary>
public sealed record OccasionRow(string Text, string Caption);

/// <summary>Extra info shown when hovering a day. Add more lines (Hijri / lunar) in Lines().</summary>
public static class DayInfo
{
    public static string Text(DateTime d) => string.Join("\n", Lines(d));

    /// <summary>The hover text plus the titles of the day's holidays and observances.</summary>
    public static string Text(DateTime d, IReadOnlyList<CalendarItem> occasions) =>
        string.Join("\n", Lines(d).Concat(occasions.Select(Title)));

    public static IEnumerable<string> Lines(DateTime d)
    {
        yield return d.ToString("dddd, d MMMM yyyy", CultureInfo.InvariantCulture);
        // later: yield return Hijri(d);
    }

    /// <summary>Persian title; in English mode the English one when the dataset has it.</summary>
    public static string Title(CalendarItem i) =>
        Loc.IsFa || string.IsNullOrWhiteSpace(i.Title.En) ? i.Title.Fa : i.Title.En!;

    /// <summary>Official holidays and the other observances of a day, kept as two separate lists.</summary>
    public static (IReadOnlyList<OccasionRow> Holidays, IReadOnlyList<OccasionRow> Observances) Split(IReadOnlyList<CalendarItem> items) =>
        (items.Where(i => i.IsHoliday).Select(i => new OccasionRow(Title(i), Loc.T("occ.holiday"))).ToList(),
         items.Where(i => !i.IsHoliday).Select(i => new OccasionRow(Title(i), Loc.T("occ.observance"))).ToList());
}
