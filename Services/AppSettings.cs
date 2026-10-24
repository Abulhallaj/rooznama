using System.Text.Json;
using Microsoft.Win32;

namespace Rooznama.Services;

public sealed class AppSettings
{
    public bool ShowGregorianInTooltip { get; set; } = true;
    public bool CalendarCollapsed { get; set; }
    public string Language { get; set; } = "fa";          // fa | en
    public string FontName { get; set; } = "Vazirmatn";   // Vazirmatn | Tahoma
    public string Theme { get; set; } = "auto";           // auto | light | dark
    public string IconColor { get; set; } = "#0078D7";
    public bool AutoHide { get; set; } = true;
    public bool StartWithWindows { get; set; }

    static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Rooznama", "settings.json");

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
