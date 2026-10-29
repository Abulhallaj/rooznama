# Changelog

All notable changes to **Rooznama (روزنما)** are documented here.
Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow [Semantic Versioning](https://semver.org/).

## [0.6.0] - Unreleased

### Added
- **Four release downloads** from CI, all self-contained (no .NET or Windows App SDK installation needed): portable ZIP and installer, each for 64-bit (x64) and 32-bit (x86).
- **Installer** (Inno Setup): per-user by default (no admin rights), Start menu entry, optional desktop icon, clean uninstall (also removes the "start with Windows" entry).
- **Portable mode**: a `portable.flag` file next to the exe (included in the portable ZIPs) keeps settings and the log in `.\data` instead of `%AppData%`.
- **Update check**: the app periodically reads the latest GitHub release (at most once a day), shows a tray notification once per new version, and the About and Settings pages show the update with a download link. It can be turned off in Settings and has a "Check now" button. Only the latest release is read; no personal data or usage statistics are sent.
- **Support page** in the side menu with a button linking to https://www.coffeete.ir/abulhallaj, plus a link to star the project; `.github/FUNDING.yml` adds GitHub's Sponsor button.
- **Project and issue links** on the About page.
- **Custom right-to-left tooltip** for the tray icon (themed, shows the Shamsi date and optionally the Gregorian date); it can be switched back to the system tooltip in Settings.
- Settings: "Check for updates automatically", "Custom tooltip", and a "Check now" button.

### Changed
- The app version is no longer hard-coded: CI builds with the version taken from the git tag (`v0.6.0` → `0.6.0`), so the About page and the update check always match the GitHub release. Local builds show `0.0.0-dev`.
- The project no longer pins `x64`: the platform follows the runtime identifier, so `win-x86` and `win-x64` builds both work.
- Settings page: related options are grouped in a grid inside three cards (appearance, tray icon and calendar, updates).
- The release workflow builds both architectures in a matrix and attaches all four files to the GitHub Release when a `v*` tag is pushed.
- "Start with Windows" re-writes the startup entry on every launch, so it keeps working after the exe is moved or updated.

### Fixed
- **Tray day number not updating after a long sleep or hibernate.** The number stayed stale when the PC was resumed days later. Besides the earlier midnight timer and watchdog, the app now also listens for suspend/resume notifications (including Modern Standby), session unlock/connect and clock changes, and runs a thread-pool heartbeat that detects a long gap between beats; every one of these events refreshes the icon and re-arms the timers. All events are written to the log (`rooznama.log`).
- Tray tooltip not being right-to-left (see "Custom right-to-left tooltip").

## [0.5.0] - Initial release

First public version: a Shamsi (Jalali) calendar for the Windows system tray, built with .NET 8 and WinUI 3 (Windows App SDK 1.7).

### Added
- Tray icon showing the Shamsi day number (Persian digits, largest size that fits), tooltip with Shamsi and Gregorian dates, native right-to-left, dark-mode-aware menu, single-instance protection.
- Calendar flyout: borderless acrylic window with rounded corners and an animated slide/fade; header with today's date and a collapse arrow; day, month and year views (click the title); up/down arrows with a sliding grid; dimmed days of adjacent months; Gregorian date on hover.
- Main window: large calendar with the Gregorian day beside each day and a Today button; Shamsi ↔ Gregorian converter with two linked date pickers; settings; about.
- Settings: language (Persian default, English), font (Vazirmatn, Tahoma), theme (automatic, light, dark), icon color, Gregorian date in the tooltip, close on focus loss, start with Windows.
- Extension points: `ICalendarEventProvider` (events list; Google Calendar later) and `DayInfo.Lines()` (Hijri/lunar dates and occasions).
- Diagnostic log and application icon.

### Known limitations
- Language, font and theme changes need a restart.
- The dark tray menu relies on undocumented `uxtheme.dll` entry points.
- Loading fonts from the app folder in an unpackaged WinUI 3 app can be unreliable; installing Vazirmatn on Windows is the most reliable option.
- Dates use .NET's arithmetic `PersianCalendar`, which can differ from the official calendar in some years.
- Official holidays and occasions are not included yet.

### Planned
- Official holidays and occasions from the University of Tehran Institute of Geophysics calendar, as a per-year JSON file with a user override.
- Hijri/lunar dates in the day tooltip.
- Google Calendar integration.
