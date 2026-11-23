namespace Rooznama.Help;

public sealed class MainCalendarHelp : HelpSection
{
    public override string Id => "main-calendar";
    public override int Order => 30;
    public override (string Fa, string En) Title => ("تقویم در پنجره اصلی", "The calendar in the main window");

    public override (string Fa, string En) Body => (
        """
        صفحه‌ی «تقویم» همان تقویم ماهانه است، بزرگ‌تر و با امکانات بیشتر. این امکانات فقط در تقویم اصلی هستند؛ تقویم کوچک کنار آیکون ساده می‌ماند.

        ## هر خانه
        - عدد بزرگ روز شمسی است. روز میلادی و، اگر «نمایش روز قمری در خانه‌های تقویم» روشن باشد، روز قمری با اعداد کوچک‌تر در همان خانه نوشته می‌شوند؛ روز اول هر ماه با نام ماه.
        - **امروز** با رنگ تأکید و **روز انتخاب‌شده** با کادر مشخص می‌شود.
        - **تعطیلات رسمی** زمینه‌ی قرمز کم‌رنگ و عدد قرمز دارند؛ جمعه‌ها نیز قرمز هستند.

        ## شماره‌ی هفته (از نسخه ۰.۹.۰)
        - در ابتدای هر ردیف شماره‌ی هفته‌ی سال نوشته می‌شود (ستونی با عنوان «هفته»).
        - شمارش ایرانی است: هفته‌ها از شنبه شروع می‌شوند و هفته‌ای که ۱ فروردین در آن است، هفته‌ی ۱ است. اگر آخرین ردیف اسفند شامل ۱ فروردین باشد، همان ردیف هفته‌ی ۱ سال بعد است.
        - با گزینه‌ی «نمایش شماره هفته در تقویم» در تنظیمات (بخش «تقویم») روشن یا خاموش می‌شود.

        ## کار با تقویم
        - روی هر روز کلیک کنید تا مناسبت‌هایش زیر تقویم نمایش داده شود.
        - دکمه‌ی **امروز** شما را به امروز برمی‌گرداند.
        - کلیک روی عنوان نما را از روز به ماه و سال می‌برد و فلش‌های بالا و پایین ماه را عوض می‌کنند.
        - روز را با کلیدهای صفحه‌کلید هم می‌توانید عوض کنید (بخش «کلیدها»).

        > بخش «رویدادها» برای رویدادهای شخصی یا رویدادهای یک تقویم متصل است. در این نسخه چیزی در آن ثبت نمی‌شود و همیشه خالی می‌ماند.
        """,
        """
        The Calendar page is the same monthly calendar, larger and with more features. These features exist only in the main calendar; the small calendar next to the icon stays simple.

        ## Each cell
        - The big number is the Shamsi day. The Gregorian day and, if "Show the Hijri day in calendar cells" is on, the Hijri day are written in smaller numbers in the same cell; the first day of a month with the month name.
        - **Today** is drawn in the accent colour and the **selected day** has a frame.
        - **Official holidays** have a light red background and a red number; Fridays are also red.

        ## Week numbers (since version 0.9.0)
        - At the start of every row the week number of the year is written (a column titled "Wk").
        - The count is the Iranian one: weeks start on Saturday and the week that contains 1 Farvardin is week 1. If the last row of Esfand contains 1 Farvardin, that row is week 1 of the next year.
        - Turn it on or off with "Show the week number in the calendar" in Settings (section "Calendar").

        ## Using it
        - Click a day to list its occasions under the calendar.
        - The **Today** button takes you back to today.
        - Clicking the title switches the view from days to months and years, and the up and down arrows change the month.
        - You can also change the day with the keyboard (see "Keys").

        > The Events box is for personal events or the events of a connected calendar. In this version nothing is recorded there and it stays empty.
        """);
}
