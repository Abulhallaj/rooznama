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

    // Not hard-coded: the release workflow builds with -p:Version=<git tag>, so this always matches the GitHub release.
    public static Version Version { get; } = Normalize(Assembly.GetExecutingAssembly().GetName().Version);
    public static string VersionText { get; } = ReadText();

    /// <summary>Local / CI development build (0.0.0-dev…): update checks are skipped.</summary>
    public static bool IsDev => Version.Major == 0 && Version.Minor == 0 && Version.Build == 0;

    public static Version Normalize(Version? v) =>
        v is null ? new Version(0, 0, 0) : new Version(v.Major, v.Minor, Math.Max(v.Build, 0));

    static string ReadText()
    {
        var info = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return string.IsNullOrWhiteSpace(info) ? Version.ToString(3) : info.Split('+')[0];
    }
}
