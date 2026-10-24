using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Rooznama.Services;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI.ViewManagement;
using WinRT.Interop;

namespace Rooznama;

/// <summary>Borderless rounded acrylic calendar flyout near the tray, with slide + fade animation.</summary>
public sealed partial class FlyoutWindow : Window
{
    const int WidthDip = 360, MarginDip = 12, SlideDip = 28;

    [DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr hwnd);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    readonly IntPtr _hwnd;
    DispatcherQueueTimer? _anim;
    RectInt32 _target;
    bool _hiding;
    long _lastHide;

    public bool AllowClose;

    public FlyoutWindow()
    {
        InitializeComponent();
        Root.FlowDirection = Loc.Flow;
        _hwnd = WindowNative.GetWindowHandle(this);

        SystemBackdrop = new DesktopAcrylicBackdrop();
        AppWindow.IsShownInSwitchers = false;   // no taskbar button
        if (AppWindow.Presenter is OverlappedPresenter p)
        {
            p.IsMaximizable = false;
            p.IsMinimizable = false;
            p.IsResizable = false;
            p.IsAlwaysOnTop = true;
            p.SetBorderAndTitleBar(false, false);
        }
        StripFrame();

        // Windows 11: rounded corners (the thin border is set per theme in ApplyChrome). No-op on Windows 10.
        int round = 2;                                                    // DWMWCP_ROUND
        DwmSetWindowAttribute(_hwnd, 33, ref round, sizeof(int));         // DWMWA_WINDOW_CORNER_PREFERENCE

        ToolTipService.SetToolTip(CollapseBtn, new ToolTip { Content = Loc.T("tip.collapse"), FlowDirection = Loc.Flow });

        AppWindow.Closing += (_, e) =>
        {
            if (AllowClose) return;
            e.Cancel = true;
            HideAnimated();
        };

        Activated += (_, e) =>
        {
            if (e.WindowActivationState == WindowActivationState.Deactivated && App.Settings.AutoHide)
                HideAnimated();
        };

        ApplyCollapsed();
    }

    // Make sure no caption / resize frame / system buttons remain: any leftover non-client frame is the "extra border".
    void StripFrame()
    {
        long style = GetWindowLongPtr(_hwnd, -16).ToInt64();                  // GWL_STYLE
        style &= ~(0x00C00000L | 0x00040000L | 0x00080000L | 0x00020000L | 0x00010000L);   // CAPTION|THICKFRAME|SYSMENU|MIN|MAX
        SetWindowLongPtr(_hwnd, -16, new IntPtr(style));
        SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0004 | 0x0010 | 0x0020); // NOSIZE|NOMOVE|NOZORDER|NOACTIVATE|FRAMECHANGED
    }

    static bool IsDark() => App.Settings.Theme switch
    {
        "dark" => true,
        "light" => false,
        _ => new UISettings().GetColorValue(UIColorType.Background).R < 128
    };

    // single subtle 1px DWM border, tuned to the current theme (like the system flyouts)
    void ApplyChrome()
    {
        int border = IsDark() ? 0x00404040 : 0x00C8C8C8;                  // COLORREF 0x00BBGGRR
        DwmSetWindowAttribute(_hwnd, 34, ref border, sizeof(int));        // DWMWA_BORDER_COLOR
    }

    public bool IsShown => AppWindow.IsVisible && !_hiding;
    // true when the window was just auto-hidden by the same click that is now reaching the tray icon
    public bool JustDeactivated => Environment.TickCount64 - _lastHide < 300;

    double Scale => GetDpiForWindow(_hwnd) / 96.0;

    // window height = exactly what the content needs (no empty space at the bottom)
    int MeasureHeightPx()
    {
        Root.Measure(new Size(WidthDip, double.PositiveInfinity));
        return (int)Math.Ceiling(Root.DesiredSize.Height * Scale);
    }

    RectInt32 ComputeTarget()
    {
        var (cx, cy) = TrayService.CursorPos();
        var wa = DisplayArea.GetFromPoint(new PointInt32(cx, cy), DisplayAreaFallback.Nearest).WorkArea;
        double s = Scale;
        int margin = (int)(MarginDip * s);
        int w = (int)(WidthDip * s);
        int h = Math.Min(MeasureHeightPx(), wa.Height - 2 * margin);
        return new RectInt32(wa.X + wa.Width - w - margin, wa.Y + wa.Height - h - margin, w, h);
    }

    public void ShowAnimated()
    {
        _hiding = false;
        var today = DateTime.Today;
        HeaderFa.Text = PersianDate.Full(today);
        HeaderEn.Text = today.ToString("dddd, dd MMMM yyyy", CultureInfo.InvariantCulture);
        Cal.ShowToday();
        ApplyCollapsed();
        ApplyChrome();

        _target = ComputeTarget();
        int dy = (int)(SlideDip * Scale);
        AppWindow.MoveAndResize(new RectInt32(_target.X, _target.Y + dy, _target.Width, _target.Height));
        Root.Opacity = 0;
        AppWindow.Show();
        Activate();
        SetForegroundWindow(_hwnd);
        Animate(true, null);
    }

    public void HideAnimated()
    {
        if (!AppWindow.IsVisible || _hiding) return;
        _hiding = true;
        _lastHide = Environment.TickCount64;
        Animate(false, () =>
        {
            AppWindow.Hide();
            _hiding = false;
        });
    }

    // window slides, content fades (acrylic itself is not alpha-blended)
    void Animate(bool show, Action? done)
    {
        _anim?.Stop();
        long start = Environment.TickCount64;
        int dur = show ? 220 : 140;
        int dy = (int)(SlideDip * Scale);

        var timer = DispatcherQueue.CreateTimer();
        _anim = timer;
        timer.Interval = TimeSpan.FromMilliseconds(15);
        timer.IsRepeating = true;
        timer.Tick += (t, _) =>
        {
            double p = Math.Min(1.0, (Environment.TickCount64 - start) / (double)dur);
            double k = show ? 1 - Math.Pow(1 - p, 3)   // ease-out cubic
                            : 1 - p * p;               // ease-in
            AppWindow.Move(new PointInt32(_target.X, _target.Y + (int)(dy * (1 - k))));
            Root.Opacity = k;
            if (p >= 1.0) { t.Stop(); done?.Invoke(); }
        };
        timer.Start();
    }

    void ApplyCollapsed()
    {
        bool c = App.Settings.CalendarCollapsed;
        Cal.Visibility = c ? Visibility.Collapsed : Visibility.Visible;
        CollapseBtn.Content = c ? "\uE70E" : "\uE70D";   // collapsed: arrow up (expand) · expanded: arrow down (collapse)
    }

    void Collapse_Click(object sender, RoutedEventArgs e)
    {
        App.Settings.CalendarCollapsed = !App.Settings.CalendarCollapsed;
        App.Settings.Save();
        ApplyCollapsed();

        // keep the bottom edge anchored
        var wa = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
        int bottom = AppWindow.Position.Y + AppWindow.Size.Height;
        int h = Math.Min(MeasureHeightPx(), wa.Height - 2 * (int)(MarginDip * Scale));
        _target = new RectInt32(AppWindow.Position.X, bottom - h, AppWindow.Size.Width, h);
        AppWindow.MoveAndResize(_target);
    }

    void Esc_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        HideAnimated();
    }
}
