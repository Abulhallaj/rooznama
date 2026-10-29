using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Rooznama.Services;
using Windows.Foundation;
using Windows.Graphics;
using WinRT.Interop;

namespace Rooznama;

/// <summary>Small borderless, never-activated tooltip shown above the tray icon (right-to-left, themed).</summary>
public sealed partial class TipWindow : Window
{
    [DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr hwnd);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    readonly IntPtr _hwnd;

    public TipWindow()
    {
        InitializeComponent();
        Root.FlowDirection = Loc.Flow;
        _hwnd = WindowNative.GetWindowHandle(this);

        AppWindow.IsShownInSwitchers = false;
        if (AppWindow.Presenter is OverlappedPresenter p)
        {
            p.IsMaximizable = false;
            p.IsMinimizable = false;
            p.IsResizable = false;
            p.IsAlwaysOnTop = true;
            p.SetBorderAndTitleBar(false, false);
        }

        long style = GetWindowLongPtr(_hwnd, -16).ToInt64();
        style &= ~(0x00C00000L | 0x00040000L | 0x00080000L | 0x00020000L | 0x00010000L);
        SetWindowLongPtr(_hwnd, -16, new IntPtr(style));
        // WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW: never takes focus, no taskbar button
        long ex = GetWindowLongPtr(_hwnd, -20).ToInt64() | 0x08000000L | 0x00000080L;
        SetWindowLongPtr(_hwnd, -20, new IntPtr(ex));
        SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0004 | 0x0010 | 0x0020);

        int round = 2;
        DwmSetWindowAttribute(_hwnd, 33, ref round, sizeof(int));          // rounded corners
    }

    /// <summary>Shows the tooltip centered above the icon rectangle (physical pixels).</summary>
    public void ShowAbove(int left, int top, int right, int bottom)
    {
        var today = DateTime.Today;
        Line1.Text = PersianDate.Full(today);
        var second = App.Settings.ShowGregorianInTooltip
            ? today.ToString("dddd, dd MMMM yyyy", CultureInfo.InvariantCulture)
            : "";
        Line2.Text = second;
        Line2.Visibility = second.Length == 0 ? Visibility.Collapsed : Visibility.Visible;

        int border = ThemeHelper.IsDark() ? 0x00404040 : 0x00C8C8C8;
        DwmSetWindowAttribute(_hwnd, 34, ref border, sizeof(int));          // single thin border, theme-aware

        double s = GetDpiForWindow(_hwnd) / 96.0;
        Root.Measure(new Size(420, double.PositiveInfinity));
        int w = (int)Math.Ceiling(Root.DesiredSize.Width * s);
        int h = (int)Math.Ceiling(Root.DesiredSize.Height * s);

        int cx = (left + right) / 2;
        var wa = DisplayArea.GetFromPoint(new PointInt32(cx, top), DisplayAreaFallback.Nearest).WorkArea;
        int x = Math.Clamp(cx - w / 2, wa.X + 4, wa.X + wa.Width - w - 4);
        int y = top - h - (int)(8 * s);

        AppWindow.MoveAndResize(new RectInt32(x, y, w, h));
        AppWindow.Show(false);              // don't activate
    }

    public void HideTip() => AppWindow.Hide();
}
