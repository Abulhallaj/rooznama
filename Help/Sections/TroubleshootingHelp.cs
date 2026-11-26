namespace Rooznama.Help;

public sealed class TroubleshootingHelp : HelpSection
{
    public override string Id => "troubleshooting";
    public override int Order => 90;
    public override (string Fa, string En) Title => ("رفع اشکال و پرسش‌ها", "Troubleshooting and questions");

    public override (string Fa, string En) Body => (
        """
        - **آیکون را نمی‌بینم:** در ویندوز ۱۱ آیکون ممکن است داخل «آیکون‌های پنهان» باشد. آن را به نوار وظیفه بکشید.
        - **زبان، فونت یا تم عوض نشد:** این‌ها بعد از «راه‌اندازی مجدد» (در همان بخش تنظیمات) اعمال می‌شوند.
        - **تاریخ قمری یک روز با جای دیگری فرق دارد:** در تنظیمات، «منبع تاریخ قمری» را روی «مرکز تقویم دانشگاه تهران (رسمی ایران)» بگذارید. برای روزهایی که این منبع داده ندارد (از ۲۰ مارس ۲۰۲۷ به بعد) ام‌القری به‌کار می‌رود و ممکن است یک روز فرق کند.
        - **برای سال‌های بعد از ۱۴۰۵ فقط بعضی مناسبت‌ها را می‌بینم:** داده‌ی سالانه‌ی مناسبت‌ها تا پایان ۱۴۰۵ است. بعد از آن فقط مناسبت‌های ثابت (۱۰ تعطیلی شمسی، ۱۷ تعطیلی قمری و ۴ مناسبت میلادی) نشان داده می‌شوند و تعطیلات قمری برچسب «تقریبی» می‌گیرند.
        - **ستون شماره‌ی هفته را نمی‌بینم:** فقط در تقویم اصلی است و با «نمایش شماره هفته در تقویم» در تنظیمات (بخش «تقویم») روشن می‌شود.
        - **فلش‌های صفحه‌کلید در تقویم اصلی کار نمی‌کنند:** یک بار روی تقویم کلیک کنید تا صفحه‌کلید به آن برسد.
        - **تنظیمات و گزارش کجا ذخیره می‌شود؟** در پوشه‌ی `%AppData%\Rooznama`. اگر فایل `portable.flag` کنار برنامه باشد (حالت پرتابل)، در پوشه‌ی `data` کنار برنامه. فایل `rooznama.log` همان‌جاست.
        - **نسخه جدید آمده ولی اطلاع نمی‌دهد:** در تنظیمات، بخش «به‌روزرسانی»، «بررسی اکنون» را بزنید. نسخه‌ی پایدار نسخه‌های پیش‌انتشار (Pre-release) را اعلام نمی‌کند؛ نسخه‌ی بتا آن‌ها را هم می‌بیند.
        - **مشکل یا پیشنهادی دارم:** در صفحه‌ی «درباره» گزینه‌ی «گزارش مشکل یا پیشنهاد» را بزنید.
        """,
        """
        - **I cannot see the icon:** on Windows 11 the icon may be in the hidden icons. Drag it onto the taskbar.
        - **The language, font or theme did not change:** they apply after "Restart" (in the same part of Settings).
        - **The Hijri date differs by a day from another source:** in Settings set "Hijri date source" to "Tehran University Calendar Center (official Iran)". For the days that source has no data for (from 20 March 2027 on) Umm al-Qura is used and can differ by a day.
        - **For the years after 1405 I see only some occasions:** the yearly occasion data ends with 1405. After that only the fixed occasions are shown (10 Shamsi holidays, 17 Hijri holidays and 4 Gregorian observances) and Hijri holidays get the label "approximate".
        - **I do not see the week-number column:** it exists only in the main calendar and is turned on with "Show the week number in the calendar" in Settings (section "Calendar").
        - **The arrow keys do nothing in the main calendar:** click the calendar once so that the keyboard reaches it.
        - **Where are the settings and the log stored?** In the folder `%AppData%\Rooznama`. If a file `portable.flag` sits next to the app (portable mode), in the folder `data` next to the app. The file `rooznama.log` is there too.
        - **A new version came out but I was not told:** in Settings, section "Updates", press "Check now". A stable version does not announce pre-releases; a beta version sees them too.
        - **I have a problem or a suggestion:** on the About page use "Report an issue / suggest".
        """);
}
