using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
        s.IconColor = TagOf(ColorBox, s.IconColor);

        s.Save();
        s.ApplyStartup();
        App.Tray.Refresh();
    }
}
