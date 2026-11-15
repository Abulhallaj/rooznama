using System.Globalization;

namespace Rooznama.Services;

/// <summary>The app's Hijri calendar: the source chosen in settings, UmAlQuraCalendar for any day that source does not cover.</summary>
public static class HijriService
{
    static readonly UtHijriSource Ut = UtHijriSource.Load(Path.Combine(AppContext.BaseDirectory, "calendar", "hijri", "ut.json"), Log.Write);

    /// <summary>Follows App.Settings.HijriSource live.</summary>
    public static IHijriProvider Provider { get; } = new HijriProvider(() => App.Settings.HijriSource, Ut);

    public static HijriDate? Get(DateTime date) => Provider.Get(date, out _);

    // index = month - 1
    static readonly string[] FaMonths =
    {
        "محرم", "صفر", "ربیع\u200cالاول", "ربیع\u200cالثانی", "جمادی\u200cالاول", "جمادی\u200cالثانی",
        "رجب", "شعبان", "رمضان", "شوال", "ذی\u200cالقعده", "ذی\u200cالحجه"
    };
    static readonly string[] EnMonths =
    {
        "Muharram", "Safar", "Rabi' al-awwal", "Rabi' al-thani", "Jumada al-awwal", "Jumada al-thani",
        "Rajab", "Sha'ban", "Ramadan", "Shawwal", "Dhu al-Qi'dah", "Dhu al-Hijjah"
    };

    public static string MonthName(int month) => (Loc.IsFa ? FaMonths : EnMonths)[month - 1];

    /// <summary>Arabic-Indic digits (٠١٢٣٤٥٦٧٨٩, not the Persian ۰۱۲۳۴۵۶۷۸۹) in Persian mode, plain digits in English mode.</summary>
    public static string Digits(string s) =>
        Loc.IsFa ? string.Concat(s.Select(c => c is >= '0' and <= '9' ? (char)('\u0660' + (c - '0')) : c)) : s;

    public static string Num(int n) => Digits(n.ToString(CultureInfo.InvariantCulture));

    public static string Full(HijriDate h) => $"{Num(h.Day)} {MonthName(h.Month)} {Num(h.Year)}";
}
