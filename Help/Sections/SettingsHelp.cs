namespace Rooznama.Help;

public sealed class SettingsHelp : HelpSection
{
    public override string Id => "settings";
    public override int Order => 70;
    public override (string Fa, string En) Title => ("تنظیمات", "Settings");

    public override (string Fa, string En) Body => (
        """
        هر تغییری در تنظیمات همان لحظه ذخیره می‌شود.

        ## ظاهر
        - **زبان:** فارسی یا انگلیسی.
        - **فونت:** فونت برنامه.
        - **تم:** خودکار (مطابق ویندوز)، روشن یا تیره.
        - تغییر زبان، فونت و تم بعد از زدن «راه‌اندازی مجدد» اعمال می‌شود.

        ## آیکون Tray و تقویم
        - **رنگ آیکون:** رنگ عدد روی آیکون.
        - **اجرا هنگام ورود به ویندوز:** برنامه با روشن‌شدن ویندوز شروع می‌شود.
        - **نمایش تاریخ میلادی در Tooltip:** تاریخ میلادی هم زیر تاریخ شمسی نوشته می‌شود.
        - **بستن تقویم با کلیک بیرون از آن:** تقویم کوچک با کلیک بیرون از آن بسته می‌شود.
        - **tooltip راست‌چین اختصاصی روی آیکون:** به‌جای Tooltip معمولی ویندوز یک Tooltip راست‌چین با متن فارسی نشان داده می‌شود.

        ## تقویم (از نسخه ۰.۸.۰)
        - **منبع تاریخ قمری:** مرکز تقویم دانشگاه تهران (رسمی ایران) یا ام‌القری.
        - **نمایش تاریخ قمری در Tooltip**
        - **نمایش روز قمری در خانه‌های تقویم**
        - **نمایش شماره هفته در تقویم:** ستون شماره‌ی هفته در تقویم اصلی (از نسخه ۰.۹.۰).

        ## به‌روزرسانی
        - **بررسی خودکار نسخه جدید:** برنامه آخرین نسخه را از GitHub می‌خواند. اطلاعات شخصی یا آمار استفاده ارسال نمی‌شود.
        - **بررسی اکنون:** همین الان بررسی می‌کند و اگر نسخه جدیدی باشد پیوند دانلود را نشان می‌دهد.
        """,
        """
        Every change in Settings is saved at once.

        ## Appearance
        - **Language:** Persian or English.
        - **Font:** the font of the app.
        - **Theme:** automatic (follow Windows), light or dark.
        - A change of language, font or theme applies after you press "Restart".

        ## Tray icon & calendar
        - **Icon color:** the colour of the number on the icon.
        - **Start with Windows:** the app starts when Windows does.
        - **Show Gregorian date in the tooltip:** the Gregorian date is written under the Shamsi one.
        - **Close the calendar when it loses focus:** the small calendar closes when you click outside it.
        - **Custom right-to-left tooltip on the tray icon:** a right-to-left tooltip with Persian text instead of the plain Windows tooltip.

        ## Calendar (since version 0.8.0)
        - **Hijri date source:** Tehran University Calendar Center (official Iran) or Umm al-Qura.
        - **Show the Hijri date in tooltips**
        - **Show the Hijri day in calendar cells**
        - **Show the week number in the calendar:** the week-number column in the main calendar (since version 0.9.0).

        ## Updates
        - **Check for updates automatically:** the app reads the latest release from GitHub. No personal data or usage statistics are sent.
        - **Check now:** checks right now and shows a download link if there is a newer version.
        """);
}
