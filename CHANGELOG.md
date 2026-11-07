# Changelog

All notable changes to **Rooznama (روزنما)** are documented here.
Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow [Semantic Versioning](https://semver.org/).

## Unreleased

## [1.0.0-beta.1] - 2026-11-05
First beta of 1.0: the Hijri calendar, fixed occasions per calendar, week numbers, keyboard navigation and personal events.

### Added
- **Hijri (qamari) dates.** Shown in the day hover text and the tray tooltip (Settings: "Show the Hijri date in tooltips") and as a small day in the large calendar's cells, with the month name on the 1st (Settings: "Show the Hijri day in calendar cells"). Hijri numbers use **Arabic digits** (٠١٢٣…), not Persian ones.
- **Hijri source choice** in Settings > Calendar: *Tehran University Calendar Center* (official Iranian dates, `calendar/hijri/ut.json`) or *Umm al-Qura* (`UmAlQuraCalendar`). **Umm al-Qura is used automatically for any day the selected source does not cover**, so every date always has a Hijri date. `calendar/sources/extract-hijri-anchors.mjs` builds the official data from the Tehran University yearly calendars; 
- **Fixed occasions per calendar**: `calendar/rules/fixed-jalali.json` (existing), new `fixed-hijri.json` (the 17 official religious holidays, `day: -1` = last day of the month for the martyrdom of Imam Reza) and `fixed-gregorian.json` (New Year's Day, 8 March, 21 September, Christmas). They fill in wherever the yearly dataset has nothing, for example Nowruz and the religious holidays of 1406 onwards; Hijri dates not taken from the official data are labelled *approximate*. The yearly dataset always wins and nothing is listed twice. See `calendar/RULES.md`.
- **Rules vs. dataset comparison** (`CalendarRuleAudit`, run by the tests): for every year the official Hijri data covers, all fixed rules agree with the dataset. The only differences are in 1384, where the dataset places Arbaeen and Eid al-Adha one day before the official qamari date.
- **Week numbers** in a column before the first (Saturday) column of the large calendar, by the Iranian rule (the week with 1 Farvardin is week 1, weeks start on Saturday). Turn it off in Settings. The calculation takes the calendar as a parameter so Gregorian or Hijri weeks can follow an active-calendar switch later.
- **Keyboard navigation** in the large calendar: arrow keys move the selected day (←/→ one day, ↑/↓ one week, in the direction of the right-to-left grid; the next or previous month opens by itself), PageUp/PageDown change the month. The calendar takes focus when it opens.
- **Personal events**: add, edit and delete from the large calendar ("Add event"; click an event to edit it). All-day or timed, with notes, and optional yearly repetition by the Shamsi, Hijri or Gregorian date. They are stored in `personal-events.json` next to `settings.json` (written atomically; a damaged file is kept as `.bad`; a file from a newer version is never overwritten). The model already carries what Google Calendar needs (remote id, etag, sync state, Gregorian wall-clock times with all-day = exclusive end, RRULE for Gregorian yearly events, deletion tombstones), so a Google provider only has to connect and translate.
- Tests for the Hijri sources, the rules and their linking to the dataset, the occasion merge, the audit, week numbers and the personal event store.

### Changed
- `CalendarEvent` gained `Id`, `Source`, `IsAllDay`, `IsReadOnly`, `Repeats` and `Description`; `App.Events` now defaults to the personal events provider (`ICalendarEventProvider` is unchanged).
- Occasions come from `CalendarOccasionService` (yearly dataset + fixed rules) instead of `CalendarEventService` directly.
- `calendar/rules` and `calendar/hijri` are now shipped with the app; `calendar/schema.json` describes the rule calendars, `aliases`, `day: -1` and the Hijri month starts.
- The update check understands pre-release versions: 1.0.0 is newer than 1.0.0-beta.1, beta.2 is newer than beta.1.

### Notes
- Hijri dates after the last official calendar (currently 20 March 2027) come from Umm al-Qura and can differ from Iran's official calendar by a day.
- Months 1405+ of the dataset are unchanged; nothing was regenerated from the PDFs.

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
