using System.Globalization;
using Rooznama.Services;
using Xunit;

namespace Rooznama.Tests;

public sealed class CalendarEventServiceTests : IDisposable
{
    readonly string _tmp = Path.Combine(Path.GetTempPath(), "rooznama-tests-" + Guid.NewGuid().ToString("N"));
    readonly List<string> _log = new();

    static string RealRoot => Path.Combine(AppContext.BaseDirectory, "calendar");

    CalendarEventService Real() => new(RealRoot, _log.Add);
    CalendarEventService Temp() => new(_tmp, _log.Add);

    void Write(string relative, string json)
    {
        var path = Path.Combine(_tmp, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json);
    }

    static string YearJson(int year, string items, int schema = 1, string extra = "") =>
        $$"""{ "year": {{year}}, "schemaVersion": {{schema}}, {{extra}} "items": [ {{items}} ] }""";

    public void Dispose() { try { Directory.Delete(_tmp, true); } catch { } }

    // 1 + 2: the actual 1405 files
    [Fact]
    public async Task Real_1405_holidays_deserialize()
    {
        var y = await Real().GetYearAsync(1405);
        var h = Assert.IsType<CalendarYearFile>(y?.Holidays);
        Assert.Equal(1405, h.Year);
        Assert.Equal(1, h.SchemaVersion);
        Assert.False(string.IsNullOrEmpty(h.DataVersion));
        Assert.NotEmpty(h.Items);
        Assert.All(h.Items, i => Assert.True(i.IsHoliday));
        var nowruz = Assert.Single(h.Items, i => i.Id == "1405-nowruz-1");
        Assert.Equal("1405-01-01", nowruz.Date);
        Assert.Contains("nowruz", nowruz.Categories);
    }

    [Fact]
    public async Task Real_1405_events_deserialize()
    {
        var y = await Real().GetYearAsync(1405);
        var e = Assert.IsType<CalendarYearFile>(y?.Events);
        Assert.Equal(1405, e.Year);
        Assert.Equal("non-holiday-observances", e.Coverage);
        Assert.NotEmpty(e.Items);
        Assert.All(e.Items, i => Assert.False(i.IsHoliday));
        Assert.Equal("1405-01-06", e.Items.Single(i => i.Id == "1405-evt-001").Date);
    }

    [Fact]
    public async Task Real_meta_lists_the_years()
    {
        var m = await Real().GetMetaAsync();
        Assert.NotNull(m);
        Assert.Contains(1405, m!.Years);
        Assert.Equal(1, m.SchemaVersion);
        Assert.False(string.IsNullOrEmpty(m.DataVersion));
        Assert.True(m.Coverage!.ContainsKey("holidays"));
    }

    // 3 + 4 + 5: optional fields
    [Fact]
    public async Task Missing_english_title_is_null_not_an_error()
    {
        var y = await Real().GetYearAsync(1405);
        Assert.All(y!.Events!.Items, i => { Assert.Null(i.Title.En); Assert.False(string.IsNullOrEmpty(i.Title.Fa)); });
        Assert.Equal("Eid al-Fitr", y.Holidays!.Items.Single(i => i.Id == "1405-eid-fitr-1-early").Title.En);
    }

    [Fact]
    public async Task Missing_ruleId_is_null_and_present_ruleId_is_kept()
    {
        var y = await Real().GetYearAsync(1405);
        Assert.Null(y!.Holidays!.Items.Single(i => i.Id == "1405-eid-fitr-1-early").RuleId);
        Assert.Equal("nowruz-1", y.Holidays.Items.Single(i => i.Id == "1405-nowruz-1").RuleId);
    }

    [Fact]
    public async Task Missing_notes_is_null_and_present_notes_are_kept()
    {
        Write("years/1500/holidays.json", YearJson(1500, """{ "id": "a", "date": "1500-01-01", "title": { "fa": "الف" }, "isHoliday": true, "categories": [] }"""));
        var y = await Temp().GetYearAsync(1500);
        Assert.Null(y!.Holidays!.Notes);
        Assert.Null(y.Holidays.Items[0].Notes);
        Assert.Null(y.Holidays.Items[0].RuleId);

        var real = await Real().GetYearAsync(1405);
        Assert.NotEmpty(real!.Holidays!.Notes!);    // 1405 holidays.json carries notes, events.json of 1383 does not
        var old = await Real().GetYearAsync(1383);
        Assert.Null(old!.Events!.Notes);
    }

    // 6 + 7 + 8
    [Fact]
    public async Task Several_records_on_one_date_are_all_returned()
    {
        var s = Real();
        Assert.True((await s.GetEventsForDateAsync("1405-01-01")).Count >= 2);                      // Eid al-Fitr + Nowruz
        Assert.Equal(2, (await s.GetHolidaysForDateAsync("1405-01-01")).Count);
        var obs = await s.GetEventsForDateAsync("1405-01-06");                                      // two observances
        Assert.Equal(new[] { "1405-evt-001", "1405-evt-002" }, obs.Select(i => i.Id).OrderBy(x => x).ToArray());
    }

    [Fact]
    public async Task Holidays_and_events_are_combined_without_losing_records()
    {
        var y = (await Real().GetYearAsync(1405))!;
        Assert.Equal(y.Holidays!.Items.Count + y.Events!.Items.Count, y.Items.Count);
        var ids = y.Items.Select(i => i.Id).ToHashSet();
        Assert.All(y.Holidays.Items.Concat(y.Events.Items), i => Assert.Contains(i.Id, ids));
        Assert.Equal(y.Items.Count, (await Real().GetEventsForYearAsync(1405)).Count);
    }

