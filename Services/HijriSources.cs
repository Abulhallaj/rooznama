using System.Globalization;
using System.Text.Json;

namespace Rooznama.Services;

// Hijri dates come from a source. Two exist: the official Iranian calendar (Tehran University Calendar Center) and
// .NET's UmAlQuraCalendar. HijriProvider asks the selected source first and falls back to UmAlQuraCalendar for any
// day the selected source does not cover. Nothing here depends on the UI, so it is compiled into the tests as well.

public interface IHijriSource
{
    /// <summary>The Hijri date of a Gregorian day, or null when this source has no data for it.</summary>
    HijriDate? TryGet(DateTime date);
}

/// <summary>.NET's UmAlQuraCalendar (Saudi civil calendar, 1900-2077). It can differ from Iran's official calendar by a day.</summary>
public sealed class UmAlQuraHijriSource : IHijriSource
{
    static readonly UmAlQuraCalendar Cal = new();

    public HijriDate? TryGet(DateTime date)
    {
        date = date.Date;
        if (date < Cal.MinSupportedDateTime || date > Cal.MaxSupportedDateTime) return null;
        return new HijriDate(Cal.GetYear(date), Cal.GetMonth(date), Cal.GetDayOfMonth(date));
    }
}

/// <summary>
/// The official Iranian Hijri dates from calendar/hijri/ut.json (made by calendar/sources/extract-hijri-anchors.mjs from the
/// Tehran University Calendar Center calendars). The file lists the day ranges it covers and the first day of every Hijri
/// month; a day outside the ranges has no answer here.
/// </summary>
public sealed class UtHijriSource : IHijriSource
{
    public const int SupportedSchemaVersion = 1;

    public static UtHijriSource Empty { get; } = new(Array.Empty<(DateTime, DateTime)>(), Array.Empty<(DateTime, int, int)>());

    readonly (DateTime From, DateTime To)[] _ranges;
    readonly (DateTime Start, int Year, int Month)[] _months;   // sorted by Start

    UtHijriSource((DateTime, DateTime)[] ranges, (DateTime, int, int)[] months)
    {
        _ranges = ranges;
        _months = months;
    }

    public bool IsEmpty => _ranges.Length == 0;

    public static UtHijriSource Load(string path, Action<string>? log = null)
    {
        try
        {
            return File.Exists(path) ? FromJson(File.ReadAllText(path), log) : Empty;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            log?.Invoke($"hijri: cannot read {path}: {ex.Message}");
            return Empty;
        }
    }

    /// <summary>Never throws: an unreadable or unsupported file gives an empty source (everything then falls back).</summary>
    public static UtHijriSource FromJson(string json, Action<string>? log = null)
    {
        try
        {
            using var doc = JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            var root = doc.RootElement;
            if (root.TryGetProperty("schemaVersion", out var sv) && sv.GetInt32() != SupportedSchemaVersion)
            {
                log?.Invoke($"hijri: unsupported schemaVersion {sv.GetInt32()}; ignored");
                return Empty;
            }

            var ranges = new List<(DateTime, DateTime)>();
            foreach (var r in root.GetProperty("ranges").EnumerateArray())
                ranges.Add((ParseDate(r[0].GetString()), ParseDate(r[1].GetString())));

            var months = new List<(DateTime, int, int)>();
            foreach (var m in root.GetProperty("months").EnumerateArray())
                months.Add((ParseDate(m[2].GetString()), m[0].GetInt32(), m[1].GetInt32()));
            months.Sort((a, b) => a.Item1.CompareTo(b.Item1));

            return new UtHijriSource(ranges.ToArray(), months.ToArray());
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException or ArgumentNullException)
        {
            log?.Invoke("hijri: cannot read the official Hijri data: " + ex.Message);
            return Empty;
        }
    }

    static DateTime ParseDate(string? s) =>
        DateTime.ParseExact(s!, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None);

    public HijriDate? TryGet(DateTime date)
    {
        date = date.Date;
        bool covered = false;
        foreach (var range in _ranges)
            if (date >= range.From && date <= range.To) { covered = true; break; }
        if (!covered) return null;

        // the last month that starts on or before the day
        int lo = 0, hi = _months.Length - 1, k = -1;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            if (_months[mid].Start <= date) { k = mid; lo = mid + 1; } else hi = mid - 1;
        }
        if (k < 0) return null;

        int day = (date - _months[k].Start).Days + 1;
        return day is >= 1 and <= 30 ? new HijriDate(_months[k].Year, _months[k].Month, day) : null;
    }
}

/// <summary>The Gregorian day that has a given Hijri date, as a provider counts it (the reverse of IHijriProvider.Get).</summary>
public static class HijriLookup
{
    /// <summary>Null when no day has that Hijri date: day 30 of a 29-day month, or a year the sources do not know.</summary>
    public static DateTime? FindGregorian(HijriDate target, IHijriProvider provider)
    {
        if (target.Year is < 1 or > 9000 || target.Month is < 1 or > 12 || target.Day is < 1 or > 30) return null;

        // The civil (tabular) calendar is never more than three days away from the observed ones, so look around its answer.
        var estimate = TabularEstimate(target);
        for (int delta = 0; delta <= 3; delta++)
            foreach (var d in delta == 0 ? new[] { 0 } : new[] { -delta, delta })
            {
                DateTime candidate;
                try { candidate = estimate.AddDays(d); } catch (ArgumentOutOfRangeException) { continue; }
                if (provider.Get(candidate, out _) == target) return candidate;
            }
        return null;
    }

    static DateTime TabularEstimate(HijriDate h)
    {
        long jdn = h.Day + (long)Math.Ceiling(29.5 * (h.Month - 1)) + (h.Year - 1L) * 354 + (3 + 11L * h.Year) / 30 + 1948440 - 1;
        return new DateTime(1, 1, 1).AddDays(jdn - 1721426);
    }
}

public interface IHijriProvider
{
    /// <param name="official">True when the answer came from the Tehran University Calendar Center data.</param>
    HijriDate? Get(DateTime date, out bool official);
}

/// <summary>The selected source first; UmAlQuraCalendar for every day the selected source does not cover.</summary>
public sealed class HijriProvider : IHijriProvider
{
    public const string UtId = "ut";
    public const string UmAlQuraId = "umalqura";

    readonly Func<string> _selected;
    readonly IHijriSource _ut;
    readonly IHijriSource _umAlQura;

    public HijriProvider(Func<string> selectedSource, IHijriSource ut, IHijriSource? umAlQura = null)
    {
        _selected = selectedSource;
        _ut = ut;
        _umAlQura = umAlQura ?? new UmAlQuraHijriSource();
    }

    public HijriDate? Get(DateTime date, out bool official)
    {
        if (_selected() == UtId && _ut.TryGet(date) is { } h)
        {
            official = true;
            return h;
        }
        official = false;
        return _umAlQura.TryGet(date);
    }
}
