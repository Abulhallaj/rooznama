using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using WinRT.Interop;

namespace Rooznama.Services;

/// <summary>Tray icon via raw Shell_NotifyIcon, hooked onto the flyout window's HWND (no WinForms).</summary>
public sealed class TrayService : IDisposable
{
    const uint WM_TRAY = 0x8001;                       // WM_APP + 1
    const uint WM_MOUSEMOVE = 0x0200, WM_LBUTTONUP = 0x0202, WM_RBUTTONUP = 0x0205, WM_NULL = 0;
    const uint WM_TIMECHANGE = 0x001E, WM_POWERBROADCAST = 0x0218;
    const long PBT_APMRESUMESUSPEND = 0x7, PBT_APMRESUMEAUTOMATIC = 0x12;
    const uint NIM_ADD = 0, NIM_MODIFY = 1, NIM_DELETE = 2;
    const uint NIF_MESSAGE = 1, NIF_ICON = 2, NIF_TIP = 4;
    const uint MF_STRING = 0, MF_SEPARATOR = 0x800;
    const uint TPM_RIGHTBUTTON = 2, TPM_RIGHTALIGN = 8, TPM_BOTTOMALIGN = 0x20, TPM_RETURNCMD = 0x100, TPM_LAYOUTRTL = 0x8000;