    [Fact]
    public async Task Events_are_found_by_jalali_date()
    {
        var day = await Real().GetEventsForDateAsync("1405-01-13");
        Assert.Contains(day, i => i.Id == "1405-sizdah-bedar" && i.Title.En == "Nature Day (Sizdah Bedar)");
        Assert.Empty(await Real().GetEventsForDateAsync("1405-01-14x"));      // not a date key
        Assert.Empty(await Real().GetEventsForDateAsync("garbage"));
    }

    // 9: Friday is not an official holiday record
    [Fact]
    public async Task Fridays_are_not_official_holidays_unless_the_data_says_so()
    {
        var s = Real();
        var pc = new PersianCalendar();
        int plainFridays = 0;
        for (int m = 1; m <= 12; m++)
            for (int d = 1; d <= pc.GetDaysInMonth(1405, m); d++)
            {
                if (pc.ToDateTime(1405, m, d, 0, 0, 0, 0).DayOfWeek != DayOfWeek.Friday) continue;
                var key = CalendarEventService.Key(1405, m, d);
                bool listed = (await s.GetHolidaysForDateAsync(key)).Count > 0;
                Assert.Equal(listed, await s.IsOfficialHolidayAsync(key));
                if (!listed) plainFridays++;
            }
        Assert.True(plainFridays > 30);                                        // most Fridays carry no holiday record
        Assert.True(await s.IsOfficialHolidayAsync("1405-01-01"));
        Assert.False(await s.IsOfficialHolidayAsync("1405-01-06"));            // observance only
    }

    // 10 + 11 + 12
    [Fact]
    public async Task Missing_year_gives_empty_results()
    {
        var s = Real();
        Assert.Null(await s.GetYearAsync(1300));
        Assert.Empty(await s.GetEventsForYearAsync(1300));
        Assert.Empty(await s.GetEventsForDateAsync("1300-01-01"));
        Assert.False(await s.IsOfficialHolidayAsync("1300-01-01"));

        Assert.Null(await Temp().GetYearAsync(1405));                          // no meta.json, no years folder
    }

    [Fact]
    public async Task One_missing_file_still_loads_the_other()
    {
        Write("years/1501/events.json", YearJson(1501, """{ "id": "e", "date": "1501-02-03", "title": { "fa": "ب" }, "isHoliday": false, "categories": ["x"] }"""));
        var y = await Temp().GetYearAsync(1501);
        Assert.Null(y!.Holidays);
        Assert.Single(y.Items);
    }

    [Fact]
    public async Task Malformed_json_is_logged_and_does_not_throw()
    {
        Write("years/1502/holidays.json", "{ this is not json");
        Write("years/1502/events.json", YearJson(1502, """{ "id": "e", "date": "1502-01-01", "title": { "fa": "ج" }, "isHoliday": false, "categories": [] }"""));
        var y = await Temp().GetYearAsync(1502);
        Assert.Null(y!.Holidays);
        Assert.Single(y.Items);
        Assert.Contains(_log, l => l.Contains("holidays.json"));

        Write("years/1503/events.json", "");
        Write("years/1503/holidays.json", "[1,2,3]");
        Assert.Null(await Temp().GetYearAsync(1503));
    }

    [Fact]
    public async Task Item_missing_a_required_field_invalidates_only_that_file()
    {
        Write("years/1504/holidays.json", YearJson(1504, """{ "id": "no-date", "title": { "fa": "د" }, "isHoliday": true }"""));
        Assert.Null(await Temp().GetYearAsync(1504));
    }

    [Fact]
    public async Task Unsupported_schema_version_is_rejected()
    {
        Write("years/1505/holidays.json", YearJson(1505, "", schema: 2));
        Assert.Null(await Temp().GetYearAsync(1505));
        Assert.Contains(_log, l => l.Contains("schemaVersion 2"));

        Write("meta.json", """{ "name": "x", "schemaVersion": 9, "years": [1506] }""");
        Write("years/1506/holidays.json", YearJson(1506, ""));
        var s = Temp();
        Assert.Null(await s.GetMetaAsync());
    }

    [Fact]
    public async Task A_file_for_the_wrong_year_is_rejected()
    {
        Write("years/1507/holidays.json", YearJson(1508, ""));
        Assert.Null(await Temp().GetYearAsync(1507));
    }

    // schema compatibility: nothing is thrown away
    [Fact]
    public async Task Unknown_fields_and_categories_are_preserved()
    {
        Write("years/1509/events.json", YearJson(1509,
            """{ "id": "u", "date": "1509-05-05", "title": { "fa": "ه", "de": "x" }, "isHoliday": false, "categories": ["brand-new-kind"], "futureField": 7 }""",
            extra: "\"futureTop\": true,"));
        var y = (await Temp().GetYearAsync(1509))!;
        var item = y.Items[0];
        Assert.Equal(new[] { "brand-new-kind" }, item.Categories);
        Assert.Equal(7, item.Extra!["futureField"].GetInt32());
        Assert.Equal("x", item.Title.Extra!["de"].GetString());
        Assert.True(y.Events!.Extra!["futureTop"].GetBoolean());
    }

    [Fact]
    public async Task A_year_is_loaded_once_and_cached()
    {
        Write("years/1510/holidays.json", YearJson(1510, """{ "id": "c", "date": "1510-01-01", "title": { "fa": "و" }, "isHoliday": true, "categories": [] }"""));
        var s = Temp();
        var first = await s.GetYearAsync(1510);
        File.Delete(Path.Combine(_tmp, "years/1510/holidays.json"));
        Assert.Same(first, await s.GetYearAsync(1510));
        Assert.True(await s.IsOfficialHolidayAsync("1510-01-01"));
    }
}
