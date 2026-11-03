using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

namespace Rooznama.Services;

/// <summary>
/// Reads the bundled Iranian calendar dataset (calendar/meta.json and calendar/years/{year}/holidays.json + events.json).
/// Works fully offline, loads each year once (cached), never throws for missing or broken files and never blocks the UI thread.
/// Dates are the dataset's Jalali strings (YYYY-MM-DD); converting a DateTime is left to PersianDate.
/// Weekends (Friday) are not part of the dataset and are handled by the UI separately.
/// </summary>
public sealed class CalendarEventService
{
    public const int SupportedSchemaVersion = 1;

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    readonly string _root;
    readonly Action<string>? _log;
    readonly Lazy<Task<CalendarMeta?>> _meta;
    readonly ConcurrentDictionary<int, Lazy<Task<CalendarYear?>>> _years = new();

    /// <param name="rootDirectory">The folder that contains meta.json and years\.</param>
    /// <param name="log">Optional diagnostics sink (the app passes Log.Write).</param>
    public CalendarEventService(string rootDirectory, Action<string>? log = null)
    {
        _root = rootDirectory;
        _log = log;
        _meta = new Lazy<Task<CalendarMeta?>>(LoadMetaAsync);
    }

    /// <summary>"YYYY-MM-DD" in the dataset's format.</summary>
    public static string Key(int year, int month, int day) =>
        FormattableString.Invariant($"{year:0000}-{month:00}-{day:00}");

    public Task<CalendarMeta?> GetMetaAsync() => _meta.Value;

    /// <summary>The combined year, or null when the year has no usable file.</summary>
    public Task<CalendarYear?> GetYearAsync(int year) =>
        _years.GetOrAdd(year, y => new Lazy<Task<CalendarYear?>>(() => LoadYearAsync(y))).Value;

    public async Task<IReadOnlyList<CalendarItem>> GetEventsForYearAsync(int year) =>
        (await GetYearAsync(year).ConfigureAwait(false))?.Items ?? Array.Empty<CalendarItem>();

    /// <summary>All records (holidays and observances) of a Jalali date; several records per date are all returned.</summary>
    public async Task<IReadOnlyList<CalendarItem>> GetEventsForDateAsync(string jalaliDate)
    {
        if (!TryGetYear(jalaliDate, out int year)) return Array.Empty<CalendarItem>();
        var y = await GetYearAsync(year).ConfigureAwait(false);
        return y?.ForDate(jalaliDate) ?? Array.Empty<CalendarItem>();
    }

    public async Task<IReadOnlyList<CalendarItem>> GetHolidaysForDateAsync(string jalaliDate) =>
        (await GetEventsForDateAsync(jalaliDate).ConfigureAwait(false)).Where(i => i.IsHoliday).ToList();

    public async Task<bool> IsOfficialHolidayAsync(string jalaliDate) =>
        (await GetEventsForDateAsync(jalaliDate).ConfigureAwait(false)).Any(i => i.IsHoliday);

    static bool TryGetYear(string date, out int year)
    {
        year = 0;
        return date is { Length: 10 } && date[4] == '-' && date[7] == '-'
               && int.TryParse(date.AsSpan(0, 4), NumberStyles.None, CultureInfo.InvariantCulture, out year);
    }

    // ---------- loading ----------
    async Task<CalendarMeta?> LoadMetaAsync()
    {
        var meta = await ReadJsonAsync<CalendarMeta>(Path.Combine(_root, "meta.json")).ConfigureAwait(false);
        if (meta is not null && meta.SchemaVersion != SupportedSchemaVersion)
        {
            _log?.Invoke($"calendar: meta.json has unsupported schemaVersion {meta.SchemaVersion}; ignored");
            return null;
        }
        return meta;
    }

    async Task<CalendarYear?> LoadYearAsync(int year)
    {
        // meta.json lists the available years; if it is unreadable we still try the files directly
        var meta = await GetMetaAsync().ConfigureAwait(false);
        if (meta is { Years.Count: > 0 } && !meta.Years.Contains(year)) return null;

        var dir = Path.Combine(_root, "years", year.ToString(CultureInfo.InvariantCulture));
        var holidays = await ReadYearFileAsync(Path.Combine(dir, "holidays.json"), year).ConfigureAwait(false);
        var events = await ReadYearFileAsync(Path.Combine(dir, "events.json"), year).ConfigureAwait(false);
        return holidays is null && events is null ? null : new CalendarYear(year, holidays, events);
    }

    async Task<CalendarYearFile?> ReadYearFileAsync(string path, int year)
    {
        var file = await ReadJsonAsync<CalendarYearFile>(path).ConfigureAwait(false);
        if (file is null) return null;

        if (file.SchemaVersion != SupportedSchemaVersion)
        {
            _log?.Invoke($"calendar: {path} has unsupported schemaVersion {file.SchemaVersion}; ignored");
            return null;
        }
        if (file.Year != year)
        {
            _log?.Invoke($"calendar: {path} contains year {file.Year}, expected {year}; ignored");
            return null;
        }
        // kept either way (nothing is discarded), but worth a log line
        foreach (var item in file.Items.Where(i => !IsDate(i.Date)))
            _log?.Invoke($"calendar: {path}: item '{item.Id}' has a malformed date '{item.Date}'");
        return file;
    }

    static bool IsDate(string s) =>
        s is { Length: 10 } && s[4] == '-' && s[7] == '-'
        && s.Where((c, i) => i != 4 && i != 7).All(char.IsAsciiDigit);

    /// <summary>Null when the file is missing or invalid; the reason goes to the log. Never throws.</summary>
    async Task<T?> ReadJsonAsync<T>(string path) where T : class
    {
        try
        {
            if (!File.Exists(path)) return null;
            await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            return await JsonSerializer.DeserializeAsync<T>(fs, JsonOptions).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            _log?.Invoke($"calendar: cannot read {path}: {ex.Message}");
            return null;
        }
    }
}
