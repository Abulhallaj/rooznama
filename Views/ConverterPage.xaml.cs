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

    void UpdateTexts()
    {
        if (GPicker.Date is { } d)
        {
            var day = d.Date;
            FaNum.Text = PersianDate.Numeric(day);
            GNum.Text = day.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
        }
        else
        {
            FaNum.Text = GNum.Text = "";
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
}
