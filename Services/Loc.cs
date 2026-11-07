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
        ["occ.holiday"] = ("تعطیل رسمی", "Official holiday"),
        ["occ.observance"] = ("مناسبت", "Observance"),
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
        ["about.project"] = ("پروژه در GitHub", "Project on GitHub"),
        ["about.issues"] = ("گزارش مشکل یا پیشنهاد", "Report an issue / suggest"),
        ["about.portable"] = ("حالت پرتابل (تنظیمات کنار برنامه ذخیره می‌شود)", "Portable mode (settings are stored next to the app)"),

        ["support"] = ("حمایت", "Support"),
        ["support.title"] = ("حمایت از روزنما", "Support Rooznama"),
        ["support.text"] = ("روزنما رایگان و متن‌باز است. اگر برایتان مفید بوده، می‌توانید با دعوت من به یک قهوه از ادامه توسعه‌اش حمایت کنید. حمایت کاملاً اختیاری است و چیزی را قفل نمی‌کند.",
                            "Rooznama is free and open source. If you find it useful, you can support its development by buying me a coffee. Support is entirely optional and unlocks nothing."),
        ["support.button"] = ("حمایت از پروژه", "Support the project"),
        ["support.star"] = ("ستاره دادن در GitHub", "Star on GitHub"),

        ["set.customtip"] = ("tooltip راست‌چین اختصاصی روی آیکون", "Custom right-to-left tooltip on the tray icon"),
        ["set.updates"] = ("به‌روزرسانی", "Updates"),
        ["set.updates.auto"] = ("بررسی خودکار نسخه جدید", "Check for updates automatically"),
        ["set.updates.note"] = ("فقط آخرین نسخه از GitHub خوانده می‌شود؛ اطلاعات شخصی یا آمار استفاده ارسال نمی‌شود.",
                                "Only the latest release is read from GitHub; no personal data or usage statistics are sent."),
        ["set.updates.check"] = ("بررسی اکنون", "Check now"),
        ["upd.checking"] = ("در حال بررسی…", "Checking…"),
        ["upd.uptodate"] = ("نسخه شما به‌روز است.", "You're up to date."),
        ["upd.available"] = ("نسخه جدید {0} موجود است", "Version {0} is available"),
        ["upd.download"] = ("دانلود", "Download"),
        ["upd.failed"] = ("بررسی انجام نشد (اتصال اینترنت؟)", "Couldn't check (no internet?)"),
        ["upd.dev"] = ("نسخه توسعه است؛ بررسی به‌روزرسانی غیرفعال است.", "Development build; update check is disabled."),

        ["set.cal"] = ("تقویم", "Calendar"),
        ["set.hijri.source"] = ("منبع تاریخ قمری", "Hijri date source"),
        ["hijri.ut"] = ("مرکز تقویم دانشگاه تهران (رسمی ایران)", "Tehran University Calendar Center (official Iran)"),
        ["hijri.umalqura"] = ("ام‌القری (UmAlQuraCalendar)", "Umm al-Qura (UmAlQuraCalendar)"),
        ["set.hijri.note"] = ("برای روزهایی که منبع انتخاب‌شده داده ندارد (مثلاً سال‌های بعد از آخرین تقویم رسمی) به‌طور خودکار از ام‌القری استفاده می‌شود؛ ممکن است تا یک روز با تقویم رسمی ایران اختلاف داشته باشد.",
                           "Days the selected source does not cover (for example years after the latest official calendar) automatically use Umm al-Qura, which can differ from Iran's official calendar by a day."),
        ["set.hijri.tip"] = ("نمایش تاریخ قمری در Tooltip", "Show the Hijri date in tooltips"),
        ["set.hijri.cells"] = ("نمایش روز قمری در خانه‌های تقویم", "Show the Hijri day in calendar cells"),
        ["set.weeks"] = ("نمایش شماره هفته در تقویم", "Show the week number in the calendar"),
        ["week"] = ("هفته", "Wk"),
        ["occ.approx"] = ("تقریبی", "approximate"),

        ["event.add"] = ("افزودن رویداد", "Add event"),
        ["event.edit"] = ("ویرایش رویداد", "Edit event"),
        ["event.title"] = ("عنوان", "Title"),
        ["event.notes"] = ("توضیحات", "Notes"),
        ["event.date"] = ("تاریخ", "Date"),
        ["event.allday"] = ("همه روز", "All day"),
        ["event.start"] = ("شروع", "Start"),
        ["event.end"] = ("پایان", "End"),
        ["event.repeat"] = ("تکرار هر سال", "Repeat every year"),
        ["event.repeat.in"] = ("بر اساس تقویم", "In the calendar"),
        ["event.yearly"] = ("هر سال", "Every year"),
        ["event.save"] = ("ذخیره", "Save"),
        ["event.delete"] = ("حذف", "Delete"),
        ["event.cancel"] = ("انصراف", "Cancel"),
        ["cal.jalali"] = ("شمسی", "Shamsi"),
        ["cal.hijri"] = ("قمری", "Hijri"),
        ["cal.gregorian"] = ("میلادی", "Gregorian"),
    };

    public static string T(string key) => S.TryGetValue(key, out var v) ? (IsFa ? v.Fa : v.En) : key;
}
