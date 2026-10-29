namespace Rooznama.Services;

/// <summary>Portable mode: a "portable.flag" file next to the exe keeps settings and log in .\data instead of %AppData%.</summary>
public static class AppPaths
{
    public static bool IsPortable { get; } = File.Exists(Path.Combine(AppContext.BaseDirectory, "portable.flag"));

    public static string DataDir { get; } = IsPortable
        ? Path.Combine(AppContext.BaseDirectory, "data")
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Rooznama");
}
