using System.Text.Json;
using Microsoft.Win32;

namespace Rooznama.Services;

public sealed class AppSettings
{
    public bool ShowGregorianInTooltip { get; set; } = true;
    public string IconColor { get; set; } = "#0078D7";
    public bool AutoHide { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public bool CalendarCollapsed { get; set; }
    public string Language { get; set; } = "fa";          // fa | en
    public string FontName { get; set; } = "Vazirmatn";   // Vazirmatn | Tahoma
    public string Theme { get; set; } = "auto";           // auto | light | dark
    public bool CustomTooltip { get; set; } = true;       // own right-to-left tray tooltip instead of the system one
    public bool CheckUpdates { get; set; } = true;
    public DateTime LastUpdateCheckUtc { get; set; }
    public string LastNotifiedVersion { get; set; } = "";
    public string HijriSource { get; set; } = "ut";       // ut (Tehran University Calendar Center) | umalqura; umalqura also fills any day ut has no data for
    public bool ShowHijriInTooltip { get; set; } = true;  // Hijri date in the day hover text and the tray tooltip
    public bool ShowHijriInCells { get; set; } = true;    // small Hijri day in the large calendar's cells
    public bool ShowWeekNumbers { get; set; } = true;     // week-of-year column in the large calendar
    public bool Experimental { get; set; }                // experimental features (today: Google Calendar sync). Off = nothing of them is shown, created or started
    public string GoogleClientId { get; set; } = "";      // OAuth "Desktop app" client of the person's own Google Cloud project
    public string GoogleClientSecret { get; set; } = "";  // (Google calls it a secret, but a desktop app cannot keep it secret)
    public bool GoogleAutoSync { get; set; } = true;      // sync every 15 minutes and after an edit
    public string GoogleAccount { get; set; } = "";       // e-mail of the account events were last synced with
    public DateTime GoogleLastSyncUtc { get; set; }

    static string FilePath => Path.Combine(AppPaths.DataDir, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new();
        }
        catch { }
        return new();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    public void ApplyStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (key is null) return;
            if (StartWithWindows && Environment.ProcessPath is { } exe)
                key.SetValue("Rooznama", $"\"{exe}\"");
            else
                key.DeleteValue("Rooznama", false);
        }
        catch { }
    }
}
