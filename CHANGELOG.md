# Changelog

All notable changes to **Rooznama (روزنما)** are documented here.
Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow [Semantic Versioning](https://semver.org/).

## [0.5.0] - Unreleased

First public development version: a Shamsi (Jalali) calendar for the Windows system tray, built with .NET 8 and WinUI 3 (Windows App SDK 1.7, unpackaged, self-contained).

### Added

**Tray**
- Tray icon showing the current Shamsi day number. The number is rendered as glyph outlines at the largest size at which the widest day (1–31) still fits, centered per day.
- Tooltip with the Shamsi date and (optionally) the Gregorian date.
- Left click opens the calendar flyout; right click opens a native menu (calendar, converter, settings, about, exit), right-to-left in Persian and dark-mode aware.
- Single-instance protection.

**Calendar flyout**
- Borderless acrylic window with rounded corners, placed above the taskbar, with an animated slide/fade on show and hide.
- Header with today's date (Shamsi and Gregorian) and a collapse/expand arrow; the collapsed state is remembered.
- Day, month and year (decade) views: click the title to zoom out, click a month or year to zoom in; the up/down arrows move by month, year or decade, and the grid slides in the direction of travel.
- Always six full weeks; days of the previous and next month are shown dimmed and are clickable.
- Hovering a day shows its Gregorian date (tooltip is right-to-left in Persian).
- Window height fits the content exactly.

**Main window**
- Side navigation: calendar and converter on top, settings and about at the bottom.
- Calendar page: the same panel in a large variant with the Gregorian day beside every Shamsi day and a **Today** button.
- Date converter: two linked date pickers (Shamsi and Gregorian); changing one updates the other; copy buttons for the numeric form.
- About page with the app icon and version.

**Settings**
- Language: Persian (default) and English, with RTL/LTR layout, localized month and weekday names and digits.
- Font: Vazirmatn (default, bundled in `Assets/Fonts` or installed) and Tahoma.
- Theme: automatic (follows Windows), light, dark.
- Tray icon color, Gregorian date in tooltip, close the flyout when it loses focus, start with Windows.
- Related options are laid out in a two-column grid inside cards; changing language, font or theme offers a restart button.

**Extensibility and tooling**
- `ICalendarEventProvider` (events list under the calendar) as the hook for Google Calendar or other sources; `DayInfo.Lines()` as the hook for Hijri/lunar dates and occasions.
- Diagnostic log at `%AppData%\Rooznama\rooznama.log`.
- Application icon (`.ico` for the executable and taskbar button, PNG for the About page).

### Changed
- The UI was rebuilt from the initial WinForms prototype on WinUI 3. WinForms can't be combined with WinUI 3 (it breaks XAML compilation), so the tray icon now uses the native `Shell_NotifyIcon` API directly.
- The calendar flyout (tray click) and the main window (menu) are now separate windows.
- The "Persian digits on the icon" option was removed; digits on the icon follow the selected language.
- The tray menu's "Calendar" entry opens the main window on the calendar page.
- The "Today" button was removed from the flyout (today stays highlighted) and the selected-date card under the grid was removed.
- Version set to 0.5.0.

### Fixed
- **Tray day number not updating at midnight** when the app stayed open overnight. The root cause wasn't confirmed; the update logic was hardened: an exact one-shot timer for the next local midnight, a 30-second watchdog, a re-check after resume from sleep or a system time change, re-adding the icon if the shell lost it, a refresh when hovering the icon, and logging of every date change.
- Vazirmatn not being applied: fonts are now referenced per file (a folder URI isn't valid), an installed Vazirmatn is detected and preferred, and the applied font string is shown on the About page.
- Flyout appearance: removed the duplicate border, stripped leftover window frame styles, and set a single theme-aware 1px border and rounded corners.

### Known limitations
- The tray icon tooltip is drawn by Windows and can't be made right-to-left.
- The dark tray menu relies on undocumented `uxtheme.dll` entry points; on a Windows build where they behave differently the menu stays light.
- Language, font and theme changes need a restart.
- Loading fonts from the app folder in an unpackaged WinUI 3 app can be unreliable; installing Vazirmatn on Windows is the most reliable option.
- Dates use .NET's arithmetic `PersianCalendar`, which can differ from the official calendar in some years.
- Official holidays and occasions are not included yet.

### Planned
- Official holidays and occasions from the University of Tehran Institute of Geophysics calendar, as a per-year JSON file with a user override.
- Portable ZIP and installer builds.
- Hijri/lunar dates in the day tooltip.
- Google Calendar integration.
