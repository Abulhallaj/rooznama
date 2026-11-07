# Calendar rules, Hijri data and how they fit together

Rooznama shows three kinds of occasions. They are merged per day by `CalendarOccasionService`.

| Layer | Where | What it is |
|---|---|---|
| Yearly dataset | `years/{year}/holidays.json`, `events.json` | The official list for one Jalali year (Tehran University Calendar Center, through FarsiUI). Dates are Jalali `YYYY-MM-DD`. **It wins.** |
| Fixed rules | `rules/fixed-jalali.json`, `fixed-hijri.json`, `fixed-gregorian.json` | Occasions fixed in one calendar: 13 Farvardin, 10 Muharram, 21 September. They fill in wherever the dataset has nothing (later years, new rules). |
| Hijri month starts | `hijri/ut.json` | The official Iranian Hijri month starts, used to turn a Gregorian day into a Hijri date. |

## Fixed rules

```json
{ "schemaVersion": 1, "calendar": "hijri",
  "items": [ { "id": "ashura", "month": 1, "day": 10,
    "title": { "fa": "عاشورای حسینی", "en": "Ashura" },
    "isHoliday": true, "categories": ["religious"] } ] }
```

- `calendar`: `jalali` (the default when absent), `hijri` or `gregorian`. One file per calendar; every file is named `fixed-*.json`.
- `month` 1-12 (Hijri: 1 = Muharram ... 12 = Dhu al-Hijjah) and `day` 1-31, or `-1` for the last day of the month (a Hijri month has 29 or 30 days).
- `aliases` (optional): other record ids the yearly files use for the same occasion, written without the year prefix.
- A rule is checked against every day, so a Hijri date that falls twice in a Jalali year (Eid al-Fitr 1405) or not at all needs no special case.

### When a rule is shown

For a day, a rule that falls on it is added unless
1. the yearly dataset lists the same occasion in that Jalali year. A dataset record belongs to a rule through its `ruleId`, or through its `id` without the year prefix (and without `-early`, `-late`, `-farvardin`, `-esfand` or a trailing `-1`/`-2`) found among the rule ids and `aliases`; or
2. the dataset already has a record with the same title that day (compared without spaces, ZWNJ and Arabic ye/kaf).

A Hijri rule date that did not come from the official data (see below) is marked `approximate`; the calendar adds that word to the occasion's caption.

## Hijri dates

`HijriProvider` asks the source chosen in Settings first, and **UmAlQuraCalendar for every day that source does not cover**.

- **Tehran University Calendar Center**: `hijri/ut.json`. Official Iranian dates, which follow the moon sighting and can differ from Umm al-Qura by a day.
- **Umm al-Qura**: .NET's `UmAlQuraCalendar` (1900-2077).

### Regenerating `hijri/ut.json`

The official yearly calendars are in `sources/` as `Calendar-<year>.norm.txt` (plain text of the PDF, one row per day).

```
node calendar/sources/extract-hijri-anchors.mjs
```

A year is used only if one row exists per Jalali day, the Hijri day counts 1..29/30 without gaps and the month starts follow each other. Every other year is skipped and listed (the app then uses Umm al-Qura there). One unreadable row per gap is rebuilt only when the Hijri day is forced by the rows around it; such days are listed in `repairedDays`. Add a new year by putting its `Calendar-<year>.norm.txt` next to the others and running the script again.

## Checking the rules against the dataset

`CalendarRuleAudit.RunAsync(year, ...)` compares, for one Jalali year, the dates the rules produce with the dates the yearly file lists for the same occasions, and reports each date as `Match`, `DataOnly` (the file has it, the rule does not fall there) or `RuleOnly`. Only rules the year file mentions are compared. The tests run it over the real data: every year the official Hijri data covers agrees, except 1384, where the dataset puts Arbaeen and Eid al-Adha one day before the official qamari date.
