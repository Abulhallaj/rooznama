using System.Globalization;

namespace Rooznama.Services;

/// <summary>Extra info shown when hovering a day. Add more lines (Hijri / lunar / occasions) in Lines().</summary>
public static class DayInfo
{
    public static string Text(DateTime d) => string.Join("\n", Lines(d));

    public static IEnumerable<string> Lines(DateTime d)
    {
        yield return d.ToString("dddd, d MMMM yyyy", CultureInfo.InvariantCulture);
        // later: yield return Hijri(d); yield return Occasion(d);
    }
}
