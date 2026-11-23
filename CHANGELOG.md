# Changelog

All notable changes to **Rooznama (روزنما)** are documented here.
Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow [Semantic Versioning](https://semver.org/).

## Unreleased

## [0.10.0] - 2026-11-23
### Added
- **In-app help**: a Help page with expandable topics and a search box (Persian and English), opened from the navigation, the tray menu or the **F1** key. Topics: getting started, the small calendar, the main calendar, keys, holidays and occasions, Hijri date, the date converter, settings and troubleshooting. Each topic is one small class in `Help/Sections`.
- Tests for the help markup, the topics and the search.

### Changed
- The About item uses an info icon, since the question mark now belongs to Help.

## [0.9.0] - 2026-11-21
### Added
- **Fixed occasions per calendar**: `calendar/rules/fixed-jalali.json` (existing), new `fixed-hijri.json` (the 17 official religious holidays; `day: -1` is the last day of the month) and `fixed-gregorian.json` (New Year's Day, 8 March, 21 September, Christmas). Rules fill in wherever the yearly dataset has nothing, for example Nowruz and the religious holidays of 1406 onwards; Hijri dates that did not come from the official data are labelled *approximate*. The yearly dataset always wins and nothing is listed twice. See `calendar/RULES.md`.
- **Rules vs. dataset comparison** (`CalendarRuleAudit`, run by the tests): for every year the official Hijri data covers, all fixed rules agree with the dataset. The only differences are in 1384, where the dataset places Arbaeen and Eid al-Adha one day before the official qamari date.
- **Week numbers** in the main calendar, in a column in front of the first (Saturday) column, by the Iranian rule (the week with 1 Farvardin is week 1). Turn it off in Settings.
- **Arrow-key navigation** in the main calendar: arrow keys move the selected day (←/→ one day, ↑/↓ one week, in the direction of the right-to-left grid; the next or previous month opens by itself), PageUp/PageDown change the month.
- **Holiday background** in the main calendar: holidays get a light red tile that is readable on the light and the dark theme.
- Tests for the fixed rules, their link to the dataset, the occasion merge, the audit and the week numbers.

### Changed
- **The main calendar and the tray flyout are separate controls.** `MainCalendarView` (new) is the main calendar page and carries the new features; `CalendarPanel` is now the flyout only and looks and behaves as before. The sizes, weights and places of the Shamsi, Gregorian and Hijri numbers in the main calendar's cells are plain constants in `Views/MainCalendarMetrics.cs`.
- Occasions come from `CalendarOccasionService` (yearly dataset + fixed rules), so the flyout also shows the fixed occasions of years without dataset.
- `calendar/schema.json` describes the rule calendars, `aliases` and `day: -1`.

## [0.8.2] - 2026-11-20
Version 0.8.1 was not released; its changes are included in 0.8.2.

### Changed
- **Converter:** the Hijri date is now chosen with a date picker, like the Shamsi and Gregorian ones (it replaces the year, month and day boxes). The picker counts days by Umm al-Qura but is set so that it shows the date of the selected Hijri source, which keeps it in agreement with the rest of the app; the line under it always gives the exact date. An official 30th day in a month that Umm al-Qura counts as 29 days is shown only in that line.

## [0.8.0] - 2026-11-11
### Added
- **Hijri (qamari) dates.** Shown in the day hover text and the tray tooltip (Settings: "Show the Hijri date in tooltips") and as a small day in the large calendar's cells, with the month name on the 1st (Settings: "Show the Hijri day in calendar cells"). Hijri numbers use **Arabic digits** (٠١٢٣…), not Persian ones.
- **Hijri source choice** in Settings > Calendar: *Tehran University Calendar Center* (official Iranian dates, `calendar/hijri/ut.json`) or *Umm al-Qura* (`UmAlQuraCalendar`). **Umm al-Qura is used automatically for any day the selected source does not cover**, so every date always has a Hijri date. `calendar/sources/extract-hijri-anchors.mjs` builds the official data from the Tehran University yearly calendars; it ships with 12 verified Jalali years (1384, 1391, 1393, 1395, 1397-1401, 1403-1405). Years whose text fails the checks are skipped, and five unreadable rows were repaired where the neighbouring Hijri days force the value (`repairedDays`). Adding a year only needs its `Calendar-<year>.norm.txt` and a re-run.
- **Hijri in the converter**: convert between Shamsi, Gregorian and Hijri dates; the Hijri date follows the selected source.
- Tests for the Hijri sources, the shipped data and the Hijri-to-Gregorian lookup.

### Changed
- The build output ships only the Persian and English language folders (`SatelliteResourceLanguages`, plus a clean-up of the `*.mui` and `*.resources.dll` folders the Windows App SDK leaves after build and publish).

### Notes
- Hijri dates after the last official calendar (currently 20 March 2027) come from Umm al-Qura and can differ from Iran's official calendar by a day.

## [0.7.0] - 2026-11-03
### Added
- **Official holidays and observances** (Jalali years 1383-1405) from the bundled calendar dataset, based on FarsiUI's calendar data (credited in `THIRD-PARTY-NOTICES.md`). Holiday days turn red in the calendar, hovering a day lists its occasions, and the area under the calendar shows the selected day's holidays and observances separately. Works fully offline; the data ships in the `calendar` folder next to the exe.
- `CalendarEventService` (loads and caches each year asynchronously; missing years, malformed JSON and unsupported schema versions are logged and skipped) and a `tests/Rooznama.Tests` xunit project.

## [0.6.2] - 2026-11-01
### Fixed
- **Tray day number not updating after manual date change.** The number stayed stale when the manually date change occured. Besides the earlier midnight timer and watchdog, the app now also listens for suspend/resume notifications (including Modern Standby), session unlock/connect and clock changes, and runs a thread-pool heartbeat that detects a long gap between beats; every one of these events refreshes the icon and re-arms the timers. All events are written to the log (`rooznama.log`).

### Docs
- **Readme updated with screenshots

## [0.6.1] - 2026-10-30
### Fixed
- A small empty system tooltip box appeared over or under the custom tray tooltip; the icon is now registered without a system tip when the custom tooltip is on.
- About page version display

### Changed
- Build workflow triggers on version tags

## [0.6.0] - 2026-10-29
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
- Holidays and occasions exist for Jalali years 1383-1405 only. They are matched to days with .NET's `PersianCalendar`, so on days where it disagrees with the official calendar the occasion can land one day off.

### Planned
- Hijri/lunar dates in the day tooltip.
- Google Calendar integration.
