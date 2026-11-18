using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rooznama.Services;
using Windows.ApplicationModel.DataTransfer;

namespace Rooznama.Views;

public sealed partial class ConverterPage : Page
{
    // The Shamsi and the Gregorian picker hold the same Gregorian DateTimeOffset; they only differ in how they display it.
    // The Hijri picker counts months and days by Umm al-Qura, so it holds the day whose Umm al-Qura date has the same year, month
    // and day as the Hijri date of the selected source: it then shows the date the rest of the app shows.
    bool _sync;
    static readonly UmAlQuraHijriSource UmAlQura = new();
    static readonly HijriProvider UmAlQuraOnly = new(() => HijriProvider.UmAlQuraId, UtHijriSource.Empty);

    public ConverterPage()
    {
        InitializeComponent();
        FaPicker.Language = Loc.Lang;
        FaPicker.DateFormat = "{dayofweek.full} {day.integer} {month.full} {year.full}";
        GPicker.DateFormat = "{dayofweek.full} {day.integer} {month.full} {year.full}";
        HPicker.Language = Loc.IsFa ? "ar-SA" : "en-US";   // Arabic digits in Persian mode
        HPicker.DateFormat = "{dayofweek.full} {day.integer} {month.full} {year.full}";

        var min = new DateTimeOffset(new DateTime(1900, 1, 1));
        var max = new DateTimeOffset(new DateTime(2200, 12, 31));
        HPicker.MinDate = new DateTimeOffset(new DateTime(1900, 5, 1));      // what Umm al-Qura can count
        HPicker.MaxDate = new DateTimeOffset(new DateTime(2077, 11, 16));
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

    // choosing a Hijri date: read the Umm al-Qura date the picker shows, find the day that has it in the selected source
    void H_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs e)
    {
        if (_sync || e.NewDate is not { } picked) return;

        if (UmAlQura.TryGet(picked.Date) is not { } shown
            || HijriLookup.FindGregorian(shown, HijriService.Provider) is not { } day
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
        UpdateTexts(updateHijriPicker: false);
    }

    // the Hijri picker follows the Gregorian date; when Umm al-Qura has no day with that date (an official 30th day in a month
    // it counts as 29 days) the picker shows the day itself and the line under it gives the exact date
    void ShowInHijriPicker(HijriDate hijri, DateTime day)
    {
        var shown = HijriLookup.FindGregorian(hijri, UmAlQuraOnly) ?? day;
        if (shown < HPicker.MinDate.Date || shown > HPicker.MaxDate.Date) return;
        _sync = true;
        try { HPicker.Date = new DateTimeOffset(shown); }
        finally { _sync = false; }
    }

    void UpdateTexts(bool updateHijriPicker = true)
    {
        if (GPicker.Date is { } d)
        {
            var day = d.Date;
            FaNum.Text = PersianDate.Numeric(day);
            GNum.Text = day.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
            if (HijriService.Get(day) is { } h)
            {
                HNum.Text = HijriService.Digits($"{h.Year:0000}/{h.Month:00}/{h.Day:00}");   // Arabic digits
                if (updateHijriPicker) ShowInHijriPicker(h, day);
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
