namespace Rooznama.Help;

public sealed class HijriHelp : HelpSection
{
    public override string Id => "hijri";
    public override int Order => 50;
    public override string Since => "0.8.0";
    public override (string Fa, string En) Title => ("تاریخ قمری", "Hijri date");

    public override (string Fa, string En) Body => (
        """
        تاریخ قمری (هجری قمری) هم نمایش داده می‌شود. اعداد قمری با **ارقام عربی** (٠١٢٣٤٥٦٧٨٩) نوشته می‌شوند، نه ارقام فارسی.

        ## کجا دیده می‌شود
        - در Tooltip آیکون، اگر «نمایش تاریخ قمری در Tooltip» روشن باشد.
        - هنگام نگه‌داشتن ماوس روی هر روز (در تقویم کوچک و تقویم اصلی).
        - به‌صورت عدد کوچک در خانه‌های تقویم اصلی، اگر «نمایش روز قمری در خانه‌های تقویم» روشن باشد.
        - در صفحه‌ی «مبدل تاریخ».

        ## منبع تاریخ قمری
        در تنظیمات، بخش «تقویم»، گزینه‌ی «منبع تاریخ قمری»:
        - **مرکز تقویم دانشگاه تهران (رسمی ایران):** تاریخ‌های رسمی ایران که طبق رؤیت هلال تعیین می‌شوند. داده‌ی رسمی برای ۱۲ سال در برنامه است (۱۳۸۴، ۱۳۹۱، ۱۳۹۳، ۱۳۹۵، ۱۳۹۷ تا ۱۴۰۱ و ۱۴۰۳ تا ۱۴۰۵) و آخرین روزش ۲۹ اسفند ۱۴۰۵ (۲۰ مارس ۲۰۲۷) است.
        - **ام‌القری:** تقویم ریاضی عربستان.

        > برای هر روزی که منبع انتخابی داده ندارد، برنامه خودکار از ام‌القری استفاده می‌کند. در این حالت تاریخ ممکن است تا یک روز با تقویم رسمی ایران فرق کند؛ مثلاً برای سال‌های بعد از ۱۴۰۵ یا سال‌هایی که داده‌ی رسمی ندارند.
        """,
        """
        The Hijri (qamari) date is shown too. Hijri numbers are written with **Arabic digits** (٠١٢٣٤٥٦٧٨٩), not Persian ones.

        ## Where you see it
        - In the tray icon tooltip, if "Show the Hijri date in tooltips" is on.
        - When you hold the mouse over a day (in the small and in the main calendar).
        - As a small number in the cells of the main calendar, if "Show the Hijri day in calendar cells" is on.
        - On the Date converter page.

        ## The Hijri source
        In Settings, section "Calendar", the option "Hijri date source":
        - **Tehran University Calendar Center (official Iran):** the official Iranian dates, which follow the sighting of the crescent. Official data for 12 years is built into the app (1384, 1391, 1393, 1395, 1397 to 1401 and 1403 to 1405) and the last official day is 29 Esfand 1405 (20 March 2027).
        - **Umm al-Qura:** the Saudi arithmetic calendar.

        > For every day the selected source has no data for, the app automatically uses Umm al-Qura. There the date can differ from Iran's official calendar by one day, for example for the years after 1405 or the years without official data.
        """);
}
