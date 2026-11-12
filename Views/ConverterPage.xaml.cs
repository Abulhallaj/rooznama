using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rooznama.Services;
using Windows.ApplicationModel.DataTransfer;

namespace Rooznama.Views;

public sealed partial class ConverterPage : Page
{
    // Both pickers hold the same Gregorian DateTimeOffset; they only differ in how they display it.
    bool _sync;

    public ConverterPage()
    {
        InitializeComponent();
        FaPicker.Language = Loc.Lang;
        FaPicker.DateFormat = "{dayofweek.full} {day.integer} {month.full} {year.full}";
        GPicker.DateFormat = "{dayofweek.full} {day.integer} {month.full} {year.full}";

        var min = new DateTimeOffset(new DateTime(1900, 1, 1));
        var max = new DateTimeOffset(new DateTime(2200, 12, 31));
        for (int m = 1; m <= 12; m++) HMonth.Items.Add(HijriService.MonthName(m));
        FaPicker.MinDate = GPicker.MinDate = min;
        FaPicker.MaxDate = GPicker.MaxDate = max;

        var today = new DateTimeOffset(DateTime.Today);
        FaPicker.Date = today;
        GPicker.Date = today;
        UpdateTexts();
    }

    void Fa_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs e) => Mirror(e.NewDate, GPicker);
    void G_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs e) => Mirror(e.NewDate, FaPicker);

    void Mirror(DateTimeOffset? date, CalendarDatePicker other)
    {
        if (_sync) return;
        _sync = true;
        try { if (other.Date != date) other.Date = date; }
        finally { _sync = false; }
        UpdateTexts();
    }

    // the Hijri boxes follow the Gregorian date, and drive it when the person types a Hijri date
    void Hijri_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs e) => FromHijri();
    void Hijri_SelectionChanged(object sender, SelectionChangedEventArgs e) => FromHijri();

    void FromHijri()
    {
        if (_sync || double.IsNaN(HYear.Value) || double.IsNaN(HDay.Value) || HMonth.SelectedIndex < 0) return;

        var h = new HijriDate((int)HYear.Value, HMonth.SelectedIndex + 1, (int)HDay.Value);
        if (HijriLookup.FindGregorian(h, HijriService.Provider) is not { } day
            || day < GPicker.MinDate.Date || day > GPicker.MaxDate.Date)
        {
            HNum.Text = Loc.T("conv.hijri.invalid");
            return;
        }

        _sync = true;
        try
        {
            var date = new DateTimeOffset(day);
            GPicker.Date = date;
            FaPicker.Date = date;
        }
        finally { _sync = false; }
        UpdateTexts(updateHijriBoxes: false);
    }

    void UpdateTexts(bool updateHijriBoxes = true)
    {
        if (GPicker.Date is { } d)
        {
            var day = d.Date;
            FaNum.Text = PersianDate.Numeric(day);
            GNum.Text = day.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
            if (HijriService.Get(day) is { } h)
            {
                HNum.Text = HijriService.Digits($"{h.Year:0000}/{h.Month:00}/{h.Day:00}");   // Arabic digits
                if (updateHijriBoxes)
                {
                    _sync = true;
                    try { HYear.Value = h.Year; HMonth.SelectedIndex = h.Month - 1; HDay.Value = h.Day; }
                    finally { _sync = false; }
                }
            }
            else HNum.Text = Loc.T("conv.hijri.none");
        }
        else
        {
            FaNum.Text = GNum.Text = HNum.Text = "";
        }
    }

    static void Copy(string text)
    {
        var dp = new DataPackage();
        dp.SetText(text);
        Clipboard.SetContent(dp);
    }

    void CopyFa_Click(object sender, RoutedEventArgs e) => Copy(FaNum.Text);
    void CopyG_Click(object sender, RoutedEventArgs e) => Copy(GNum.Text);
    void CopyH_Click(object sender, RoutedEventArgs e) => Copy(HNum.Text);
}
