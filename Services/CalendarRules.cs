using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rooznama.Services;

public enum CalendarKind { Jalali, Hijri, Gregorian }

/// <summary>
/// calendar/rules/fixed-*.json: occasions that are fixed in one calendar (13 Farvardin, 10 Muharram, 21 September).
/// "calendar" is jalali (the default when absent), hijri or gregorian.
/// </summary>
public sealed class CalendarRuleFile
{
    [JsonPropertyName("schemaVersion")] public int SchemaVersion { get; init; }
    [JsonPropertyName("calendar")] public string? Calendar { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("items")] public List<CalendarRule> Items { get; init; } = new();

    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
}

public sealed class CalendarRule
{
    [JsonPropertyName("id")] public required string Id { get; init; }
    [JsonPropertyName("month")] public required int Month { get; init; }
    /// <summary>1-31, or -1 for the last day of the month (a Hijri month has 29 or 30 days).</summary>
    [JsonPropertyName("day")] public required int Day { get; init; }
    [JsonPropertyName("title")] public required LocalizedTitle Title { get; init; }
    [JsonPropertyName("isHoliday")] public bool IsHoliday { get; init; }
    [JsonPropertyName("categories")] public List<string> Categories { get; init; } = new();
    [JsonPropertyName("notes")] public string? Notes { get; init; }
    /// <summary>Other names the yearly dataset uses for the same occasion (its record ids without the year prefix).</summary>
    [JsonPropertyName("aliases")] public List<string> Aliases { get; init; } = new();

    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
}

public sealed record RuleMatch(CalendarKind Calendar, CalendarRule Rule, DateTime Date);

/// <summary>
/// All fixed rules of all calendars. A rule is evaluated per day, so an occasion that falls twice in a Jalali year (a Hijri
/// date does) or not at all needs no special handling. Independent of the UI: the Hijri dates are passed in.
/// </summary>
public sealed class CalendarRuleSet
{
    static readonly PersianCalendar Pc = new();
    static readonly string[] YearSuffixes = { "-early", "-late", "-farvardin", "-esfand" };
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    readonly List<(CalendarKind Calendar, CalendarRule Rule)> _rules;
    readonly Dictionary<string, string> _byName = new(StringComparer.OrdinalIgnoreCase);   // rule id or alias -> rule id

    public static CalendarRuleSet Empty { get; } = new(Array.Empty<(CalendarKind, CalendarRule)>());

    public IReadOnlyList<(CalendarKind Calendar, CalendarRule Rule)> Rules => _rules;

    public CalendarRuleSet(IEnumerable<(CalendarKind Calendar, CalendarRule Rule)> rules)
    {
        _rules = rules.ToList();
        foreach (var (_, r) in _rules) _byName[r.Id] = r.Id;                              // ids first, so an alias never hides an id
        foreach (var (_, r) in _rules)
            foreach (var a in r.Aliases) _byName.TryAdd(a, r.Id);
    }

