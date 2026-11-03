using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rooznama.Services;

// Models for the bundled Iranian calendar dataset (calendar/). The property names are the dataset's own;
// optional fields stay nullable and unknown fields are kept in Extra, so newer data files never lose information.

/// <summary>calendar/meta.json</summary>
public sealed class CalendarMeta
{
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("schemaVersion")] public int SchemaVersion { get; init; }
    [JsonPropertyName("dataVersion")] public string? DataVersion { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("years")] public List<int> Years { get; init; } = new();
    [JsonPropertyName("coverage")] public Dictionary<string, string>? Coverage { get; init; }
    [JsonPropertyName("license")] public Dictionary<string, string>? License { get; init; }
    [JsonPropertyName("source")] public string? Source { get; init; }
    [JsonPropertyName("cdn")] public Dictionary<string, string>? Cdn { get; init; }
    [JsonPropertyName("publishedAt")] public string? PublishedAt { get; init; }
    [JsonPropertyName("notes")] public List<string>? Notes { get; init; }

    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
}

/// <summary>calendar/years/{year}/holidays.json or events.json</summary>
public sealed class CalendarYearFile
{
    [JsonPropertyName("year")] public required int Year { get; init; }
    [JsonPropertyName("schemaVersion")] public required int SchemaVersion { get; init; }
    [JsonPropertyName("dataVersion")] public string? DataVersion { get; init; }
    [JsonPropertyName("source")] public string? Source { get; init; }
    [JsonPropertyName("coverage")] public string? Coverage { get; init; }
    [JsonPropertyName("notes")] public List<string>? Notes { get; init; }
    [JsonPropertyName("items")] public required List<CalendarItem> Items { get; init; }

    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
}

public sealed class CalendarItem
{
    [JsonPropertyName("id")] public required string Id { get; init; }
    /// <summary>Jalali date exactly as stored in the dataset: YYYY-MM-DD.</summary>
    [JsonPropertyName("date")] public required string Date { get; init; }
    [JsonPropertyName("ruleId")] public string? RuleId { get; init; }
    [JsonPropertyName("title")] public required LocalizedTitle Title { get; init; }
    [JsonPropertyName("isHoliday")] public bool IsHoliday { get; init; }
    /// <summary>Free-form strings (not an enum): unknown categories are kept as they are.</summary>
    [JsonPropertyName("categories")] public List<string> Categories { get; init; } = new();
    /// <summary>Per-item note (a string); present in a few holiday records only.</summary>
    [JsonPropertyName("notes")] public string? Notes { get; init; }

    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
}

public sealed class LocalizedTitle
{
    [JsonPropertyName("fa")] public required string Fa { get; init; }
    [JsonPropertyName("en")] public string? En { get; init; }

    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
}

/// <summary>One Jalali year: holidays.json and events.json combined (either file may be absent).</summary>
public sealed class CalendarYear
{
    readonly Dictionary<string, CalendarItem[]> _byDate;

    public int Year { get; }
    public CalendarYearFile? Holidays { get; }
    public CalendarYearFile? Events { get; }

    /// <summary>Every record of both files: holidays first, then events. Nothing is merged or dropped.</summary>
    public IReadOnlyList<CalendarItem> Items { get; }

    public CalendarYear(int year, CalendarYearFile? holidays, CalendarYearFile? events)
    {
        Year = year;
        Holidays = holidays;
        Events = events;
        Items = (holidays?.Items ?? Enumerable.Empty<CalendarItem>()).Concat(events?.Items ?? Enumerable.Empty<CalendarItem>()).ToList();
        _byDate = Items.GroupBy(i => i.Date).ToDictionary(g => g.Key, g => g.ToArray());
    }

    public IReadOnlyList<CalendarItem> ForDate(string jalaliDate) =>
        _byDate.TryGetValue(jalaliDate, out var a) ? a : Array.Empty<CalendarItem>();
}
