using System.Collections.Concurrent;
using System.Globalization;

namespace Rooznama.Services;

/// <summary>
/// The holidays and observances of a day: the yearly dataset (official, per year) plus the fixed rules of every calendar.
/// The dataset wins: a rule is not added when the dataset lists the same occasion in that Jalali year (same ruleId/alias) or
/// already has a record with the same title on that day. Everywhere the dataset has nothing (later years, new rules), the
/// rules fill in. Hijri rule dates that did not come from the official Iranian data are marked "approximate".
/// </summary>
public sealed class CalendarOccasionService
{
    static readonly PersianCalendar Pc = new();

    readonly CalendarEventService _data;
    readonly CalendarRuleSet _rules;
    readonly IHijriProvider _hijri;
    readonly ConcurrentDictionary<int, HashSet<string>> _linked = new();   // Jalali year -> rule ids the dataset lists

    public CalendarOccasionService(CalendarEventService data, CalendarRuleSet rules, IHijriProvider hijri)
    {
        _data = data;
        _rules = rules;
        _hijri = hijri;
    }

    public async Task<IReadOnlyList<CalendarItem>> GetForDateAsync(DateTime date)
    {
        date = date.Date;
        if (date < Pc.MinSupportedDateTime || date > Pc.MaxSupportedDateTime.AddDays(-2)) return Array.Empty<CalendarItem>();

        int jy = Pc.GetYear(date);
        var key = CalendarEventService.Key(jy, Pc.GetMonth(date), Pc.GetDayOfMonth(date));
        var year = await _data.GetYearAsync(jy).ConfigureAwait(false);
        IReadOnlyList<CalendarItem> items = year?.ForDate(key) ?? Array.Empty<CalendarItem>();

        var matches = _rules.Match(date, d => _hijri.Get(d, out _));
        if (matches.Count == 0) return items;

        var linked = year is null ? null : _linked.GetOrAdd(jy, _ => LinkedRuleIds(year));
        var result = new List<CalendarItem>(items);
        foreach (var m in matches)
        {
            if (linked is not null && linked.Contains(m.Rule.Id)) continue;
            var title = CalendarRuleSet.NormalizeTitle(m.Rule.Title.Fa);
            if (items.Any(i => CalendarRuleSet.NormalizeTitle(i.Title.Fa) == title)) continue;
            result.Add(ToItem(m, key));
        }
        return result;
    }

    HashSet<string> LinkedRuleIds(CalendarYear year)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in year.Items)
            if (_rules.RuleIdOf(item) is { } id) set.Add(id);
        return set;
    }

    CalendarItem ToItem(RuleMatch m, string key)
    {
        var categories = new List<string>(m.Rule.Categories);
        if (m.Calendar == CalendarKind.Hijri)
        {
            _hijri.Get(m.Date, out bool official);
            if (!official) categories.Add("approximate");
        }
        return new CalendarItem
        {
            Id = $"rule:{m.Rule.Id}:{key}",
            Date = key,
            RuleId = m.Rule.Id,
            Title = m.Rule.Title,
            IsHoliday = m.Rule.IsHoliday,
            Categories = categories,
            Notes = m.Rule.Notes
        };
    }
}

public enum RuleAuditKind
{
    /// <summary>The dataset and the rule agree on this date.</summary>
    Match,
    /// <summary>The dataset lists the occasion on a date where the rule does not fall.</summary>
    DataOnly,
    /// <summary>The rule falls on a date where the dataset does not list the occasion.</summary>
    RuleOnly
}

public sealed record RuleAuditEntry(int Year, string RuleId, RuleAuditKind Kind, string Date);

/// <summary>Compares the dates the rules produce with the dates the yearly dataset lists for the same occasions.</summary>
public static class CalendarRuleAudit
{
    /// <summary>
    /// One Jalali year. Only rules the dataset records in that year are compared; a rule the year file does not mention
    /// at all (for example a Gregorian observance listed under another name) is not an error.
    /// </summary>
    public static async Task<IReadOnlyList<RuleAuditEntry>> RunAsync(int jalaliYear, CalendarEventService data, CalendarRuleSet rules, IHijriProvider hijri)
    {
        var year = await data.GetYearAsync(jalaliYear).ConfigureAwait(false);
        if (year is null) return Array.Empty<RuleAuditEntry>();

        var dataDates = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        foreach (var item in year.Items)
        {
            if (rules.RuleIdOf(item) is not { } id) continue;
            if (!dataDates.TryGetValue(id, out var set)) dataDates[id] = set = new SortedSet<string>(StringComparer.Ordinal);
            set.Add(item.Date);
        }

        var pc = new PersianCalendar();
        var ruleDates = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        var first = pc.ToDateTime(jalaliYear, 1, 1, 0, 0, 0, 0);
        var next = pc.ToDateTime(jalaliYear + 1, 1, 1, 0, 0, 0, 0);
        for (var d = first; d < next; d = d.AddDays(1))
        {
            foreach (var m in rules.Match(d, x => hijri.Get(x, out _)))
            {
                if (!dataDates.ContainsKey(m.Rule.Id)) continue;
                if (!ruleDates.TryGetValue(m.Rule.Id, out var set)) ruleDates[m.Rule.Id] = set = new SortedSet<string>(StringComparer.Ordinal);
                set.Add(CalendarEventService.Key(pc.GetYear(d), pc.GetMonth(d), pc.GetDayOfMonth(d)));
            }
        }

        var entries = new List<RuleAuditEntry>();
        foreach (var (id, fromData) in dataDates)
        {
            var fromRule = ruleDates.TryGetValue(id, out var s) ? s : new SortedSet<string>(StringComparer.Ordinal);
            foreach (var date in fromData.Union(fromRule).OrderBy(x => x, StringComparer.Ordinal))
            {
                var kind = fromData.Contains(date) && fromRule.Contains(date) ? RuleAuditKind.Match
                         : fromData.Contains(date) ? RuleAuditKind.DataOnly
                         : RuleAuditKind.RuleOnly;
                entries.Add(new RuleAuditEntry(jalaliYear, id, kind, date));
            }
        }
        return entries;
    }
}
