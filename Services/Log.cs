namespace Rooznama.Services;

/// <summary>Tiny diagnostic log: rooznama.log in the data folder (trimmed at 100 KB).</summary>
public static class Log
{
    static readonly object Gate = new();

    static string FilePath => Path.Combine(AppPaths.DataDir, "rooznama.log");

    public static void Write(string message)
    {
        try
        {
            lock (Gate)
            {
                var f = FilePath;
                Directory.CreateDirectory(Path.GetDirectoryName(f)!);
                if (File.Exists(f) && new FileInfo(f).Length > 100_000) File.Delete(f);
                File.AppendAllText(f, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {message}{Environment.NewLine}");
            }
        }
        catch { }
    }
}