    delegate IntPtr SubclassProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, UIntPtr id, UIntPtr data);

    [StructLayout(LayoutKind.Sequential)] struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string szInfo;
        public uint uVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [DllImport("shell32.dll", EntryPoint = "Shell_NotifyIconW", CharSet = CharSet.Unicode)] static extern bool Shell_NotifyIcon(uint msg, ref NOTIFYICONDATA data);
    [DllImport("comctl32.dll")] static extern bool SetWindowSubclass(IntPtr hWnd, SubclassProc proc, UIntPtr id, UIntPtr data);
    [DllImport("comctl32.dll")] static extern bool RemoveWindowSubclass(IntPtr hWnd, SubclassProc proc, UIntPtr id);
    [DllImport("comctl32.dll")] static extern IntPtr DefSubclassProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr h);
    [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT p);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] static extern IntPtr CreatePopupMenu();
    [DllImport("user32.dll", EntryPoint = "AppendMenuW", CharSet = CharSet.Unicode)] static extern bool AppendMenu(IntPtr menu, uint flags, UIntPtr id, string? text);
    [DllImport("user32.dll")] static extern uint TrackPopupMenuEx(IntPtr menu, uint flags, int x, int y, IntPtr hwnd, IntPtr tpm);
    [DllImport("user32.dll")] static extern bool DestroyMenu(IntPtr menu);
    [DllImport("user32.dll", EntryPoint = "PostMessageW")] static extern bool PostMessage(IntPtr hwnd, uint msg, IntPtr w, IntPtr l);
    [DllImport("user32.dll", EntryPoint = "RegisterWindowMessageW", CharSet = CharSet.Unicode)] static extern uint RegisterWindowMessage(string name);

    // Undocumented uxtheme ordinals (used by many apps) so the native tray menu follows dark mode.
    [DllImport("uxtheme.dll", EntryPoint = "#135")] static extern int SetPreferredAppMode(int mode);
    [DllImport("uxtheme.dll", EntryPoint = "#136")] static extern void FlushMenuThemes();

    readonly IntPtr _hwnd;
    readonly SubclassProc _proc;          // keep a reference so the delegate isn't collected
    readonly uint _taskbarCreated;
    readonly DispatcherQueueTimer _watchdog;   // every 30 s: safety net
    readonly DispatcherQueueTimer _midnight;   // one-shot, exactly at the next local midnight
    readonly DispatcherQueueTimer _soon;       // delayed re-check after resume / time change
    IntPtr _hIcon;
    bool _added;
    DateTime _lastDate = DateTime.MinValue;

    public TrayService()
    {
        try
        {
            // 1 = AllowDark (follow Windows) · 2 = ForceDark · 3 = ForceLight
            SetPreferredAppMode(App.Settings.Theme switch { "dark" => 2, "light" => 3, _ => 1 });
            FlushMenuThemes();
        }
        catch { }

        _hwnd = WindowNative.GetWindowHandle(App.Flyout);
        _taskbarCreated = RegisterWindowMessage("TaskbarCreated");   // explorer restarted → re-add icon
        _proc = WndProc;
        SetWindowSubclass(_hwnd, _proc, (UIntPtr)1, UIntPtr.Zero);

        var dq = DispatcherQueue.GetForCurrentThread();

        _watchdog = dq.CreateTimer();
        _watchdog.Interval = TimeSpan.FromSeconds(30);
        _watchdog.Tick += (_, _) => Safe(() =>
        {
            Refresh(false);
            if (!_midnight.IsRunning) ArmMidnight();
        });

        _midnight = dq.CreateTimer();
        _midnight.IsRepeating = false;
        _midnight.Tick += (_, _) => Safe(() =>
        {
            Refresh(false);
            ArmMidnight();
        });

        _soon = dq.CreateTimer();
        _soon.IsRepeating = false;
        _soon.Interval = TimeSpan.FromSeconds(3);
        _soon.Tick += (_, _) => Safe(() => Refresh());

        Refresh();
        ArmMidnight();
        _watchdog.Start();
        Log.Write("tray started");
    }

    static void Safe(Action a)
    {
        try { a(); }
        catch (Exception ex) { Log.Write("ERROR " + ex); }
    }

    void ArmMidnight()
    {
        var now = DateTime.Now;
        var wait = now.Date.AddDays(1) - now + TimeSpan.FromSeconds(1);
        _midnight.Stop();
        _midnight.Interval = wait;
        _midnight.Start();
        Log.Write($"midnight timer armed, fires in {wait:hh\\:mm\\:ss}");
    }

    // sleep/resume or clock change: timers may have been lost or shifted
    void Resync(string why)
    {
        Log.Write(why);
        Safe(() => { Refresh(); ArmMidnight(); });
        _soon.Stop();
        _soon.Start();                      // the shell may not be ready right after resume
    }

    public static (int X, int Y) CursorPos()
    {
        GetCursorPos(out var p);
        return (p.X, p.Y);
    }

    IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, UIntPtr id, UIntPtr data)
    {
        if (msg == WM_TRAY)
        {
            switch ((uint)((long)lParam & 0xFFFF))
            {
                case WM_MOUSEMOVE: Safe(() => Refresh(false)); break;   // hovering = about to read the tooltip
                case WM_LBUTTONUP: Toggle(); break;
                case WM_RBUTTONUP: ShowMenu(); break;
            }
            return IntPtr.Zero;
        }
        if (msg == _taskbarCreated)
        {
            _added = false;
            Resync("taskbar re-created");
            return IntPtr.Zero;
        }
        if (msg == WM_POWERBROADCAST && ((long)wParam == PBT_APMRESUMEAUTOMATIC || (long)wParam == PBT_APMRESUMESUSPEND))
            Resync("resumed from sleep");
        else if (msg == WM_TIMECHANGE)
            Resync("system time changed");

        return DefSubclassProc(hWnd, msg, wParam, lParam);
    }

    void Toggle()
    {
        var w = App.Flyout;
        if (w.JustDeactivated) return;      // the click itself already hid it
        if (w.IsShown) w.HideAnimated(); else w.ShowAnimated();
    }

    void ShowMenu()
    {
        var (x, y) = CursorPos();
        IntPtr menu = CreatePopupMenu();
        AppendMenu(menu, MF_STRING, (UIntPtr)1, Loc.T("calendar"));
        AppendMenu(menu, MF_STRING, (UIntPtr)2, Loc.T("converter"));
        AppendMenu(menu, MF_STRING, (UIntPtr)3, Loc.T("settings"));
        AppendMenu(menu, MF_STRING, (UIntPtr)4, Loc.T("about"));
        AppendMenu(menu, MF_SEPARATOR, UIntPtr.Zero, null);
        AppendMenu(menu, MF_STRING, (UIntPtr)9, Loc.T("exit"));

        uint flags = TPM_RETURNCMD | TPM_RIGHTBUTTON | TPM_BOTTOMALIGN | TPM_RIGHTALIGN;
        if (Loc.IsFa) flags |= TPM_LAYOUTRTL;          // right-to-left menu

        SetForegroundWindow(_hwnd);         // required, otherwise the menu doesn't close on outside click
        uint cmd = TrackPopupMenuEx(menu, flags, x, y, _hwnd, IntPtr.Zero);
        PostMessage(_hwnd, WM_NULL, IntPtr.Zero, IntPtr.Zero);
        DestroyMenu(menu);

        switch (cmd)
        {
            case 1: App.ShowShell("calendar"); break;
            case 2: App.ShowShell("converter"); break;
            case 3: App.ShowShell("settings"); break;
            case 4: App.ShowShell("about"); break;
            case 9: App.Quit(); break;
        }
    }

    /// <param name="force">false = only when the day changed</param>
    public void Refresh(bool force = true)
    {
        var today = DateTime.Today;
        if (!force && today == _lastDate) return;
        if (today != _lastDate) Log.Write($"day changed → {today:yyyy-MM-dd} (Shamsi day {PersianDate.Day(today)})");
        _lastDate = today;

        var text = PersianDate.Full(today);
        if (App.Settings.ShowGregorianInTooltip)
            text += "\n" + today.ToString("dddd, dd MMMM yyyy", CultureInfo.InvariantCulture);
        if (text.Length > 127) text = text[..127];

        var oldIcon = _hIcon;
        _hIcon = MakeIcon(PersianDate.Day(today));

        var nid = new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _hwnd,
            uID = 1,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = WM_TRAY,
            hIcon = _hIcon,
            szTip = text
        };
        // if the shell lost our icon (modify fails), add it again
        if (!_added || !Shell_NotifyIcon(NIM_MODIFY, ref nid))
            _added = Shell_NotifyIcon(NIM_ADD, ref nid);

        if (oldIcon != IntPtr.Zero) DestroyIcon(oldIcon);
    }

    const float IconPx = 32f, IconFit = 30f;      // 1px margin
    static FontFamily? _iconFont;
    static FontStyle _iconStyle = FontStyle.Bold;
    static PrivateFontCollection? _pfc;               // keeps a bundled font alive
    static float _em;

    static FontFamily IconFont()
    {
        if (_iconFont is not null) return _iconFont;

        var dir = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts");
        var wanted = App.Settings.FontName;
        foreach (var name in wanted == "Vazirmatn" ? new[] { "Vazirmatn-Bold.ttf", "Vazirmatn-Regular.ttf" } : Array.Empty<string>())
        {
            var file = Path.Combine(dir, name);
            if (!File.Exists(file)) continue;
            try
            {
                _pfc = new PrivateFontCollection();
                _pfc.AddFontFile(file);
                _iconFont = _pfc.Families[0];
                break;
            }
            catch { _pfc = null; }
        }

        if (_iconFont is null)
        {
            using var installed = new InstalledFontCollection();
            _iconFont = installed.Families.FirstOrDefault(f => f.Name == wanted) ?? new FontFamily("Tahoma");
        }
        if (!_iconFont.IsStyleAvailable(FontStyle.Bold)) _iconStyle = FontStyle.Regular;
        return _iconFont;
    }

    // One em size for every day: the largest at which the widest/tallest day (1..31) still fits.
    static float IconEm(FontFamily fam)
    {
        if (_em > 0) return _em;
        float maxW = 1, maxH = 1;
        for (int d = 1; d <= 31; d++)
        {
            using var p = new GraphicsPath();
            p.AddString(PersianDate.Num(d), fam, (int)_iconStyle, 100f, PointF.Empty, StringFormat.GenericTypographic);
            var b = p.GetBounds();
            maxW = Math.Max(maxW, b.Width);
            maxH = Math.Max(maxH, b.Height);
        }
        return _em = 100f * Math.Min(IconFit / maxW, IconFit / maxH);
    }

    static IntPtr MakeIcon(int day)
    {
        var fam = IconFont();
        float em = IconEm(fam);

        Color bgColor;
        try { bgColor = ColorTranslator.FromHtml(App.Settings.IconColor); }
        catch { bgColor = Color.FromArgb(0, 120, 215); }

        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var bg = new SolidBrush(bgColor);
            g.FillRectangle(bg, 0, 0, IconPx, IconPx);

            using var path = new GraphicsPath();
            path.AddString(PersianDate.Num(day), fam, (int)_iconStyle, em, PointF.Empty, StringFormat.GenericTypographic);
            var b = path.GetBounds();
            g.TranslateTransform((IconPx - b.Width) / 2f - b.X, (IconPx - b.Height) / 2f - b.Y);
            g.FillPath(Brushes.White, path);
        }
        return bmp.GetHicon();              // caller destroys with DestroyIcon
    }

    public void Dispose()
    {
        _watchdog.Stop();
        _midnight.Stop();
        _soon.Stop();
        var nid = new NOTIFYICONDATA { cbSize = Marshal.SizeOf<NOTIFYICONDATA>(), hWnd = _hwnd, uID = 1, szTip = "", szInfo = "", szInfoTitle = "" };
        Shell_NotifyIcon(NIM_DELETE, ref nid);
        RemoveWindowSubclass(_hwnd, _proc, (UIntPtr)1);
        if (_hIcon != IntPtr.Zero) { DestroyIcon(_hIcon); _hIcon = IntPtr.Zero; }
    }
}
