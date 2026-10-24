using System.Globalization;
using System.Numerics;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Rooznama.Services;

namespace Rooznama.Views;

public sealed partial class CalendarPanel : UserControl
{
    enum Mode { Days, Months, Years }

    const int MaxYear = 9377;
    static readonly string[] FaHeads = { "ش", "ی", "د", "س", "چ", "پ", "ج" };            // شنبه → جمعه
    static readonly string[] EnHeads = { "Sa", "Su", "Mo", "Tu", "We", "Th", "Fr" };

    Mode _mode = Mode.Days;
    int _y, _m;
    DateTime _selected = DateTime.Today;
    bool _large;

    int CellH => _large ? 64 : 40;
    int HeadH => _large ? 32 : 28;

    /// <summary>Large cells with the Gregorian day beside each Shamsi day, plus a Today button (shell calendar page).</summary>
    public bool IsLarge
    {
        get => _large;
        set { _large = value; ApplyMode(); Render(); }
    }

    public CalendarPanel()
    {
        InitializeComponent();
        ElementCompositionPreview.SetIsTranslationEnabled(ViewGrid, true);
        ApplyMode();
        ShowToday();
    }

    void ApplyMode()
    {
        ViewGrid.Height = HeadH + 6 * CellH;
        TodayBtn.Visibility = _large ? Visibility.Visible : Visibility.Collapsed;
    }

    public void ShowToday(bool animate = false)
    {
        _selected = DateTime.Today;
        _y = PersianDate.Year(_selected);
        _m = PersianDate.Month(_selected);
        _mode = Mode.Days;
        Render(0, animate);
    }

    // ---------- rendering ----------
    /// <param name="dir">-1: new content enters from above · +1: from below · 0: fade only</param>
    void Render(int dir = 0, bool animate = false)
    {
        ViewGrid.Children.Clear();
        ViewGrid.RowDefinitions.Clear();
        ViewGrid.ColumnDefinitions.Clear();

        switch (_mode)
        {
            case Mode.Days: RenderDays(); break;
            case Mode.Months: RenderMonths(); break;
            default: RenderYears(); break;
        }
        TitleBtn.IsEnabled = _mode != Mode.Years;

        if (animate) AnimateIn(dir);
        _ = LoadEventsAsync(_selected.Date);
    }

