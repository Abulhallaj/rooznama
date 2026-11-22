namespace Rooznama.Help;

public sealed class StartHelp : HelpSection
{
    public override string Id => "start";
    public override int Order => 10;
    public override (string Fa, string En) Title => ("شروع سریع", "Getting started");

    public override (string Fa, string En) Body => (
        """
        روزنما در نوار وظیفه (system tray) می‌نشیند و روز شمسی را به‌صورت عدد روی آیکونش نشان می‌دهد.

        ## با آیکون چه می‌توان کرد
        - **ماوس را روی آیکون نگه دارید:** تاریخ شمسی و میلادی (و اگر روشن باشد، قمری) را می‌بینید.
        - **کلیک چپ:** تقویم کوچک کنار آیکون باز یا بسته می‌شود.
        - **کلیک راست:** منویی با «تقویم»، «مبدل تاریخ»، «تنظیمات»، «درباره»، «راهنما» و «خروج» باز می‌شود.

        ## پنجره اصلی
        گزینه‌های منو پنجره اصلی را باز می‌کنند. صفحه‌های آن: تقویم، مبدل تاریخ، حمایت، تنظیمات، راهنما و درباره. در پنجره اصلی کلید **F1** همین راهنما را باز می‌کند.

        > در ویندوز ۱۱ ممکن است آیکون داخل «آیکون‌های پنهان» (فلش کنار ساعت) باشد. آن را به نوار وظیفه بکشید تا همیشه دیده شود.
        """,
        """
        Rooznama lives in the system tray and shows the Shamsi (Jalali) day of the month as a number on its icon.

        ## What you can do with the icon
        - **Hold the mouse over the icon:** you see the Shamsi and Gregorian date (and the Hijri date, if it is turned on).
        - **Left click:** the small calendar next to the icon opens or closes.
        - **Right click:** a menu with Calendar, Date converter, Settings, About, Help and Exit.

        ## The main window
        The menu entries open the main window. Its pages are Calendar, Date converter, Support, Settings, Help and About. In the main window the **F1** key opens this help.

        > On Windows 11 the icon may sit in the hidden icons (the arrow next to the clock). Drag it onto the taskbar to keep it visible.
        """);
}
