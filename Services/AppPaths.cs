namespace Rooznama.Services;

/// <summary>
/// Portable mode: a "portable.flag" file keeps settings and log in a "data" folder next to it instead of %AppData%.
/// Velopack layout: {root}\Update.exe and {root}\current\Rooznama.exe. "current" is replaced on every update, so in the portable
/// ZIP the flag (and "data") sit in {root}, one level above the exe. A flag next to the exe (plain publish folder) still works.
/// </summary>
public static class AppPaths
{
    static readonly string? PortableRoot = FindPortableRoot();

    public static bool IsPortable { get; } = PortableRoot is not null;

    public static string DataDir { get; } = PortableRoot is { } root
        ? Path.Combine(root, "data")
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Rooznama");

    static string? FindPortableRoot()
    {
        var exeDir = AppContext.BaseDirectory;
        if (File.Exists(Path.Combine(exeDir, "portable.flag"))) return exeDir;

        var parent = Path.GetFullPath(Path.Combine(exeDir, ".."));
        return File.Exists(Path.Combine(parent, "portable.flag")) ? parent : null;
    }
}
