namespace Rooznama.Help;

public sealed class ConverterHelp : HelpSection
{
    public override string Id => "converter";
    public override int Order => 60;
    public override (string Fa, string En) Title => ("مبدل تاریخ", "Date converter");

    public override (string Fa, string En) Body => (
        """
        صفحه‌ی «مبدل تاریخ» سه تاریخ را کنار هم نگه می‌دارد: شمسی، میلادی و قمری.

        - هر کدام را با انتخاب‌گر تاریخ عوض کنید، دو تای دیگر خودکار تغییر می‌کنند.
        - زیر انتخاب‌گرها تاریخ به‌صورت عددی (مثلاً ۱۴۰۵/۰۸/۲۷) نوشته می‌شود و دکمه‌ی **کپی** آن را در کلیپ‌بورد می‌گذارد.
        - در انتخاب‌گرها فقط روزهای ماه جاری دیده می‌شوند.

        ## تاریخ قمری در مبدل (از نسخه ۰.۸.۰)
        انتخاب‌گر قمری طوری تنظیم می‌شود که همان تاریخ منبع انتخاب‌شده در تنظیمات (رسمی ایران یا ام‌القری) را نشان بدهد. خط عددی زیر آن همیشه تاریخ دقیق است.

        > اگر تاریخ قمری انتخاب‌شده در منبع شما وجود نداشته باشد (مثلاً روز سی‌ام ماهی که ۲۹ روزه است)، پیام «تاریخ قمری نامعتبر» دیده می‌شود و تاریخ‌های دیگر تغییر نمی‌کنند. بعد از سال ۲۰۷۷ میلادی تاریخ قمری نشان داده نمی‌شود.
        """,
        """
        The Date converter page keeps three dates side by side: Shamsi, Gregorian and Hijri.

        - Change any of them with its date picker and the other two follow.
        - Under the pickers the date is written as numbers (for example 1405/08/27) and the **Copy** button puts it on the clipboard.
        - The pickers show only the days of the current month.

        ## The Hijri date in the converter (since version 0.8.0)
        The Hijri picker is set so that it shows the date of the source chosen in Settings (official Iran or Umm al-Qura). The numeric line under it is always the exact date.

        > If the Hijri date you pick does not exist in your source (for example the 30th of a month that has 29 days), the message "Invalid Hijri date" appears and the other dates stay as they are. After the year 2077 (Gregorian) no Hijri date is shown.
        """);
}