    /// <summary>Reads every fixed-*.json in the folder. A missing folder or a broken file is logged and skipped; never throws.</summary>
    public static CalendarRuleSet Load(string directory, Action<string>? log = null)
    {
        var rules = new List<(CalendarKind, CalendarRule)>();
        try
        {
            if (!Directory.Exists(directory)) return Empty;
            foreach (var path in Directory.EnumerateFiles(directory, "fixed-*.json").OrderBy(p => p, StringComparer.Ordinal))
            {
                try
                {
                    var file = JsonSerializer.Deserialize<CalendarRuleFile>(File.ReadAllText(path), JsonOptions);
                    if (file is null) continue;
                    if (file.SchemaVersion != 1) { log?.Invoke($"rules: {path} has unsupported schemaVersion {file.SchemaVersion}; ignored"); continue; }
                    if (!TryKind(file.Calendar, out var kind)) { log?.Invoke($"rules: {path} has unknown calendar '{file.Calendar}'; ignored"); continue; }

                    foreach (var r in file.Items)
                    {
                        if (IsValid(r)) rules.Add((kind, r));
                        else log?.Invoke($"rules: {path}: rule '{r.Id}' has an invalid month/day; ignored");
                    }
                }
                catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
                {
                    log?.Invoke($"rules: cannot read {path}: {ex.Message}");
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            log?.Invoke($"rules: cannot read {directory}: {ex.Message}");
        }
        return new CalendarRuleSet(rules);
    }

    static bool TryKind(string? name, out CalendarKind kind)
    {
        switch (name?.Trim().ToLowerInvariant())
        {
            case null or "" or "jalali": kind = CalendarKind.Jalali; return true;
            case "hijri": kind = CalendarKind.Hijri; return true;
            case "gregorian": kind = CalendarKind.Gregorian; return true;
            default: kind = default; return false;
        }
    }

    static bool IsValid(CalendarRule r) => r.Month is >= 1 and <= 12 && (r.Day == -1 || r.Day is >= 1 and <= 31);

    // ---------- matching ----------
    /// <summary>The rules that fall on a day. <paramref name="hijri"/> gives the Hijri date of any day (null when unknown).</summary>
    public IReadOnlyList<RuleMatch> Match(DateTime date, Func<DateTime, HijriDate?> hijri)
    {
        var result = new List<RuleMatch>();
        if (_rules.Count == 0) return result;

        var d = date.Date;
        bool jOk = TryJalali(d, out int jm, out int jd, out bool jLast);
        bool hKnown = false, hOk = false, hLast = false;
        int hm = 0, hd = 0;

        foreach (var (kind, rule) in _rules)
        {
            bool hit = false;
            switch (kind)
            {
                case CalendarKind.Jalali:
                    hit = jOk && jm == rule.Month && (rule.Day == jd || (rule.Day == -1 && jLast));
                    break;
                case CalendarKind.Gregorian:
                    hit = d.Month == rule.Month && (rule.Day == d.Day || (rule.Day == -1 && d.Month != d.AddDays(1).Month));
                    break;
                case CalendarKind.Hijri:
                    if (!hKnown) { hKnown = true; (hOk, hm, hd, hLast) = HijriFields(d, hijri); }
                    hit = hOk && hm == rule.Month && (rule.Day == hd || (rule.Day == -1 && hLast));
                    break;
            }
            if (hit) result.Add(new RuleMatch(kind, rule, d));
        }
        return result;
    }

    static bool TryJalali(DateTime d, out int month, out int day, out bool last)
    {
        month = day = 0;
        last = false;
        if (d < Pc.MinSupportedDateTime || d > Pc.MaxSupportedDateTime.AddDays(-2)) return false;
        month = Pc.GetMonth(d);
        day = Pc.GetDayOfMonth(d);
        last = Pc.GetMonth(d.AddDays(1)) != month;
        return true;
    }

    static (bool Ok, int Month, int Day, bool Last) HijriFields(DateTime d, Func<DateTime, HijriDate?> hijri)
    {
        if (hijri(d) is not { } cur) return (false, 0, 0, false);
        bool last = d < DateTime.MaxValue.Date && hijri(d.AddDays(1)) is { } next && next.Month != cur.Month;
        return (true, cur.Month, cur.Day, last);
    }

    // ---------- linking the yearly dataset to the rules ----------
    /// <summary>
    /// The rule a yearly-dataset record belongs to: its ruleId, otherwise its id (without the year prefix and without an
    /// -early/-late or -1/-2 suffix) looked up among the rule ids and aliases. Null when it is not linked to any rule.
    /// </summary>
    public string? RuleIdOf(CalendarItem item)
    {
        if (!string.IsNullOrEmpty(item.RuleId))
            return _byName.TryGetValue(item.RuleId, out var viaRule) ? viaRule : item.RuleId;

        foreach (var candidate in Candidates(StripYear(item.Id)))
            if (_byName.TryGetValue(candidate, out var id)) return id;
        return null;
    }

    static string StripYear(string id) =>
        id.Length > 5 && id[4] == '-' && char.IsAsciiDigit(id[0]) && char.IsAsciiDigit(id[1]) && char.IsAsciiDigit(id[2]) && char.IsAsciiDigit(id[3])
            ? id[5..]
            : id;

    static IEnumerable<string> Candidates(string slug)
    {
        yield return slug;
        var s = slug;
        foreach (var suffix in YearSuffixes)
            if (s.EndsWith(suffix, StringComparison.Ordinal))
            {
                s = s[..^suffix.Length];
                yield return s;
                break;
            }
        if (s.Length > 2 && s[^2] == '-' && char.IsAsciiDigit(s[^1])) yield return s[..^2];
    }

    /// <summary>Titles compared without spaces, ZWNJ and the Arabic variants of ye/kaf, so "روزجهانی زنان" equals "روز جهانی زنان".</summary>
    public static string NormalizeTitle(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
        {
            if (char.IsWhiteSpace(c) || c is '\u200c' or '\u200d' or '\u200e' or '\u200f') continue;
            sb.Append(c switch { '\u064a' => '\u06cc', '\u0643' => '\u06a9', _ => c });
        }
        return sb.ToString();
    }
}
