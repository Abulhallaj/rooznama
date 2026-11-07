using System.Text.Json;
using Rooznama.Services;
using Xunit;

namespace Rooznama.Tests;

public sealed class HijriTests
{
    static string UtPath => Path.Combine(AppContext.BaseDirectory, "calendar", "hijri", "ut.json");

    static UtHijriSource RealUt() => UtHijriSource.Load(UtPath);

    [Fact]
    public void Official_data_is_loaded_and_has_the_known_dates()
    {
        var ut = RealUt();
        Assert.False(ut.IsEmpty);

        // 1405: 1 Farvardin (21 March 2026) is 1 Shawwal 1447; Ashura 1405-04-04 (25 June 2026) is 10 Muharram 1448
        Assert.Equal(new HijriDate(1447, 10, 1), ut.TryGet(new DateTime(2026, 3, 21)));
        Assert.Equal(new HijriDate(1448, 1, 9), ut.TryGet(new DateTime(2026, 6, 24)));
        Assert.Equal(new HijriDate(1448, 1, 10), ut.TryGet(new DateTime(2026, 6, 25)));
        Assert.Equal(9, ut.TryGet(new DateTime(2026, 3, 20))?.Month);   // the day before is the end of Ramadan
    }

    [Fact]
    public void Days_outside_the_official_ranges_have_no_answer()
    {
        var ut = RealUt();
        Assert.Null(ut.TryGet(new DateTime(2040, 1, 1)));
        Assert.Null(ut.TryGet(new DateTime(1990, 1, 1)));
    }

    [Fact]
    public void Selected_official_source_is_used_where_it_has_data_and_umalqura_everywhere_else()
    {
        var ut = RealUt();
        var provider = new HijriProvider(() => HijriProvider.UtId, ut);

        var covered = provider.Get(new DateTime(2026, 6, 25), out bool official1);
        Assert.True(official1);
        Assert.Equal(new HijriDate(1448, 1, 10), covered);

        var fallback = provider.Get(new DateTime(2040, 1, 1), out bool official2);
        Assert.False(official2);
        Assert.NotNull(fallback);
    }

    [Fact]
    public void Umalqura_selected_never_reports_official()
    {
        var provider = new HijriProvider(() => HijriProvider.UmAlQuraId, RealUt());
        var h = provider.Get(new DateTime(2026, 6, 25), out bool official);
        Assert.False(official);
        Assert.NotNull(h);
        Assert.InRange(h!.Value.Day, 1, 30);
    }

    [Fact]
    public void Umalqura_is_null_outside_its_supported_range()
    {
        var src = new UmAlQuraHijriSource();
        Assert.Null(src.TryGet(new DateTime(1800, 1, 1)));
        Assert.Null(src.TryGet(new DateTime(2100, 1, 1)));
        Assert.NotNull(src.TryGet(new DateTime(2026, 11, 5)));
    }

    [Fact]
    public void Broken_or_unsupported_data_gives_an_empty_source_and_never_throws()
    {
        var log = new List<string>();
        Assert.True(UtHijriSource.FromJson("not json", log.Add).IsEmpty);
        Assert.True(UtHijriSource.FromJson("""{ "schemaVersion": 99, "ranges": [], "months": [] }""", log.Add).IsEmpty);
        Assert.True(UtHijriSource.FromJson("""{ "schemaVersion": 1 }""", log.Add).IsEmpty);
        Assert.Equal(3, log.Count);
        Assert.True(UtHijriSource.Load(Path.Combine(Path.GetTempPath(), "missing-" + Guid.NewGuid().ToString("N") + ".json")).IsEmpty);
    }

    [Fact]
    public void Shipped_file_is_consistent()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(UtPath));
        var months = doc.RootElement.GetProperty("months").EnumerateArray()
            .Select(m => (Year: m[0].GetInt32(), Month: m[1].GetInt32(), Start: DateTime.Parse(m[2].GetString()!, System.Globalization.CultureInfo.InvariantCulture)))
            .ToList();
        Assert.NotEmpty(months);

        // a Hijri month has 29 or 30 days, and consecutive entries are consecutive months
        for (int i = 1; i < months.Count; i++)
        {
            var (a, b) = (months[i - 1], months[i]);
            int length = (b.Start - a.Start).Days;
            if (length > 30) continue;   // a gap between two covered ranges
            Assert.InRange(length, 29, 30);
            Assert.True((b.Year == a.Year && b.Month == a.Month + 1) || (b.Year == a.Year + 1 && a.Month == 12 && b.Month == 1), $"{a} -> {b}");
        }
    }
}
