using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rooznama.Services;

namespace Rooznama.Views;

public sealed partial class SettingsPage : Page
{
    bool _loading = true;
    bool _connecting;
    string? _connectError;

    public SettingsPage()
    {
        InitializeComponent();

        var s = App.Settings;
        GregorianSwitch.IsOn = s.ShowGregorianInTooltip;
        AutoHideSwitch.IsOn = s.AutoHide;
        StartupSwitch.IsOn = s.StartWithWindows;
        CustomTipSwitch.IsOn = s.CustomTooltip;
        UpdatesSwitch.IsOn = s.CheckUpdates;
        HijriTipSwitch.IsOn = s.ShowHijriInTooltip;
        HijriCellsSwitch.IsOn = s.ShowHijriInCells;
        WeeksSwitch.IsOn = s.ShowWeekNumbers;
        Select(HijriSourceBox, s.HijriSource);
        Select(ColorBox, s.IconColor);
        Select(LangBox, s.Language);
        Select(FontBox, s.FontName);
        Select(ThemeBox, s.Theme);
        GoogleAutoSwitch.IsOn = s.GoogleAutoSync;
        GoogleClientIdBox.Text = s.GoogleClientId;
        GoogleClientSecretBox.Password = s.GoogleClientSecret;

        _loading = false;

        UpdateGoogleUi();
        App.GoogleSync.StatusChanged += OnGoogleStatus;
        Unloaded += (_, _) => App.GoogleSync.StatusChanged -= OnGoogleStatus;
    }

    static void Select(ComboBox box, string tag) =>
        box.SelectedItem = box.Items.OfType<ComboBoxItem>().FirstOrDefault(i => (string)i.Tag == tag) ?? box.Items[0];

    static string TagOf(ComboBox box, string fallback) =>
        box.SelectedItem is ComboBoxItem { Tag: string t } ? t : fallback;

    void Option_Toggled(object sender, RoutedEventArgs e) => Apply();
    void Color_Changed(object sender, SelectionChangedEventArgs e) => Apply();
    void Option_Changed(object sender, SelectionChangedEventArgs e) => Apply();

