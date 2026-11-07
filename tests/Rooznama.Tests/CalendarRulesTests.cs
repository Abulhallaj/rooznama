using Rooznama.Services;
using Xunit;

namespace Rooznama.Tests;

public sealed class CalendarRulesTests : IDisposable
{
    readonly string _tmp = Path.Combine(Path.GetTempPath(), "rooznama-rules-" + Guid.NewGuid().ToString("N"));

    static string RulesDir => Path.Combine(AppContext.BaseDirectory, "calendar", "rules");
    static string UtPath => Path.Combine(AppContext.BaseDirectory, "calendar", "hijri", "ut.json");

    static CalendarRuleSet Real(List<string>? log = null) => CalendarRuleSet.Load(RulesDir, log is null ? null : (Action<string>)log.Add);
    static IHijriProvider OfficialIran() => new HijriProvider(() => HijriProvider.UtId, UtHijriSource.Load(UtPath));
    static Func<DateTime, HijriDate?> Official() { var p = OfficialIran(); return d => p.Get(d, out _); }

    static CalendarItem Item(string id, string? ruleId = null) =>
        new() { Id = id, Date = "1405-01-01", RuleId = ruleId, Title = new LocalizedTitle { Fa = "x" } };

    public void Dispose() { try { Directory.Delete(_tmp, true); } catch { } }

    [Fact]
    public void The_shipped_rules_load_for_all_three_calendars()
    {
        var log = new List<string>();
        var set = Real(log);
        Assert.Empty(log);
        Assert.Equal(10, set.Rules.Count(r => r.Calendar == CalendarKind.Jalali));
        Assert.Equal(17, set.Rules.Count(r => r.Calendar == CalendarKind.Hijri));
        Assert.Equal(4, set.Rules.Count(r => r.Calendar == CalendarKind.Gregorian));
        Assert.Equal(set.Rules.Count, set.Rules.Select(r => r.Rule.Id).Distinct().Count());   // ids are unique across files
    }

    [Fact]
    public void A_fixed_Jalali_rule_matches_its_day()
    {
        // 13 Farvardin 1405 = 2 April 2026 (Sizdah Bedar / Nature Day)
        var hits = Real().Match(new DateTime(2026, 4, 2), Official());
        var hit = Assert.Single(hits, h => h.Calendar == CalendarKind.Jalali);
        Assert.Equal("sizdah-bedar", hit.Rule.Id);
        Assert.Empty(Real().Match(new DateTime(2026, 4, 3), Official()).Where(h => h.Calendar == CalendarKind.Jalali));
    }

    [Fact]
    public void A_fixed_Gregorian_rule_matches_its_day_in_every_year()
    {
        foreach (int year in new[] { 2026, 2027, 2040 })
            Assert.Contains(Real().Match(new DateTime(year, 9, 21), Official()), h => h.Rule.Id == "peace-day");
    }

    [Fact]
    public void A_fixed_Hijri_rule_matches_with_the_official_dates()
    {
        // 10 Muharram 1448 = 25 June 2026 (Ashura), 9 Muharram the day before (Tasua)
        Assert.Contains(Real().Match(new DateTime(2026, 6, 25), Official()), h => h.Calendar == CalendarKind.Hijri && h.Rule.Id == "ashura");
        Assert.Contains(Real().Match(new DateTime(2026, 6, 24), Official()), h => h.Rule.Id == "tasua");
        Assert.DoesNotContain(Real().Match(new DateTime(2026, 6, 26), Official()), h => h.Rule.Id is "ashura" or "tasua");
        // 1 Shawwal 1447 = 21 March 2026 (Eid al-Fitr) - which is also 1 Farvardin
        var march21 = Real().Match(new DateTime(2026, 3, 21), Official());
        Assert.Contains(march21, h => h.Rule.Id == "eid-fitr");
        Assert.Contains(march21, h => h.Rule.Id == "nowruz-1");
    }

    [Fact]
    public void A_Hijri_rule_matches_nothing_when_no_Hijri_date_is_known()
    {
        Assert.DoesNotContain(Real().Match(new DateTime(2026, 6, 25), _ => null), h => h.Calendar == CalendarKind.Hijri);
    }

    [Fact]
    public void Day_minus_1_is_the_last_day_of_a_Hijri_month_of_29_or_30_days()
    {
        var rule = new CalendarRule { Id = "end-of-safar", Month = 2, Day = -1, Title = new LocalizedTitle { Fa = "آخر صفر" } };
        var set = new CalendarRuleSet(new[] { (CalendarKind.Hijri, rule) });
        var start = new DateTime(2026, 1, 1);

        // a fake calendar whose months have 29 days: 2026-01-01 = 2/1, so 2/29 is 2026-01-29
        HijriDate? Fake(DateTime d)
        {
            int n = (d - start).Days;
            return n < 0 ? null : new HijriDate(1448, 2 + n / 29, n % 29 + 1);
        }
        var days = Enumerable.Range(0, 70).Select(i => start.AddDays(i)).Where(d => set.Match(d, Fake).Count > 0).ToList();
        Assert.Equal(new[] { new DateTime(2026, 1, 29) }, days);
    }

