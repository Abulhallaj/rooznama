using System.Globalization;

namespace Rooznama.Services;

public static class PersianDate
{
    static readonly PersianCalendar Pc = new();

    static readonly string[] FaMonths =
        { "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند" };
    static readonly string[] EnMonths =
        { "Farvardin", "Ordibehesht", "Khordad", "Tir", "Mordad", "Shahrivar", "Mehr", "Aban", "Azar", "Dey", "Bahman", "Esfand" };

    // index = (int)DayOfWeek
    static readonly string[] FaDays = { "یکشنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنجشنبه", "جمعه", "شنبه" };
    static readonly string[] EnDays = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

    public static string MonthName(int m) => (Loc.IsFa ? FaMonths : EnMonths)[m - 1];
    public static string WeekdayName(DayOfWeek d) => (Loc.IsFa ? FaDays : EnDays)[(int)d];

    public static int Year(DateTime d) => Pc.GetYear(d);
    public static int Month(DateTime d) => Pc.GetMonth(d);
    public static int Day(DateTime d) => Pc.GetDayOfMonth(d);
    public static int DaysInMonth(int y, int m) => Pc.GetDaysInMonth(y, m);
    public static DateTime ToGregorian(int y, int m, int d) => Pc.ToDateTime(y, m, d, 0, 0, 0, 0);
    public static bool InRange(DateTime d) => d >= Pc.MinSupportedDateTime && d <= Pc.MaxSupportedDateTime;

    /// <summary>Persian digits in Persian mode, plain digits in English mode.</summary>
    public static string Digits(string s) =>
        Loc.IsFa ? string.Concat(s.Select(c => c is >= '0' and <= '9' ? (char)('۰' + (c - '0')) : c)) : s;

    public static string Num(int n) => Digits(n.ToString(CultureInfo.InvariantCulture));

    public static string Full(DateTime d) =>
        $"{WeekdayName(d.DayOfWeek)} {Num(Day(d))} {MonthName(Month(d))} {Num(Year(d))}";

    public static string Numeric(DateTime d) => Digits($"{Year(d):0000}/{Month(d):00}/{Day(d):00}");
}