    // language, font and theme need a restart (resources are resolved when windows are created)
    void Appearance_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        var s = App.Settings;
        s.Language = TagOf(LangBox, s.Language);
        s.FontName = TagOf(FontBox, s.FontName);
        s.Theme = TagOf(ThemeBox, s.Theme);
        s.Save();
        RestartBar.IsOpen = true;
    }

    void Restart_Click(object sender, RoutedEventArgs e) => App.Restart();

    void Apply()
    {
        if (_loading) return;

        var s = App.Settings;
        s.ShowGregorianInTooltip = GregorianSwitch.IsOn;
        s.AutoHide = AutoHideSwitch.IsOn;
        s.StartWithWindows = StartupSwitch.IsOn;
        s.CustomTooltip = CustomTipSwitch.IsOn;
        s.CheckUpdates = UpdatesSwitch.IsOn;
        s.IconColor = TagOf(ColorBox, s.IconColor);
        s.HijriSource = TagOf(HijriSourceBox, s.HijriSource);
        s.ShowHijriInTooltip = HijriTipSwitch.IsOn;
        s.ShowHijriInCells = HijriCellsSwitch.IsOn;
        s.ShowWeekNumbers = WeeksSwitch.IsOn;
        s.GoogleAutoSync = GoogleAutoSwitch.IsOn;
        s.GoogleClientId = GoogleClientIdBox.Text.Trim();
        s.GoogleClientSecret = GoogleClientSecretBox.Password.Trim();

        s.Save();
        s.ApplyStartup();
        App.Tray.Refresh();
    }

    // ---------- Google Calendar ----------
    void OnGoogleStatus() => DispatcherQueue.TryEnqueue(UpdateGoogleUi);

    void GoogleClient_LostFocus(object sender, RoutedEventArgs e) => Apply();

    void GoogleAuto_Toggled(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        Apply();
        App.GoogleSync.Start();   // starts or stops the timers
    }

    async void GoogleConnect_Click(object sender, RoutedEventArgs e)
    {
        if (App.Google.IsConnected)
        {
            await App.Google.DisconnectAsync();   // the events stay here; reconnecting the same account carries on where it stopped
            App.GoogleSync.Reset();
            UpdateGoogleUi();
            return;
        }

        Apply();   // keeps the client id that was just typed
        _connectError = null;
        _connecting = true;
        UpdateGoogleUi();
        try
        {
            var email = await App.Google.ConnectAsync(CancellationToken.None);

            // another account than before: nothing of the old one may stay linked
            var s = App.Settings;
            if (s.GoogleAccount.Length > 0 && email is not null && !string.Equals(s.GoogleAccount, email, StringComparison.OrdinalIgnoreCase)
                && App.PersonalEvents.ResetRemoteLinks())
                App.PersonalEvents.NotifyChanged();
            if (email is not null) s.GoogleAccount = email;
            s.Save();

            App.GoogleSync.Reset();
            _ = App.GoogleSync.SyncNowAsync();
        }
        catch (Exception ex)
        {
            _connectError = ex.Message;
            Log.Write("google connect failed: " + ex.Message);
        }
        finally
        {
            _connecting = false;
            UpdateGoogleUi();
        }
    }

    async void GoogleSync_Click(object sender, RoutedEventArgs e) => await App.GoogleSync.SyncNowAsync();

    void UpdateGoogleUi()
    {
        var g = App.Google;
        GoogleConnectBtn.Content = Loc.T(g.IsConnected ? "google.disconnect" : "google.connect");
        GoogleConnectBtn.IsEnabled = !_connecting;
        GoogleSyncBtn.IsEnabled = g.IsConnected && App.GoogleSync.State != GoogleSyncState.Syncing;
        GoogleClientIdBox.IsEnabled = GoogleClientSecretBox.IsEnabled = !g.IsConnected;   // the client can only change while disconnected
        GoogleStatusText.Text = GoogleStatus();
    }

    string GoogleStatus()
    {
        var g = App.Google;
        if (_connecting) return Loc.T("google.waiting");
        if (!g.IsConnected)
        {
            if (_connectError is not null) return string.Format(Loc.T("google.status.failed"), _connectError);
            return Loc.T(g.HasClient ? "google.status.off" : "google.status.noclient");
        }

        var who = g.Email ?? "";
        var sync = App.GoogleSync;
        return sync.State switch
        {
            GoogleSyncState.Syncing => string.Format(Loc.T("google.status.syncing"), who),
            GoogleSyncState.Error => string.Format(Loc.T("google.status.error"), who, sync.LastError),
            GoogleSyncState.NeedsReconnect => Loc.T("google.status.reconnect"),
            _ => sync.LastSyncUtc is { } t
                ? string.Format(Loc.T("google.status.last"), who, PersianDate.Digits(t.ToLocalTime().ToString("yyyy-MM-dd HH:mm")))
                : string.Format(Loc.T("google.status.connected"), who)
        };
    }

    async void Check_Click(object sender, RoutedEventArgs e)
    {
        UpdateLink.Visibility = Visibility.Collapsed;
        if (AppInfo.IsDev)
        {
            CheckStatus.Text = Loc.T("upd.dev");
            return;
        }

        CheckBtn.IsEnabled = false;
        CheckStatus.Text = Loc.T("upd.checking");
        var r = await UpdateService.CheckAsync();
        CheckBtn.IsEnabled = true;

        if (!r.Ok)
        {
            CheckStatus.Text = Loc.T("upd.failed");
        }
        else if (r.Update is { } u)
        {
            CheckStatus.Text = string.Format(Loc.T("upd.available"), u.Tag);
            UpdateLink.Content = Loc.T("upd.download");
            UpdateLink.NavigateUri = new Uri(u.Url);
            UpdateLink.Visibility = Visibility.Visible;
        }
        else
        {
            CheckStatus.Text = Loc.T("upd.uptodate");
        }
    }
}
