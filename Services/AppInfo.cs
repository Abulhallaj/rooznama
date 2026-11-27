using System.Reflection;

namespace Rooznama.Services;

public static class AppInfo
{
    public const string RepoOwner = "Abulhallaj";
    public const string RepoName = "rooznama";
    public const string RepoUrl = "https://github.com/Abulhallaj/rooznama";
    public const string IssuesUrl = RepoUrl + "/issues";
    public const string ReleasesUrl = RepoUrl + "/releases";
    public const string SupportUrl = "https://www.coffeete.ir/abulhallaj";

    // Not hard-coded: MinVer derives it from the git tags (tag v0.11.0 -> 0.11.0), so a release always matches its GitHub release.
    // Commits after a tag are 0.11.1-dev.0.<commits since the tag>.
    public static string VersionText { get; } = ReadText();

    /// <summary>Local / CI development build (not installed by Velopack): update checks are skipped.</summary>
    public static bool IsDev => !UpdateService.IsInstalled;

    public static Version Normalize(Version? v) =>
        v is null ? new Version(0, 0, 0) : new Version(v.Major, v.Minor, Math.Max(v.Build, 0));

    static string ReadText()
    {
        var info = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrWhiteSpace(info)) return "0.0.0";

        // 0.11.1-dev.0.5+1a2b3c4d... -> pre-release and dev builds also show the short commit, so it is clear exactly which build runs
        var parts = info.Split('+');
        return parts.Length > 1 && parts[0].Contains('-')
            ? $"{parts[0]}+{parts[1][..Math.Min(7, parts[1].Length)]}"
            : parts[0];
    }
}
