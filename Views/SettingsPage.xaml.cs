using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rooznama.Services;

namespace Rooznama.Views;

public sealed partial class SettingsPage : Page
{
    bool _loading = true;

    public SettingsPage()
    {
        InitializeComponent();

        var s = App.Settings;
        GregorianSwitch.IsOn = s.ShowGregorianInTooltip;
        AutoHideSwitch.IsOn = s.AutoHide;
        StartupSwitch.IsOn = s.StartWithWindows;
        CustomTipSwitch.IsOn = s.CustomTooltip;
        UpdatesSwitch.IsOn = s.CheckUpdates;
        Select(ColorBox, s.IconColor);
        Select(LangBox, s.Language);
        Select(FontBox, s.FontName);
        Select(ThemeBox, s.Theme);

        _loading = false;
    }

    static void Select(ComboBox box, string tag) =>
        box.SelectedItem = box.Items.OfType<ComboBoxItem>().FirstOrDefault(i => (string)i.Tag == tag) ?? box.Items[0];

    static string TagOf(ComboBox box, string fallback) =>
        box.SelectedItem is ComboBoxItem { Tag: string t } ? t : fallback;

    void Option_Toggled(object sender, RoutedEventArgs e) => Apply();
    void Color_Changed(object sender, SelectionChangedEventArgs e) => Apply();

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

        s.Save();
        s.ApplyStartup();
        App.Tray.Refresh();
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