    [Fact]
    public void Day_minus_1_works_for_Jalali_and_Gregorian_months_too()
    {
        var esfand = new CalendarRule { Id = "end-of-year", Month = 12, Day = -1, Title = new LocalizedTitle { Fa = "x" } };
        var feb = new CalendarRule { Id = "end-of-feb", Month = 2, Day = -1, Title = new LocalizedTitle { Fa = "y" } };
        var set = new CalendarRuleSet(new[] { (CalendarKind.Jalali, esfand), (CalendarKind.Gregorian, feb) });

        // 29 Esfand 1405 is the last day (1405 is not a leap year); 30 Esfand 1403 was the last day of a leap year
        Assert.Contains(set.Match(new DateTime(2027, 3, 20), _ => null), h => h.Rule.Id == "end-of-year");
        Assert.Empty(set.Match(new DateTime(2027, 3, 19), _ => null).Where(h => h.Rule.Id == "end-of-year"));
        Assert.Contains(set.Match(new DateTime(2025, 3, 20), _ => null), h => h.Rule.Id == "end-of-year");
        Assert.Contains(set.Match(new DateTime(2028, 2, 29), _ => null), h => h.Rule.Id == "end-of-feb");
        Assert.Empty(set.Match(new DateTime(2028, 2, 28), _ => null));
    }

    [Fact]
    public void Dataset_records_are_linked_to_rules_by_ruleId_alias_and_suffix()
    {
        var set = Real();
        Assert.Equal("nowruz-1", set.RuleIdOf(Item("1405-nowruz-1", "nowruz-1")));
        Assert.Equal("tasua", set.RuleIdOf(Item("1405-tasua")));
        Assert.Equal("eid-fitr", set.RuleIdOf(Item("1405-eid-fitr-1-early")));      // alias eid-fitr-1 after dropping -early
        Assert.Equal("eid-fitr-2", set.RuleIdOf(Item("1405-eid-fitr-2-early")));
        Assert.Equal("mabath", set.RuleIdOf(Item("1403-mabath-2")));                // trailing -2 dropped
        Assert.Equal("arbaeen", set.RuleIdOf(Item("1400-arbaeen-late")));
        Assert.Equal("martyrdom-fatimah", set.RuleIdOf(Item("1401-martyrdom-fatimah-esfand")));
        Assert.Equal("mid-shaaban", set.RuleIdOf(Item("1390-mid-shaban")));         // alias
        Assert.Equal("martyrdom-imam-askari", set.RuleIdOf(Item("1390-martyrdom-imam-hasan-askari")));
        Assert.Null(set.RuleIdOf(Item("1405-evt-003")));
        Assert.Null(set.RuleIdOf(Item("1384-martyrdom-imam-hadi")));                // an occasional holiday, deliberately not a rule
    }

    [Fact]
    public void Titles_are_compared_without_spaces_zwnj_and_arabic_letter_variants()
    {
        Assert.Equal(CalendarRuleSet.NormalizeTitle("روز جهانی زنان"), CalendarRuleSet.NormalizeTitle("روزجهانی زنان"));
        Assert.Equal(CalendarRuleSet.NormalizeTitle("شهادت امام علی\u200cالنقی"), CalendarRuleSet.NormalizeTitle("شهادت امام علیالنقی"));
        Assert.Equal(CalendarRuleSet.NormalizeTitle("\u0639\u0644\u06cc"), CalendarRuleSet.NormalizeTitle("\u0639\u0644\u064a"));   // Persian vs Arabic yeh
        Assert.NotEqual(CalendarRuleSet.NormalizeTitle("روز جهانی صلح"), CalendarRuleSet.NormalizeTitle("روز جهانی زنان"));
    }

    [Fact]
    public void Broken_files_and_invalid_rules_are_logged_and_skipped()
    {
        Directory.CreateDirectory(_tmp);
        File.WriteAllText(Path.Combine(_tmp, "fixed-bad.json"), "{ not json");
        File.WriteAllText(Path.Combine(_tmp, "fixed-version.json"), """{ "schemaVersion": 7, "items": [] }""");
        File.WriteAllText(Path.Combine(_tmp, "fixed-calendar.json"), """{ "schemaVersion": 1, "calendar": "mayan", "items": [] }""");
        File.WriteAllText(Path.Combine(_tmp, "fixed-ok.json"), """
            { "schemaVersion": 1, "calendar": "gregorian", "items": [
              { "id": "good", "month": 5, "day": 5, "title": { "fa": "الف" }, "isHoliday": false },
              { "id": "bad-month", "month": 13, "day": 1, "title": { "fa": "ب" }, "isHoliday": false },
              { "id": "bad-day", "month": 1, "day": 32, "title": { "fa": "ج" }, "isHoliday": false } ] }
            """);
        File.WriteAllText(Path.Combine(_tmp, "other.json"), "ignored: the name does not start with fixed-");

        var log = new List<string>();
        var set = CalendarRuleSet.Load(_tmp, log.Add);

        Assert.Equal("good", Assert.Single(set.Rules).Rule.Id);
        Assert.Equal(5, log.Count);
        Assert.Empty(CalendarRuleSet.Load(Path.Combine(_tmp, "nope")).Rules);   // a missing folder is fine
    }
}
