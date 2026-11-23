namespace Rooznama.Help;

public sealed class KeysHelp : HelpSection
{
    public override string Id => "keys";
    public override int Order => 35;
    public override string Since => "0.9.0";
    public override (string Fa, string En) Title => ("کلیدها", "Keys");

    public override (string Fa, string En) Body => (
        """
        ## در تقویم اصلی
        - **فلش راست و چپ:** یک روز به عقب و به جلو. چون تقویم راست‌به‌چپ است، فلش راست روز قبل و فلش چپ روز بعد را انتخاب می‌کند (در حالت انگلیسی برعکس).
        - **فلش بالا و پایین:** یک هفته به عقب و به جلو.
        - **PageUp و PageDown:** یک ماه به عقب و به جلو. شماره‌ی روز می‌ماند، یا اگر ماه مقصد کوتاه‌تر باشد آخرین روز آن ماه انتخاب می‌شود.
        - وقتی با فلش‌ها از ماه نمایش‌داده‌شده بیرون بروید، ماه قبل یا بعد خودکار باز می‌شود.
        - هنگام باز شدن تقویم اصلی صفحه‌کلید آماده است؛ اگر کار نکرد یک بار روی تقویم کلیک کنید.

        ## جاهای دیگر
        - **F1:** باز کردن این راهنما (در پنجره‌ی اصلی).
        - **Esc:** بستن تقویم کوچک کنار آیکون.
        """,
        """
        ## In the main calendar
        - **Right and left arrow:** one day back and forward. The calendar runs right to left, so the right arrow selects the previous day and the left arrow the next one (the other way round in English).
        - **Up and down arrow:** one week back and forward.
        - **PageUp and PageDown:** one month back and forward. The day number stays, or the last day of the target month is chosen if that month is shorter.
        - When the arrows take you out of the month on display, the previous or the next month opens by itself.
        - The keyboard is ready when the main calendar opens; if it does not react, click the calendar once.

        ## Elsewhere
        - **F1:** opens this help (in the main window).
        - **Esc:** closes the small calendar next to the icon.
        """);
}
