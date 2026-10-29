<div align="center">

<img src="Assets/AppIcon.png" width="128" alt="Rooznama icon"/>

# روزنما · Rooznama

**تقویم شمسی سبک برای system tray ویندوز**
*A lightweight Shamsi (Jalali) calendar for the Windows system tray*

[![build](https://github.com/Abulhallaj/rooznama/actions/workflows/build.yml/badge.svg)](https://github.com/Abulhallaj/rooznama/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

[فارسی](#فارسی) · [English](#english) · [Changelog](CHANGELOG.md)

</div>

<!-- Add screenshots to docs/screenshots and link them here -->

---

## فارسی

> وضعیت: در حال توسعه. هنوز روی همه نسخه‌های ویندوز آزمایش نشده است.

روزنما روز شمسی را به‌صورت عدد روی آیکون tray نشان می‌دهد؛ با نگه‌داشتن ماوس تاریخ دقیق شمسی و میلادی دیده می‌شود و با کلیک یک تقویم شمسی شبیه تقویم ویندوز ۱۱ باز می‌شود.

### امکانات
- آیکون tray با عدد روز شمسی (رنگ قابل تنظیم) و tooltip شمسی و میلادی
- تقویم بازشونده کنار tray: پس‌زمینه acrylic، انیمیشن، نمای روز / ماه / سال، روزهای ماه قبل و بعد به‌صورت کم‌رنگ، و تاریخ میلادی با هاور روی هر روز
- پنجره اصلی: تقویم بزرگ با روز میلادی کنار هر روز، مبدل تاریخ شمسی ↔ میلادی، تنظیمات، درباره
- فارسی (پیش‌فرض) و انگلیسی، فونت وزیرمتن، تم خودکار / روشن / تیره
- اجرا با ویندوز (اختیاری)

### دانلود
از بخش [Releases](https://github.com/Abulhallaj/rooznama/releases) یکی از چهار فایل را بگیرید (همه بدون نیاز به نصب .NET یا چیز دیگری کار می‌کنند):

| | پرتابل (ZIP) | نصبی (Setup) |
|---|---|---|
| ویندوز ۶۴ بیتی | `Rooznama-<نسخه>-portable-x64.zip` | `Rooznama-<نسخه>-setup-x64.exe` |
| ویندوز ۳۲ بیتی | `Rooznama-<نسخه>-portable-x86.zip` | `Rooznama-<نسخه>-setup-x86.exe` |

- **پرتابل:** ZIP را در هر پوشه‌ای باز کنید و `Rooznama.exe` را اجرا کنید. تنظیمات و لاگ در پوشه `data` کنار برنامه می‌مانند.
- **نصبی:** بدون نیاز به دسترسی ادمین برای کاربر جاری نصب می‌شود و میانبر Start و حذف تمیز دارد.
- آیکون در system tray ظاهر می‌شود (در ویندوز ۱۱ ممکن است داخل overflow باشد؛ به نوار بکشیدش).

### به‌روزرسانی
برنامه به‌صورت دوره‌ای (حداکثر روزی یک بار) آخرین نسخه را از GitHub می‌خواند و در صورت وجود نسخه جدید اعلان می‌دهد. فقط آخرین نسخه خوانده می‌شود و هیچ اطلاعات شخصی یا آماری ارسال نمی‌شود. از تنظیمات قابل خاموش‌کردن است.

### حمایت
روزنما رایگان و متن‌باز است. اگر برایتان مفید بود، می‌توانید از [این لینک](https://www.coffeete.ir/abulhallaj) حمایت کنید (اختیاری است).

### ساخت از سورس
پیش‌نیاز: ویندوز ۱۰ نسخه ۱۸۰۹ یا بالاتر، [.NET 8 SDK](https://dotnet.microsoft.com/download)، و ترجیحاً Visual Studio 2022 با workload «.NET desktop development».

```powershell
git clone https://github.com/Abulhallaj/rooznama.git
cd rooznama
dotnet run
# نسخه خودکفا (۶۴ بیتی؛ برای ۳۲ بیتی win-x86 و -p:PlatformTarget=x86):
dotnet publish -c Release -r win-x64 --self-contained -p:WindowsAppSDKSelfContained=true -o publish
```

اگر بعد از جایگزینی فایل‌ها خطای `XamlCompiler` دیدید، پوشه‌های `bin` و `obj` را پاک کنید.

### فونت وزیرمتن
فایل‌های [Vazirmatn](https://github.com/rastikerdar/vazirmatn) (`Vazirmatn-Regular.ttf` و `Vazirmatn-Bold.ttf` و `OFL.txt`) را در `Assets/Fonts` بگذارید، یا فونت را روی ویندوز نصب کنید (مطمئن‌ترین راه). در صفحه «درباره» فونتِ اعمال‌شده دیده می‌شود. بدون آن، از Segoe UI استفاده می‌شود.

### داده‌ها و لاگ
- نصبی: `%AppData%\Rooznama\settings.json` و `rooznama.log`
- پرتابل: پوشه `data` کنار برنامه

### نقشه راه
مناسبت‌ها و تعطیلات رسمی (از تقویم رسمی کشور)، تاریخ قمری، اتصال به Google Calendar.

---

## English

> Status: early development. Not yet tested on every Windows build.

Rooznama shows the Shamsi (Jalali) day number on a system-tray icon. Hover for the exact Shamsi and Gregorian date; click for a Windows 11–style Shamsi calendar flyout.

### Features
- Tray icon with the Shamsi day number (configurable color) and a Shamsi + Gregorian tooltip
- Calendar flyout next to the tray: acrylic background, animations, day / month / year views, dimmed days of adjacent months, Gregorian date on hover
- Main window: large calendar with the Gregorian day beside each day, Shamsi ↔ Gregorian converter, settings, about
- Persian (default) and English, Vazirmatn font, automatic / light / dark theme
- Optional start with Windows

### Download
Get one of the four files from [Releases](https://github.com/Abulhallaj/rooznama/releases). All of them are self-contained: no .NET or other prerequisites to install.

| | Portable (ZIP) | Installer |
|---|---|---|
| Windows 64-bit | `Rooznama-<version>-portable-x64.zip` | `Rooznama-<version>-setup-x64.exe` |
| Windows 32-bit | `Rooznama-<version>-portable-x86.zip` | `Rooznama-<version>-setup-x86.exe` |

- **Portable:** extract anywhere and run `Rooznama.exe`. Settings and the log stay in the `data` folder next to the app.
- **Installer:** per-user install without admin rights, Start menu entry, clean uninstall.
- The icon appears in the system tray (on Windows 11 it may be in the overflow area — drag it to the bar).

### Updates
The app periodically (at most once a day) reads the latest GitHub release and shows a notification when a newer version exists. Only the latest release is read; no personal data or usage statistics are sent. It can be turned off in Settings.

### Support
Rooznama is free and open source. If it's useful to you, you can [support it here](https://www.coffeete.ir/abulhallaj) (optional).

### Build from source
Requirements: Windows 10 1809+ (best on Windows 11), [.NET 8 SDK](https://dotnet.microsoft.com/download), preferably Visual Studio 2022 with the ".NET desktop development" workload.

```powershell
git clone https://github.com/Abulhallaj/rooznama.git
cd rooznama
dotnet run
# self-contained build (64-bit; for 32-bit use win-x86 and -p:PlatformTarget=x86):
dotnet publish -c Release -r win-x64 --self-contained -p:WindowsAppSDKSelfContained=true -o publish
```

If you get `XamlCompiler` errors after replacing files, delete the `bin` and `obj` folders.

### Vazirmatn font
Put [Vazirmatn](https://github.com/rastikerdar/vazirmatn) (`Vazirmatn-Regular.ttf`, `Vazirmatn-Bold.ttf`, `OFL.txt`) in `Assets/Fonts`, or install the font on Windows (the most reliable option). The About page shows the font string that was applied. Without it the app falls back to Segoe UI.

### Data and logs
- Installed: `%AppData%\Rooznama\settings.json` and `rooznama.log`
- Portable: the `data` folder next to the app

### Project layout
```
App.xaml(.cs)            app startup, theme, font, single instance
FlyoutWindow             tray calendar flyout
ShellWindow              main window (navigation)
Views/                   CalendarPanel, CalendarPage, ConverterPage, SettingsPage, SupportPage, AboutPage
Services/                TrayService (Shell_NotifyIcon), PersianDate, Loc (fa/en), AppSettings,
                         UpdateService, AppInfo, AppPaths, DayInfo, CalendarEvents (provider hook), Log
TipWindow                right-to-left tray tooltip
installer/               Inno Setup script (used by CI)
Assets/                  icons, fonts
```

### Roadmap
Official holidays and occasions, Hijri/lunar dates, Google Calendar integration.

### License
[MIT](LICENSE). Third-party components: [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