    void AnimateIn(int dir)
    {
        var v = ElementCompositionPreview.GetElementVisual(ViewGrid);
        var c = v.Compositor;
        var ease = c.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1f));
        var span = TimeSpan.FromMilliseconds(220);

        var slide = c.CreateVector3KeyFrameAnimation();
        slide.InsertKeyFrame(0f, new Vector3(0, 26 * dir, 0));
        slide.InsertKeyFrame(1f, Vector3.Zero, ease);
        slide.Duration = span;

        var fade = c.CreateScalarKeyFrameAnimation();
        fade.InsertKeyFrame(0f, 0f);
        fade.InsertKeyFrame(1f, 1f, ease);
        fade.Duration = span;

        v.StartAnimation("Translation", slide);
        v.StartAnimation("Opacity", fade);
    }

    void RenderDays()
    {
        TitleText.Text = $"{PersianDate.MonthName(_m)} {PersianDate.Num(_y)}";

        for (int c = 0; c < 7; c++) ViewGrid.ColumnDefinitions.Add(new ColumnDefinition());
        ViewGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(HeadH) });
        for (int r = 0; r < 6; r++) ViewGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(CellH) });

        var heads = Loc.IsFa ? FaHeads : EnHeads;
        for (int c = 0; c < 7; c++)
        {
            var t = new TextBlock
            {
                Text = heads[c],
                FontSize = _large ? 13 : 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = 0.7
            };
            if (c == 6) t.Foreground = new SolidColorBrush(Colors.IndianRed);
            Grid.SetColumn(t, c);
            ViewGrid.Children.Add(t);
        }

        // always 6 full weeks: the tail of the previous month and the head of the next are shown dimmed
        var first = PersianDate.ToGregorian(_y, _m, 1);
        int offset = ((int)first.DayOfWeek + 1) % 7;   // شنبه = 0
        var start = first.AddDays(-offset);
        var today = DateTime.Today;

        for (int i = 0; i < 42; i++)
        {
            var date = start.AddDays(i);
            if (!PersianDate.InRange(date)) continue;

            bool inMonth = PersianDate.Month(date) == _m && PersianDate.Year(date) == _y;
            var b = NewDayCell(date);
            b.Tag = date;
            ToolTipService.SetToolTip(b, new ToolTip { Content = DayInfo.Text(date), FlowDirection = Loc.Flow });
            Mark(b, accent: date == today, selected: date == _selected.Date, red: i % 7 == 6, dim: !inMonth);
            Grid.SetRow(b, 1 + i / 7);
            Grid.SetColumn(b, i % 7);
            ViewGrid.Children.Add(b);
        }
    }

    void RenderMonths()
    {
        TitleText.Text = PersianDate.Num(_y);
        for (int c = 0; c < 4; c++) ViewGrid.ColumnDefinitions.Add(new ColumnDefinition());
        for (int r = 0; r < 3; r++) ViewGrid.RowDefinitions.Add(new RowDefinition());

        var today = DateTime.Today;
        for (int m = 1; m <= 12; m++)
        {
            var b = NewCell(PersianDate.MonthName(m), circle: false);
            b.Tag = m;
            Mark(b, accent: _y == PersianDate.Year(today) && m == PersianDate.Month(today));
            Grid.SetRow(b, (m - 1) / 4);
            Grid.SetColumn(b, (m - 1) % 4);
            ViewGrid.Children.Add(b);
        }
    }

    void RenderYears()
    {
        int start = (_y / 10) * 10 - 1;   // 12 tiles: previous year, the decade, next year
        TitleText.Text = $"{PersianDate.Num(start + 1)} – {PersianDate.Num(start + 10)}";
        for (int c = 0; c < 4; c++) ViewGrid.ColumnDefinitions.Add(new ColumnDefinition());
        for (int r = 0; r < 3; r++) ViewGrid.RowDefinitions.Add(new RowDefinition());

        int thisYear = PersianDate.Year(DateTime.Today);
        for (int i = 0; i < 12; i++)
        {
            int y = start + i;
            var b = NewCell(PersianDate.Num(y), circle: false);
            b.Tag = y;
            b.IsEnabled = y >= 1 && y <= MaxYear;
            Mark(b, accent: y == thisYear, dim: y < start + 1 || y > start + 10);
            Grid.SetRow(b, i / 4);
            Grid.SetColumn(b, i % 4);
            ViewGrid.Children.Add(b);
        }
    }

    // compact: round cell with the Shamsi day · large: rounded tile with the Gregorian day under the Shamsi one
    Button NewDayCell(DateTime date)
    {
        int day = PersianDate.Day(date);
        if (!_large) return NewCell(PersianDate.Num(day), circle: true);

        var b = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(2),
            Padding = new Thickness(0),
            CornerRadius = new CornerRadius(8)
        };
        var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        sp.Children.Add(new TextBlock
        {
            Text = PersianDate.Num(day),
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        sp.Children.Add(new TextBlock
        {
            Text = date.Day == 1 ? date.ToString("d MMM", CultureInfo.InvariantCulture)
                                 : date.Day.ToString(CultureInfo.InvariantCulture),
            FontSize = 11,
            Opacity = 0.65,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        b.Content = sp;
        b.Click += Cell_Click;
        return b;
    }

    Button NewCell(string text, bool circle)
    {
        var b = new Button { Content = text, Padding = new Thickness(0) };
        if (circle)
        {
            b.Width = b.Height = 38;
            b.HorizontalAlignment = HorizontalAlignment.Center;
            b.VerticalAlignment = VerticalAlignment.Center;
            b.CornerRadius = new CornerRadius(19);
        }
        else
        {
            b.HorizontalAlignment = HorizontalAlignment.Stretch;
            b.VerticalAlignment = VerticalAlignment.Stretch;
            b.Margin = new Thickness(4);
            b.CornerRadius = new CornerRadius(8);
            if (_large) b.FontSize = 16;
        }
        b.Click += Cell_Click;
        return b;
    }

    static void Mark(Button b, bool accent = false, bool selected = false, bool red = false, bool dim = false)
    {
        if (accent)
        {
            b.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
        }
        else
        {
            b.Background = new SolidColorBrush(Colors.Transparent);
            b.BorderThickness = new Thickness(0);
            if (red) b.Foreground = new SolidColorBrush(Colors.IndianRed);
        }
        if (selected)
        {
            b.BorderBrush = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
            b.BorderThickness = new Thickness(2);
        }
        if (dim && !accent) b.Opacity = 0.45;
    }

    // ---------- interaction ----------
    void Cell_Click(object sender, RoutedEventArgs e)
    {
        var b = (Button)sender;
        switch (_mode)
        {
            case Mode.Days when b.Tag is DateTime d:
                int dir = 0;
                if (PersianDate.Month(d) != _m || PersianDate.Year(d) != _y)   // a dimmed day of a neighbouring month
                {
                    dir = d < PersianDate.ToGregorian(_y, _m, 1) ? -1 : 1;
                    _y = PersianDate.Year(d);
                    _m = PersianDate.Month(d);
                }
                _selected = d;
                Render(dir, dir != 0);
                return;
            case Mode.Months when b.Tag is int m:
                _m = m;
                _mode = Mode.Days;
                break;
            case Mode.Years when b.Tag is int y:
                _y = y;
                _mode = Mode.Months;
                break;
            default:
                return;
        }
        Render(0, true);
    }

    void Title_Click(object sender, RoutedEventArgs e)
    {
        if (_mode == Mode.Days) _mode = Mode.Months;
        else if (_mode == Mode.Months) _mode = Mode.Years;
        else return;
        Render(0, true);
    }

    void Today_Click(object sender, RoutedEventArgs e) => ShowToday(true);
    void Up_Click(object sender, RoutedEventArgs e) => Step(-1);
    void Down_Click(object sender, RoutedEventArgs e) => Step(+1);

    // days: ±month · months: ±year · years: ±decade
    void Step(int dir)
    {
        switch (_mode)
        {
            case Mode.Days:
                _m += dir;
                if (_m > 12) { _m = 1; _y++; }
                if (_m < 1) { _m = 12; _y--; }
                break;
            case Mode.Months:
                _y += dir;
                break;
            default:
                _y += 10 * dir;
                break;
        }
        _y = Math.Clamp(_y, 1, MaxYear);
        Render(dir, true);
    }

    async Task LoadEventsAsync(DateTime day)
    {
        var list = await App.Events.GetEventsAsync(day, day.AddDays(1));
        if (day != _selected.Date) return;   // selection changed while loading
        EventsList.ItemsSource = list;
        NoEvents.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}
