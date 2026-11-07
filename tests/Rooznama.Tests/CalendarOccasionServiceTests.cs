using System.Text.Json;
using Rooznama.Services;
using Xunit;

namespace Rooznama.Tests;

public sealed class CalendarOccasionServiceTests
{
    static string Root => Path.Combine(AppContext.BaseDirectory, "calendar");
    static string UtPath => Path.Combine(Root, "hijri", "ut.json");

    static readonly CalendarRuleSet Rules = CalendarRuleSet.Load(Path.Combine(Root, "rules"));
    static readonly UtHijriSource Ut = UtHijriSource.Load(UtPath);
    static readonly IHijriProvider Iran = new HijriProvider(() => HijriProvider.UtId, Ut);

    static CalendarEventService Data() => new(Root);
    static CalendarOccasionService Service() => new(Data(), Rules, Iran);

    static int[] UtYears()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(UtPath));
        return doc.RootElement.GetProperty("jalaliYears").EnumerateArray().Select(y => y.GetInt32()).ToArray();
    }

    // ---------- merging ----------
    [Fact]
    public async Task The_dataset_wins_and_nothing_is_listed_twice()
    {
        // 1 Farvardin 1405 is Nowruz (Jalali rule) and Eid al-Fitr (Hijri rule); the dataset lists both
        var items = await Service().GetForDateAsync(new DateTime(2026, 3, 21));
        Assert.Single(items, i => i.RuleId == "nowruz-1");
        Assert.Single(items, i => i.Title.Fa == "عید سعید فطر");
        Assert.All(items.Where(i => i.IsHoliday), i => Assert.DoesNotContain("approximate", i.Categories));
        Assert.DoesNotContain(items, i => i.Id.StartsWith("rule:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_rule_is_skipped_when_the_dataset_has_a_record_with_the_same_title_that_day()
    {
        // International Day of Peace, 21 September 2026 = 1405-06-30: the dataset has it (without a ruleId)
        var items = await Service().GetForDateAsync(new DateTime(2026, 9, 21));
        Assert.Single(items, i => CalendarRuleSet.NormalizeTitle(i.Title.Fa) == CalendarRuleSet.NormalizeTitle("روز جهانی صلح"));
    }

    [Fact]
    public async Task Rules_fill_in_for_years_the_dataset_does_not_cover()
    {
        // 1406 has no data files: Nowruz and the Gregorian observances come from the rules
        var nowruz = await Service().GetForDateAsync(new DateTime(2027, 3, 21));
        var item = Assert.Single(nowruz, i => i.RuleId == "nowruz-1");
        Assert.True(item.IsHoliday);

        var christmas = await Service().GetForDateAsync(new DateTime(2027, 12, 25));
        Assert.Contains(christmas, i => i.RuleId == "christmas" && !i.IsHoliday);
    }

    [Fact]
    public async Task Hijri_rule_dates_that_are_not_official_are_marked_approximate()
    {
        // all of 1406 is beyond the official data: Ashura 1449 is found with the Umm al-Qura fallback
        var svc = Service();
        var found = new List<CalendarItem>();
        for (var d = new DateTime(2027, 3, 21); d < new DateTime(2028, 3, 20); d = d.AddDays(1))
            found.AddRange((await svc.GetForDateAsync(d)).Where(i => i.Id.StartsWith("rule:", StringComparison.Ordinal)));

        var hijri = found.Where(i => Rules.Rules.Any(r => r.Calendar == CalendarKind.Hijri && r.Rule.Id == i.RuleId)).ToList();
        Assert.Contains(hijri, i => i.RuleId == "ashura");
        Assert.All(hijri, i => Assert.Contains("approximate", i.Categories));
        Assert.All(found.Except(hijri), i => Assert.DoesNotContain("approximate", i.Categories));
    }

    [Fact]
    public async Task Days_out_of_the_Persian_calendar_range_give_nothing()
    {
        Assert.Empty(await Service().GetForDateAsync(DateTime.MinValue));
        Assert.Empty(await Service().GetForDateAsync(DateTime.MaxValue));
    }

    // ---------- audit: rules against the yearly dataset ----------
    [Fact]
    public async Task Every_year_the_official_Hijri_data_covers_agrees_with_the_rules_except_1384()
    {
        var years = UtYears();
        Assert.Contains(1405, years);
        foreach (int year in years.Where(y => y != 1384))
        {
            var entries = await CalendarRuleAudit.RunAsync(year, Data(), Rules, Iran);
            Assert.NotEmpty(entries);
            Assert.True(entries.All(e => e.Kind == RuleAuditKind.Match),
                $"{year}: " + string.Join(", ", entries.Where(e => e.Kind != RuleAuditKind.Match).Select(e => $"{e.Kind} {e.RuleId} {e.Date}")));
        }
    }

    [Fact]
    public async Task The_dataset_for_1384_disagrees_on_exactly_two_occasions()
    {
        // the dataset puts Arbaeen and Eid al-Adha 1384 one day before the official qamari dates (19 Safar, 9 Dhu al-Hijjah)
        var entries = await CalendarRuleAudit.RunAsync(1384, Data(), Rules, Iran);
        var off = entries.Where(e => e.Kind != RuleAuditKind.Match).OrderBy(e => e.Date, StringComparer.Ordinal)
            .Select(e => $"{e.RuleId} {e.Kind} {e.Date}").ToList();
        Assert.Equal(new[]
        {
            "arbaeen DataOnly 1384-01-10",
            "arbaeen RuleOnly 1384-01-11",
            "eid-adha DataOnly 1384-10-20",
            "eid-adha RuleOnly 1384-10-21"
        }, off);
    }

    [Fact]
    public async Task The_fixed_Jalali_rules_agree_with_the_dataset_in_every_year()
    {
        var jalali = new CalendarRuleSet(Rules.Rules.Where(r => r.Calendar == CalendarKind.Jalali));
        for (int year = 1383; year <= 1405; year++)
        {
            var entries = await CalendarRuleAudit.RunAsync(year, Data(), jalali, Iran);
            Assert.NotEmpty(entries);
            Assert.True(entries.All(e => e.Kind == RuleAuditKind.Match),
                $"{year}: " + string.Join(", ", entries.Where(e => e.Kind != RuleAuditKind.Match).Select(e => $"{e.Kind} {e.RuleId} {e.Date}")));
        }
    }

    [Fact]
    public async Task A_year_without_data_has_nothing_to_audit()
    {
        Assert.Empty(await CalendarRuleAudit.RunAsync(1406, Data(), Rules, Iran));
    }
}
