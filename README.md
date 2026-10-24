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

> وضعیت: در حال توسعه (نسخه ۰٫۵٫۰). هنوز روی همه نسخه‌های ویندوز آزمایش نشده است.

روزنما روز شمسی را به‌صورت عدد روی آیکون tray نشان می‌دهد؛ با نگه‌داشتن ماوس تاریخ دقیق شمسی و میلادی دیده می‌شود و با کلیک یک تقویم شمسی شبیه تقویم ویندوز ۱۱ باز می‌شود.

### امکانات
- آیکون tray با عدد روز شمسی (رنگ قابل تنظیم) و tooltip شمسی و میلادی
- تقویم بازشونده کنار tray: پس‌زمینه acrylic، انیمیشن، نمای روز / ماه / سال، روزهای ماه قبل و بعد به‌صورت کم‌رنگ، و تاریخ میلادی با هاور روی هر روز
- پنجره اصلی: تقویم بزرگ با روز میلادی کنار هر روز، مبدل تاریخ شمسی ↔ میلادی، تنظیمات، درباره
- فارسی (پیش‌فرض) و انگلیسی، فونت وزیرمتن، تم خودکار / روشن / تیره
- اجرا با ویندوز (اختیاری)

### اجرا
1. از بخش [Releases](https://github.com/Abulhallaj/rooznama/releases) فایل ZIP را بگیرید و در یک پوشه باز کنید.
2. `Rooznama.exe` را اجرا کنید. آیکون در system tray ظاهر می‌شود (در ویندوز ۱۱ ممکن است داخل overflow باشد؛ به نوار بکشیدش).

### ساخت از سورس
پیش‌نیاز: ویندوز ۱۰ نسخه ۱۸۰۹ یا بالاتر، [.NET 8 SDK](https://dotnet.microsoft.com/download)، و ترجیحاً Visual Studio 2022 با workload «.NET desktop development».

```powershell
git clone https://github.com/Abulhallaj/rooznama.git
cd rooznama
dotnet run
# نسخه پرتابل:
dotnet publish -c Release -r win-x64 --self-contained -p:WindowsAppSDKSelfContained=true -o publish
```

اگر بعد از جایگزینی فایل‌ها خطای `XamlCompiler` دیدید، پوشه‌های `bin` و `obj` را پاک کنید.

### فونت وزیرمتن
فایل‌های [Vazirmatn](https://github.com/rastikerdar/vazirmatn) (`Vazirmatn-Regular.ttf` و `Vazirmatn-Bold.ttf` و `OFL.txt`) را در `Assets/Fonts` بگذارید، یا فونت را روی ویندوز نصب کنید (مطمئن‌ترین راه). در صفحه «درباره» فونتِ اعمال‌شده دیده می‌شود. بدون آن، از Segoe UI استفاده می‌شود.

### داده‌ها و لاگ
- تنظیمات: `%AppData%\Rooznama\settings.json`
- لاگ: `%AppData%\Rooznama\rooznama.log`

### نقشه راه
مناسبت‌ها و تعطیلات رسمی (از تقویم رسمی کشور)، تاریخ قمری، نسخه پرتابل و installer، اتصال به Google Calendar.

---

## English

> Status: early development (0.5.0). Not yet tested on every Windows build.

Rooznama shows the Shamsi (Jalali) day number on a system-tray icon. Hover for the exact Shamsi and Gregorian date; click for a Windows 11–style Shamsi calendar flyout.

### Features
- Tray icon with the Shamsi day number (configurable color) and a Shamsi + Gregorian tooltip
- Calendar flyout next to the tray: acrylic background, animations, day / month / year views, dimmed days of adjacent months, Gregorian date on hover
- Main window: large calendar with the Gregorian day beside each day, Shamsi ↔ Gregorian converter, settings, about
- Persian (default) and English, Vazirmatn font, automatic / light / dark theme
- Optional start with Windows

### Run
1. Download the ZIP from [Releases](https://github.com/Abulhallaj/rooznama/releases) and extract it.
2. Run `Rooznama.exe`. The icon appears in the system tray (on Windows 11 it may be in the overflow area — drag it to the bar).

### Build from source
Requirements: Windows 10 1809+ (best on Windows 11), [.NET 8 SDK](https://dotnet.microsoft.com/download), preferably Visual Studio 2022 with the ".NET desktop development" workload.

```powershell
git clone https://github.com/Abulhallaj/rooznama.git
cd rooznama
dotnet run
# portable build:
dotnet publish -c Release -r win-x64 --self-contained -p:WindowsAppSDKSelfContained=true -o publish
```

If you get `XamlCompiler` errors after replacing files, delete the `bin` and `obj` folders.

### Vazirmatn font
Put [Vazirmatn](https://github.com/rastikerdar/vazirmatn) (`Vazirmatn-Regular.ttf`, `Vazirmatn-Bold.ttf`, `OFL.txt`) in `Assets/Fonts`, or install the font on Windows (the most reliable option). The About page shows the font string that was applied. Without it the app falls back to Segoe UI.

### Data and logs
- Settings: `%AppData%\Rooznama\settings.json`
- Log: `%AppData%\Rooznama\rooznama.log`

### Project layout
```
App.xaml(.cs)            app startup, theme, font, single instance
FlyoutWindow             tray calendar flyout
ShellWindow              main window (navigation)
Views/                   CalendarPanel, CalendarPage, ConverterPage, SettingsPage, AboutPage
Services/                TrayService (Shell_NotifyIcon), PersianDate, Loc (fa/en),
                         AppSettings, DayInfo, CalendarEvents (provider hook), Log
Assets/                  icons, fonts
```

### Roadmap
Official holidays and occasions, Hijri/lunar dates, portable ZIP and installer builds, Google Calendar integration.

### License
[MIT](LICENSE). Third-party components: [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
