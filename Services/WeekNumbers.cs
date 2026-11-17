using System.Globalization;

namespace Rooznama.Services;

/// <summary>Week-of-year numbers. Only the Iranian rule exists today; the other calendars plug into Of() when the active calendar becomes switchable.</summary>
public static class WeekNumbers
{
    static readonly PersianCalendar Pc = new();

    /// <summary>The number of the week (Saturday to Friday) that contains <paramref name="date"/>, by the rule of the given calendar; 0 when it cannot be worked out.</summary>
    public static int Of(DateTime date, CalendarKind calendar = CalendarKind.Jalali) => calendar switch
    {
        // later: Gregorian (ISO 8601) and Hijri weeks
        _ => Iranian(date)
    };

    /// <summary>
    /// Iranian rule: weeks start on Saturday and the week that contains 1 Farvardin is week 1. A week that spans two years
    /// (it contains 1 Farvardin of the new year) is week 1 of the new year.
    /// </summary>
    public static int Iranian(DateTime date)
    {
        var d = date.Date;
        if (d < Pc.MinSupportedDateTime.AddDays(14) || d > Pc.MaxSupportedDateTime.AddDays(-14)) return 0;

        var weekStart = d.AddDays(-(((int)d.DayOfWeek + 1) % 7));
        int year = Pc.GetYear(weekStart.AddDays(6));
        var nowruz = Pc.ToDateTime(year, 1, 1, 0, 0, 0, 0);
        var week1 = nowruz.AddDays(-(((int)nowruz.DayOfWeek + 1) % 7));
        return (weekStart - week1).Days / 7 + 1;
    }
}
