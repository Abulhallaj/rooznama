using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Rooznama.Services;
using Rooznama.Views;
using Windows.Graphics;
using WinRT.Interop;

namespace Rooznama;

/// <summary>Regular window (taskbar button, real close): calendar, converter, settings, about.</summary>
public sealed partial class ShellWindow : Window
{
    [DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr hwnd);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hwnd);

    public ShellWindow()
    {
        InitializeComponent();
        Root.FlowDirection = Loc.Flow;
        Title = Loc.T("app.name");
        SystemBackdrop = new MicaBackdrop();

        var ico = Path.Combine(AppContext.BaseDirectory, "Assets", "Rooznama.ico");
        if (File.Exists(ico)) AppWindow.SetIcon(ico);

        NavCalendar.Content = Loc.T("calendar");
        NavConverter.Content = Loc.T("converter");
        NavSettings.Content = Loc.T("settings");
        NavAbout.Content = Loc.T("about");

        Nav.SelectedItem = NavCalendar;
        SizeAndCenter();
    }

    void SizeAndCenter()
    {
        double s = GetDpiForWindow(WindowNative.GetWindowHandle(this)) / 96.0;
        var wa = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
        int w = (int)(680 * s);
        int h = Math.Min((int)(720 * s), wa.Height);
        AppWindow.MoveAndResize(new RectInt32(wa.X + (wa.Width - w) / 2, wa.Y + (wa.Height - h) / 2, w, h));
    }

    public void ShowPage(string tag)
    {
        Nav.SelectedItem = Nav.MenuItems.Concat(Nav.FooterMenuItems)
                              .OfType<NavigationViewItem>().First(i => (string)i.Tag == tag);
        Activate();
        SetForegroundWindow(WindowNative.GetWindowHandle(this));
    }

    void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var tag = args.SelectedItemContainer?.Tag as string;
        Type page = tag switch
        {
            "converter" => typeof(ConverterPage),
            "settings" => typeof(SettingsPage),
            "about" => typeof(AboutPage),
            _ => typeof(CalendarPage)
        };
        ContentFrame.Navigate(page);
    }
}
