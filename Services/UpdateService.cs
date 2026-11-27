using Microsoft.UI.Dispatching;
using Velopack;
using Velopack.Sources;

namespace Rooznama.Services;

public sealed record UpdateInfo(Version Version, string Tag, string Url);
public sealed record CheckResult(bool Ok, UpdateInfo? Update);

/// <summary>
/// Checks the GitHub releases through Velopack (no personal data) and notifies once per new version.
/// Installing downloads only the changed files (delta packages) and restarts the app.
/// </summary>
public static class UpdateService
{
    static DispatcherQueueTimer? _first, _periodic;
    static UpdateManager? _manager;
    static Velopack.UpdateInfo? _pending;

    public static UpdateInfo? Available { get; private set; }

    /// <summary>true when installed by Velopack (installer or portable ZIP); false for a plain publish / debug build.</summary>
    public static bool IsInstalled => Manager.IsInstalled;

    // A beta (0.11.0-beta.1) also follows pre-releases; a stable version follows stable releases only.
    static UpdateManager Manager => _manager ??=
        new UpdateManager(new GithubSource(AppInfo.RepoUrl, null, AppInfo.VersionText.Contains('-')));

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
            // not installed by Velopack (plain publish folder, built from source): nothing to update in place
            if (!IsInstalled)
            {
                Available = null;
                return new CheckResult(true, null);
            }

            var info = await Manager.CheckForUpdatesAsync();
            MarkChecked();
            _pending = info;
            if (info is null)
            {
                Available = null;
                return new CheckResult(true, null);
            }

            var tag = "v" + info.TargetFullRelease.Version;
            TryParseTag(tag, out var latest);
            Available = new UpdateInfo(latest, tag, $"{AppInfo.ReleasesUrl}/tag/{tag}");
            return new CheckResult(true, Available);
        }
        catch (Exception ex)
        {
            Log.Write("update check failed: " + ex.Message);
            return new CheckResult(false, null);
        }
    }

    /// <summary>Downloads the update (only the changed files when a delta exists), then restarts into the new version.</summary>
    /// <returns>false if it failed; on success the process exits and this never returns.</returns>
    public static async Task<bool> InstallAsync(Action<int>? progress = null)
    {
        try
        {
            if (_pending is null) return false;
            await Manager.DownloadUpdatesAsync(_pending, progress);
            App.Tray.Dispose();   // so the tray icon doesn't linger after the process exits
            Manager.ApplyUpdatesAndRestart(_pending.TargetFullRelease);
            return true;
        }
        catch (Exception ex)
        {
            Log.Write("update install failed: " + ex.Message);
            return false;
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
