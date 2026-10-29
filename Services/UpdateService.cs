using System.Text.Json;
using Microsoft.UI.Dispatching;

namespace Rooznama.Services;

public sealed record UpdateInfo(Version Version, string Tag, string Url);
public sealed record CheckResult(bool Ok, UpdateInfo? Update);

/// <summary>Checks the latest GitHub release (public API, no personal data) and notifies once per new version.</summary>
public static class UpdateService
{
    static readonly HttpClient Http = CreateClient();
    static DispatcherQueueTimer? _first, _periodic;

    public static UpdateInfo? Available { get; private set; }

    static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd($"Rooznama/{AppInfo.VersionText}");
        c.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return c;
    }

    /// <summary>First check ~30 s after start, then every 6 h (the once-per-day rule is enforced in AutoCheckAsync).</summary>
    public static void Start()
    {
        var dq = DispatcherQueue.GetForCurrentThread();

        _first = dq.CreateTimer();
        _first.IsRepeating = false;
        _first.Interval = TimeSpan.FromSeconds(30);
        _first.Tick += async (_, _) => await AutoCheckAsync();
        _first.Start();

        _periodic = dq.CreateTimer();
        _periodic.Interval = TimeSpan.FromHours(6);
        _periodic.Tick += async (_, _) => await AutoCheckAsync();
        _periodic.Start();
    }

    static async Task AutoCheckAsync()
    {
        var s = App.Settings;
        if (!s.CheckUpdates || AppInfo.IsDev) return;
        if (DateTime.UtcNow - s.LastUpdateCheckUtc < TimeSpan.FromHours(23)) return;

        var r = await CheckAsync();
        if (r.Update is { } u && s.LastNotifiedVersion != u.Tag)
        {
            s.LastNotifiedVersion = u.Tag;
            s.Save();
            App.Tray.Notify(Loc.T("app.name"), string.Format(Loc.T("upd.available"), u.Tag), u.Url);
        }
    }

    public static async Task<CheckResult> CheckAsync()
    {
        try
        {
            using var resp = await Http.GetAsync(
                $"https://api.github.com/repos/{AppInfo.RepoOwner}/{AppInfo.RepoName}/releases/latest");

            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)   // no release published yet
            {
                MarkChecked();
                Available = null;
                return new CheckResult(true, null);
            }
            resp.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            var tag = root.GetProperty("tag_name").GetString() ?? "";
            var url = root.TryGetProperty("html_url", out var u) ? u.GetString() ?? AppInfo.ReleasesUrl : AppInfo.ReleasesUrl;

            MarkChecked();
            if (TryParseTag(tag, out var latest) && latest > AppInfo.Version)
            {
                Available = new UpdateInfo(latest, tag, url);
                return new CheckResult(true, Available);
            }
            Available = null;
            return new CheckResult(true, null);
        }
        catch (Exception ex)
        {
            Log.Write("update check failed: " + ex.Message);
            return new CheckResult(false, null);
        }
    }

    static void MarkChecked()
    {
        App.Settings.LastUpdateCheckUtc = DateTime.UtcNow;
        App.Settings.Save();
    }

    static bool TryParseTag(string tag, out Version v)
    {
        var core = tag.Trim().TrimStart('v', 'V').Split('-', '+')[0];
        if (Version.TryParse(core, out var parsed))
        {
            v = AppInfo.Normalize(parsed);
            return true;
        }
        v = new Version(0, 0, 0);
        return false;
    }
}
