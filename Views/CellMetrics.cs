using System.Globalization;
using System.Text.Json;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Rooznama.Services;
using Windows.UI;

namespace Rooznama.Views;

/// <summary>Where a number sits in a day cell. "Start" is the side the text starts on: the right in Persian, the left in English.</summary>
public enum CellSlot { TopStart, TopCenter, TopEnd, CenterStart, Center, CenterEnd, BottomStart, BottomCenter, BottomEnd }

/// <summary>
/// Sizes, positions and colours of the large calendar's day cells, all in one place. In Debug builds the values can be overridden
/// by cell-metrics.json in the data folder: it is created with the shipped values on first run, watched while the app runs, and the
/// calendar redraws the moment it is saved. Release builds always use the shipped values below.
/// </summary>
internal static class CellMetrics
{
    // ---------- the values that ship (keep them equal to Template below) ----------
    public static int CellHeight => (int)Math.Clamp(Num("cellHeight", 64), 40, 160);
    /// <summary>Distance of a number in a corner or on an edge from the cell border, in pixels.</summary>
    public static double Edge => Num("edge", 6);

    public static double ShamsiSize => Num("shamsi.size", 18);
    public static Windows.UI.Text.FontWeight ShamsiWeight => Weight(Str("shamsi.weight", "SemiBold"));
    public static CellSlot ShamsiSlot => Slot("shamsi.slot", CellSlot.Center);
    /// <summary>Pixels the Shamsi number is moved up from the middle (only for the Center slots).</summary>
    public static double ShamsiLift => Num("shamsi.lift", 6);

    public static double GregorianSize => Num("gregorian.size", 11);
    public static double GregorianOpacity => Num("gregorian.opacity", 0.65);
    public static CellSlot GregorianSlot => Slot("gregorian.slot", CellSlot.BottomStart);

    public static double HijriSize => Num("hijri.size", 11);
    public static double HijriOpacity => Num("hijri.opacity", 0.65);
    public static CellSlot HijriSlot => Slot("hijri.slot", CellSlot.BottomEnd);

    /// <summary>Background of a holiday: a light red that is readable on the light and the dark theme (#AARRGGBB).</summary>
    public static Color HolidayBackground => Hex(Str("holiday", "#2EE53935"), Color.FromArgb(0x2E, 0xE5, 0x39, 0x35));
    /// <summary>Give Fridays the holiday background too (they already have red text).</summary>
    public static bool FridayBackground => Bool("fridayBackground", false);

    /// <summary>Puts a number in its slot: alignment plus the edge margin.</summary>
    public static void Place(FrameworkElement e, CellSlot slot, double lift = 0)
    {
        int row = (int)slot / 3, col = (int)slot % 3;   // rows: top, middle, bottom · columns: start, middle, end
        e.VerticalAlignment = row == 0 ? VerticalAlignment.Top : row == 1 ? VerticalAlignment.Center : VerticalAlignment.Bottom;
        e.HorizontalAlignment = col == 0 ? HorizontalAlignment.Left : col == 1 ? HorizontalAlignment.Center : HorizontalAlignment.Right;
        double edge = Edge;
        e.Margin = new Thickness(col == 0 ? edge : 0, row == 0 ? edge : 0, col == 2 ? edge : 0,
                                 (row == 2 ? edge : 0) + (row == 1 ? lift * 2 : 0));   // a bottom margin of 2x moves a centred item up by x
    }

    // ---------- live override file (Debug builds) ----------
    static JsonDocument? _doc = null;

    /// <summary>Raised (on a background thread) after cell-metrics.json was saved and read again. Always silent in Release builds.</summary>
#pragma warning disable CS0067   // never raised in Release builds
    public static event Action? Changed;
#pragma warning restore CS0067

    public static string FilePath => Path.Combine(AppPaths.DataDir, "cell-metrics.json");

#if DEBUG
    static FileSystemWatcher? _watcher;
    static Timer? _debounce;

    const string Template = """
{
  // Edit and save: the calendar redraws at once (Debug builds only). Delete a line to get its default back.
  // Slots: TopStart TopCenter TopEnd CenterStart Center CenterEnd BottomStart BottomCenter BottomEnd
  // "Start" is the side the text starts on: the right in Persian, the left in English.
  "cellHeight": 64,
  "edge": 6,
  "shamsi":    { "size": 18, "weight": "SemiBold", "slot": "Center", "lift": 6 },
  "gregorian": { "size": 11, "opacity": 0.65, "slot": "BottomStart" },
  "hijri":     { "size": 11, "opacity": 0.65, "slot": "BottomEnd" },
  "holiday": "#2EE53935",
  "fridayBackground": false
}
""";
#endif

    /// <summary>Debug builds: creates cell-metrics.json on first use, loads it and watches it. Does nothing in Release builds.</summary>
    public static void StartLive()
    {
#if DEBUG
        if (_watcher is not null) return;
        try
        {
            Directory.CreateDirectory(AppPaths.DataDir);
            if (!File.Exists(FilePath)) File.WriteAllText(FilePath, Template);
            Reload();
            _watcher = new FileSystemWatcher(AppPaths.DataDir, "cell-metrics.json")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName | NotifyFilters.CreationTime,
                EnableRaisingEvents = true
            };
            _watcher.Changed += (_, _) => Schedule();
            _watcher.Created += (_, _) => Schedule();
            _watcher.Renamed += (_, _) => Schedule();   // some editors save through a temporary file
            Log.Write("cell metrics (live): " + FilePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            Log.Write("cell metrics: " + ex.Message);
        }
#endif
    }

#if DEBUG
    static void Schedule()
    {
        _debounce?.Dispose();   // editors write the file in several steps
        _debounce = new Timer(_ => { Reload(); Changed?.Invoke(); }, null, 200, Timeout.Infinite);
    }

    static void Reload()
    {
        try
        {
            using var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            _doc = JsonDocument.Parse(fs, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            Log.Write("cell metrics: kept the previous values (" + ex.Message + ")");   // half-typed JSON must not reset the calendar
        }
    }
#endif

    // ---------- reading values ----------
    static bool TryGet(string path, out JsonElement value)
    {
        value = default;
        var doc = _doc;
        if (doc is null) return false;
        var e = doc.RootElement;
        foreach (var part in path.Split('.'))
        {
            if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(part, out var next)) return false;
            e = next;
        }
        value = e;
        return true;
    }

    static double Num(string path, double def) => TryGet(path, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : def;
    static string Str(string path, string def) => TryGet(path, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? def : def;
    static bool Bool(string path, bool def) => TryGet(path, out var v) && (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False) ? v.GetBoolean() : def;

    static CellSlot Slot(string path, CellSlot def) => Enum.TryParse<CellSlot>(Str(path, ""), true, out var s) ? s : def;

    static Windows.UI.Text.FontWeight Weight(string name) => name.ToLowerInvariant() switch
    {
        "thin" => FontWeights.Thin,
        "light" => FontWeights.Light,
        "normal" or "regular" => FontWeights.Normal,
        "medium" => FontWeights.Medium,
        "bold" => FontWeights.Bold,
        "extrabold" => FontWeights.ExtraBold,
        _ => FontWeights.SemiBold
    };

    static Color Hex(string text, Color fallback)
    {
        var s = text.Trim().TrimStart('#');
        if (!uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v)) return fallback;
        return s.Length switch
        {
            6 => Color.FromArgb(0xFF, (byte)(v >> 16), (byte)(v >> 8), (byte)v),
            8 => Color.FromArgb((byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v),
            _ => fallback
        };
    }
}
