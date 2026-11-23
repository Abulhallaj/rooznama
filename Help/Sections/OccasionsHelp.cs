namespace Rooznama.Help;

public sealed class OccasionsHelp : HelpSection
{
    public override string Id => "occasions";
    public override int Order => 40;
    public override (string Fa, string En) Title => ("تعطیلات و مناسبت‌ها", "Holidays and occasions");

    public override (string Fa, string En) Body => (
        """
        روزنما تعطیلات رسمی و مناسبت‌های هر روز را کاملاً آفلاین نشان می‌دهد، از دو منبع:

        - **داده‌ی سالانه‌ی رسمی** برای سال‌های **۱۳۸۳ تا ۱۴۰۵**، بر پایه‌ی تقویم مرکز تقویم دانشگاه تهران. این داده همیشه اولویت دارد.
        - **مناسبت‌های ثابت هر تقویم** (از نسخه ۰.۹.۰) که جای خالی داده را پر می‌کنند، مثلاً برای سال‌های بعد از ۱۴۰۵.

        ## برچسب‌ها
        - **تعطیل رسمی:** این روزها قرمزند و در فهرست زیر تقویم این برچسب را دارند؛ در تقویم اصلی زمینه‌ی قرمز کم‌رنگ هم دارند.
        - **مناسبت:** روزهای ویژه‌ی غیرتعطیل (مثل روزهای جهانی).
        - **تقریبی:** کنار برچسب بعضی تعطیلات قمری می‌آید (مثلاً «تعطیل رسمی · تقریبی»). یعنی تاریخ قمری آن روز از ام‌القری آمده، نه از تقویم رسمی ایران، و ممکن است یک روز جابه‌جا باشد.

        ## مناسبت‌های ثابت
        - **شمسی (۱۰ تعطیلی):** نوروز (چهار روز)، ۱۲ و ۱۳ فروردین، ۱۴ و ۱۵ خرداد، ۲۲ بهمن و ۲۹ اسفند.
        - **قمری (۱۷ تعطیلی مذهبی):** تاسوعا و عاشورا، اربعین، رحلت پیامبر و شهادت امام حسن، شهادت امام رضا (آخر صفر)، شهادت امام حسن عسکری، میلاد پیامبر و امام صادق، شهادت حضرت فاطمه، ولادت امام علی، مبعث، نیمه شعبان، شهادت امام علی، عید فطر (دو روز)، شهادت امام صادق، عید قربان و عید غدیر.
        - **میلادی (۴ مناسبت):** ۱ ژانویه، ۸ مارس (روز جهانی زنان)، ۲۱ سپتامبر (روز جهانی صلح) و ۲۵ دسامبر (کریسمس).

        > برای سال‌های بعد از ۱۴۰۵ فقط همین مناسبت‌های ثابت نشان داده می‌شوند؛ بقیه‌ی مناسبت‌ها (مثل بیشتر روزهای جهانی) داده‌ی سالانه می‌خواهند و تا ۱۴۰۵ هستند.
        """,
        """
        Rooznama shows the official holidays and the occasions of every day entirely offline, from two sources:

        - **The official yearly data** for the years **1383 to 1405**, based on the calendar of the Tehran University Calendar Center. It always has priority.
        - **Fixed occasions per calendar** (since version 0.9.0), which fill the gaps, for example for the years after 1405.

        ## Labels
        - **Official holiday:** these days are red and carry this label in the list under the calendar; in the main calendar they also have a light red background.
        - **Observance:** other special days (international days, for example).
        - **approximate:** appears next to the label of some Hijri holidays (for example "Official holiday · approximate"). It means the Hijri date of that day came from Umm al-Qura, not from Iran's official calendar, and may be a day off.

        ## The fixed occasions
        - **Shamsi (10 holidays):** Nowruz (four days), 12 and 13 Farvardin, 14 and 15 Khordad, 22 Bahman and 29 Esfand.
        - **Hijri (17 religious holidays):** Tasua and Ashura, Arbaeen, the demise of the Prophet and the martyrdom of Imam Hassan, the martyrdom of Imam Reza (end of Safar), the martyrdom of Imam Hassan Askari, the birthday of the Prophet and Imam Sadiq, the martyrdom of Fatimah, the birthday of Imam Ali, Mab'ath, Mid-Sha'ban, the martyrdom of Imam Ali, Eid al-Fitr (two days), the martyrdom of Imam Sadiq, Eid al-Adha and Eid al-Ghadir.
        - **Gregorian (4 observances):** 1 January, 8 March (International Women's Day), 21 September (International Day of Peace) and 25 December (Christmas).

        > For the years after 1405 only these fixed occasions are shown; the other occasions (most international days, for example) need the yearly data, which ends with 1405.
        """);
}
