namespace Rooznama.Help;

public sealed class OccasionsHelp : HelpSection
{
    public override string Id => "occasions";
    public override int Order => 40;
    public override (string Fa, string En) Title => ("تعطیلات و مناسبت‌ها", "Holidays and occasions");

    public override (string Fa, string En) Body => (
        """
        روزنما تعطیلات رسمی و مناسبت‌های هر روز را کاملاً آفلاین نشان می‌دهد.

        - **تعطیل رسمی:** این روزها قرمز می‌شوند و در فهرست زیر تقویم برچسب «تعطیل رسمی» دارند.
        - **مناسبت:** روزهای ویژه‌ی دیگر (مثل روزهای جهانی و مناسبت‌های مذهبی غیرتعطیل) با برچسب «مناسبت» فهرست می‌شوند.
        - داده برای سال‌های **۱۳۸۳ تا ۱۴۰۵** است و بر پایه‌ی تقویم رسمی مرکز تقویم دانشگاه تهران ساخته شده است.

        > برای سال‌های بعد از ۱۴۰۵ در این نسخه داده‌ای نیست: جمعه‌ها همچنان قرمزند، ولی تعطیلات رسمی نشان داده نمی‌شوند.
        """,
        """
        Rooznama shows the official holidays and the occasions of every day entirely offline.

        - **Official holiday:** these days are red and carry the label "Official holiday" in the list under the calendar.
        - **Observance:** other special days (international days, religious observances that are not holidays) are listed with the label "Observance".
        - The data covers the years **1383 to 1405** and is based on the official calendar of the Tehran University Calendar Center.

        > For years after 1405 this version has no data: Fridays are still red, but official holidays are not shown.
        """);
}
