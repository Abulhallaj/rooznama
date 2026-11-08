using System.Diagnostics;
using System.Drawing.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Rooznama.Services;

namespace Rooznama;

public partial class App : Application
{
    static Mutex? _mutex;
    static ShellWindow? _shell;

    public static AppSettings Settings { get; } = AppSettings.Load();

    // Personal events (a JSON file next to settings.json). Events is the provider the calendar reads;
    // swap or combine it later with a Google Calendar provider, nothing else has to change.
    public static PersonalEventStore PersonalEvents { get; } = new(Path.Combine(AppPaths.DataDir, "personal-events.json"), Log.Write);
    public static ICalendarEventProvider Events { get; set; } = new PersonalEventProvider(PersonalEvents);

    // Bundled offline holidays and observances (calendar\ next to the exe).
    public static CalendarEventService CalendarData { get; } =
        new(Path.Combine(AppContext.BaseDirectory, "calendar"), Log.Write);

    // Fixed rules per calendar (calendar\rules) merged with the yearly dataset; Hijri dates follow the selected source.
    public static CalendarRuleSet CalendarRules { get; } =
        CalendarRuleSet.Load(Path.Combine(AppContext.BaseDirectory, "calendar", "rules"), Log.Write);
    public static CalendarOccasionService Occasions { get; } = new(CalendarData, CalendarRules, HijriService.Provider);

    // Google Calendar: sign-in and the two-way sync of the personal events.
    static readonly HttpClient GoogleHttp = CreateHttp();
    public static GoogleAuth Google { get; } = new(GoogleHttp, GoogleClientConfig.Get, Path.Combine(AppPaths.DataDir, "google-token.bin"));
    public static GoogleSyncScheduler GoogleSync { get; } = new(Google, PersonalEvents, GoogleHttp);

    static HttpClient CreateHttp()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Rooznama/" + AppInfo.VersionText);
        return http;
    }

    public static FlyoutWindow Flyout { get; private set; } = null!;
    public static TrayService Tray { get; private set; } = null!;

    public App()
    {
        _mutex = new Mutex(true, "Rooznama.SingleInstance", out bool isNew);
        if (!isNew) Environment.Exit(0);

        // "auto" = leave it alone so the app follows the Windows light/dark setting live
        if (Settings.Theme == "dark") RequestedTheme = ApplicationTheme.Dark;
        else if (Settings.Theme == "light") RequestedTheme = ApplicationTheme.Light;

        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        ApplyFont();
        _ = CalendarData.GetYearAsync(PersianDate.Year(DateTime.Today));   // warm the cache off the UI thread
        Flyout = new FlyoutWindow();   // created hidden; shown from the tray
        Tray = new TrayService();

        if (Settings.StartWithWindows) Settings.ApplyStartup();   // keeps the Run path current if the exe was moved/updated
        UpdateService.Start();
        GoogleSync.Start();   // does nothing unless Google is connected
    }

    /// <summary>The FontFamily string actually applied (shown in About for diagnosis).</summary>
    public static string FontSource { get; private set; } = "";

    // Must run before any window is created.
    static void ApplyFont()
    {
        FontSource = Settings.FontName == "Vazirmatn" ? VazirmatnSource() : $"{Settings.FontName}, Segoe UI";
        var ff = new FontFamily(FontSource);
        Current.Resources["AppFontFamily"] = ff;
        Current.Resources["ContentControlThemeFontFamily"] = ff;
    }

    static string VazirmatnSource()
    {
        const string fallback = "Vazirmatn, Segoe UI Variable, Segoe UI";

        // 1) installed on Windows (system or per-user): the most reliable source
        using (var installed = new InstalledFontCollection())
            if (installed.Families.Any(f => f.Name == "Vazirmatn")) return fallback;

        // 2) bundled file: referenced per FILE (a folder URI is not valid) with the family name read from the file;
        //    the installed/system names stay as fallbacks in the same list
        var file = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "Vazirmatn-Regular.ttf");
        if (File.Exists(file))
        {
            try
            {
                using var pfc = new PrivateFontCollection();
                pfc.AddFontFile(file);
                return $"ms-appx:///Assets/Fonts/Vazirmatn-Regular.ttf#{pfc.Families[0].Name}, {fallback}";
            }
            catch { }
        }
        return fallback;
    }

    public static void ShowShell(string tag)
    {
        Flyout.HideAnimated();
        if (_shell is null)
        {
            _shell = new ShellWindow();
            _shell.Closed += (_, _) => _shell = null;
        }
        _shell.ShowPage(tag);
    }

    public static void Restart()
    {
        // free the single-instance mutex so the new process isn't rejected
        try { _mutex?.ReleaseMutex(); _mutex?.Dispose(); } catch { }
        Process.Start(new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = true });
        Quit();
    }

    public static void Quit()
    {
        GoogleSync.Dispose();
        Tray.Dispose();
        _shell?.Close();
        Flyout.AllowClose = true;
        Flyout.Close();
        Current.Exit();
    }
}
