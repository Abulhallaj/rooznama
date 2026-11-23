namespace Rooznama.Help;

public sealed class FlyoutHelp : HelpSection
{
    public override string Id => "flyout";
    public override int Order => 20;
    public override (string Fa, string En) Title => ("تقویم کوچک کنار آیکون", "The small calendar next to the icon");

    public override (string Fa, string En) Body => (
        """
        با کلیک چپ روی آیکون یک تقویم ماهانه کنار آن باز می‌شود. بالای آن تاریخ امروز (شمسی و میلادی) نوشته شده و دکمه‌ی فلش کنارش تقویم را کوچک یا بزرگ می‌کند؛ برنامه یادش می‌ماند که آخر بار چه حالتی بود.

        ## جابه‌جایی
        - **فلش‌های بالا و پایین** ماه قبل و ماه بعد را نشان می‌دهند.
        - **کلیک روی عنوان** (نام ماه و سال) نما را از روزها به ماه‌ها و بعد به سال‌ها می‌برد. با انتخاب یک ماه یا سال به نمای روزها برمی‌گردید.
        - روزهای ماه قبل و بعد کم‌رنگ دیده می‌شوند.

        ## روزها
        - ماوس را روی هر روز نگه دارید تا تاریخ میلادی (و قمری) و مناسبت‌های آن روز را در یک راهنمای کوچک ببینید.
        - جمعه‌ها و تعطیلات رسمی قرمز هستند. زیر تقویم، مناسبت‌های روز انتخاب‌شده فهرست می‌شود.

        ## بستن
        کلید **Esc** یا کلیک بیرون از تقویم آن را می‌بندد. اگر نمی‌خواهید با کلیک بیرون بسته شود، در تنظیمات گزینه‌ی «بستن تقویم با کلیک بیرون از آن» را خاموش کنید.

        > این تقویم عمداً ساده مانده است. شماره‌ی هفته، روز قمری در خانه‌ها و زمینه‌ی قرمز تعطیلات فقط در تقویم اصلی (پنجره‌ی اصلی) هستند.
        """,
        """
        A left click on the icon opens a monthly calendar next to it. At the top it shows today's date (Shamsi and Gregorian) and an arrow button that collapses or expands the calendar; the app remembers which state you left it in.

        ## Moving around
        - **The up and down arrows** show the previous and the next month.
        - **Clicking the title** (month and year) switches the view from days to months and then to years. Choosing a month or a year takes you back to the days.
        - Days of the previous and the next month are shown dimmed.

        ## Days
        - Hold the mouse over a day to see its Gregorian (and Hijri) date and its occasions in a small hint.
        - Fridays and official holidays are red. Under the calendar the occasions of the selected day are listed.

        ## Closing
        The **Esc** key or a click outside the calendar closes it. If you do not want it to close when you click outside, turn off "Close the calendar when it loses focus" in Settings.

        > This calendar is kept simple on purpose. The week numbers, the Hijri day in the cells and the red background of holidays are only in the main calendar (the main window).
        """);
}
