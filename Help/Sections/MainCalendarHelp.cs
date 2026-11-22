namespace Rooznama.Help;

public sealed class MainCalendarHelp : HelpSection
{
    public override string Id => "main-calendar";
    public override int Order => 30;
    public override (string Fa, string En) Title => ("تقویم در پنجره اصلی", "The calendar in the main window");

    public override (string Fa, string En) Body => (
        """
        صفحه‌ی «تقویم» همان تقویم ماهانه را بزرگ‌تر نشان می‌دهد.

        ## هر خانه
        - عدد بزرگ روز شمسی است و زیر آن روز میلادی می‌آید (روز اول ماه با نام ماه).
        - اگر «نمایش روز قمری در خانه‌های تقویم» روشن باشد، روز قمری هم نمایش داده می‌شود؛ روز اول ماه قمری با نام ماه (از نسخه ۰.۸.۰).
        - امروز با رنگ تأکید، روز انتخاب‌شده با کادر، و جمعه‌ها و تعطیلات رسمی با رنگ قرمز مشخص می‌شوند.

        ## کار با تقویم
        - روی هر روز کلیک کنید تا مناسبت‌های آن زیر تقویم نمایش داده شود.
        - دکمه‌ی **امروز** شما را به امروز برمی‌گرداند.
        - کلیک روی عنوان نما را از روز به ماه و سال می‌برد و فلش‌های بالا و پایین ماه را عوض می‌کنند.

        > بخش «رویدادها» برای رویدادهای شخصی یا رویدادهای یک تقویم متصل است. در این نسخه چیزی در آن ثبت نمی‌شود و همیشه خالی می‌ماند.
        """,
        """
        The Calendar page shows the same monthly calendar, larger.

        ## Each cell
        - The big number is the Shamsi day; the Gregorian day sits under it (the first of a month with the month name).
        - If "Show the Hijri day in calendar cells" is on, the Hijri day is shown too; the first day of a Hijri month with the month name (since version 0.8.0).
        - Today is drawn in the accent colour, the selected day has a frame, and Fridays and official holidays are red.

        ## Using it
        - Click a day to list its occasions under the calendar.
        - The **Today** button takes you back to today.
        - Clicking the title switches the view from days to months and years, and the up and down arrows change the month.

        > The Events box is for personal events or the events of a connected calendar. In this version nothing is recorded there and it stays empty.
        """);
}
