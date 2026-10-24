using Microsoft.UI.Xaml;

namespace Rooznama.Services;

/// <summary>Tiny fa/en localisation. XAML uses {x:Bind svc:Loc.T('key')}; changing language needs a restart.</summary>
public static class Loc
{
    public static bool IsFa => App.Settings.Language != "en";
    public static FlowDirection Flow => IsFa ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    public static string Lang => IsFa ? "fa-IR" : "en-US";

    static readonly Dictionary<string, (string Fa, string En)> S = new()
    {
        ["app.name"] = ("روزنما", "Rooznama"),

        ["calendar"] = ("تقویم", "Calendar"),
        ["converter"] = ("مبدل تاریخ", "Date converter"),
        ["settings"] = ("تنظیمات", "Settings"),
        ["about"] = ("درباره", "About"),
        ["exit"] = ("خروج", "Exit"),

        ["events"] = ("رویدادها", "Events"),
        ["events.none"] = ("رویدادی برای این روز ثبت نشده است", "No events for this day"),

        ["conv.shamsi"] = ("تاریخ شمسی", "Shamsi (Jalali) date"),
        ["conv.gregorian"] = ("تاریخ میلادی", "Gregorian date"),
        ["copy"] = ("کپی", "Copy"),

        ["set.appearance"] = ("ظاهر", "Appearance"),
        ["set.language"] = ("زبان", "Language"),
        ["set.font"] = ("فونت", "Font"),
        ["set.tray"] = ("آیکون Tray و تقویم", "Tray icon & calendar"),
        ["set.color"] = ("رنگ آیکون", "Icon color"),
        ["color.blue"] = ("آبی", "Blue"),
        ["color.green"] = ("سبز", "Green"),
        ["color.red"] = ("قرمز", "Red"),
        ["color.purple"] = ("بنفش", "Purple"),
        ["color.dark"] = ("خاکستری تیره", "Dark gray"),
        ["set.gregorian"] = ("نمایش تاریخ میلادی در Tooltip", "Show Gregorian date in the tooltip"),
        ["set.autohide"] = ("بستن تقویم با کلیک بیرون از آن", "Close the calendar when it loses focus"),
        ["set.startup"] = ("اجرا هنگام ورود به ویندوز", "Start with Windows"),
        ["on"] = ("روشن", "On"),
        ["off"] = ("خاموش", "Off"),
        ["set.restartmsg"] = ("برای اعمال زبان، فونت و تم، برنامه باید دوباره راه‌اندازی شود.", "Restart the app to apply the language, font and theme."),
        ["set.theme"] = ("تم", "Theme"),
        ["theme.auto"] = ("خودکار (مطابق ویندوز)", "Automatic (follow Windows)"),
        ["theme.light"] = ("روشن", "Light"),
        ["theme.dark"] = ("تیره", "Dark"),
        ["today"] = ("امروز", "Today"),
        ["tip.collapse"] = ("کوچک‌کردن / بزرگ‌کردن", "Collapse / expand"),
        ["set.restart"] = ("راه‌اندازی مجدد", "Restart"),

        ["about.version"] = ("نسخه", "Version"),
        ["about.desc"] = ("تقویم شمسی سبک برای system tray: روز شمسی روی آیکون، تاریخ شمسی و میلادی در Tooltip، تقویم ماهانه و مبدل تاریخ.",
                          "A lightweight Shamsi (Jalali) calendar for the system tray: the day on the icon, Shamsi and Gregorian dates in the tooltip, a monthly calendar and a date converter."),
        ["about.font"] = ("فونت", "Font"),
        ["about.tech"] = ("ساخته‌شده با .NET و WinUI 3", "Built with .NET and WinUI 3"),
    };

    public static string T(string key) => S.TryGetValue(key, out var v) ? (IsFa ? v.Fa : v.En) : key;
}
