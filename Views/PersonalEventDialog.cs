using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Rooznama.Services;

namespace Rooznama.Views;

/// <summary>Add / edit dialog for a personal event. Built in code (no XAML file to keep in sync) and shown on the main window only.</summary>
public static class PersonalEventDialog
{
    public enum Outcome { Cancelled, Saved, Deleted }

    public static async Task<Outcome> ShowAsync(XamlRoot root, PersonalEvent? existing, DateTime day)
    {
        bool isNew = existing is null;
        var ev = existing?.Clone() ?? new PersonalEvent { IsAllDay = true, Start = day.Date, End = day.Date.AddDays(1) };
        var date = ev.Start.Date;

        var title = new TextBox { Header = Loc.T("event.title"), Text = ev.Title, MaxLength = 200 };
        var picker = new CalendarDatePicker
        {
            Header = Loc.T("event.date"),
            CalendarIdentifier = Loc.IsFa ? "PersianCalendar" : "GregorianCalendar",
            Language = Loc.Lang,
            Date = new DateTimeOffset(date),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        var allDay = new CheckBox { Content = Loc.T("event.allday"), IsChecked = ev.IsAllDay };
        var start = new TimePicker
        {
            Header = Loc.T("event.start"),
            ClockIdentifier = "24HourClock",
            MinuteIncrement = 5,
            Time = ev.IsAllDay ? new TimeSpan(9, 0, 0) : ev.Start.TimeOfDay
        };
        var end = new TimePicker
        {
            Header = Loc.T("event.end"),
            ClockIdentifier = "24HourClock",
            MinuteIncrement = 5,
            Time = ev.IsAllDay ? new TimeSpan(10, 0, 0) : ev.End.TimeOfDay
        };
        var notes = new TextBox
        {
            Header = Loc.T("event.notes"),
            Text = ev.Description ?? "",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 80
        };
        var repeat = new CheckBox { Content = Loc.T("event.repeat"), IsChecked = ev.Recurrence != EventRecurrence.None };
        var repeatIn = new ComboBox { Header = Loc.T("event.repeat.in"), HorizontalAlignment = HorizontalAlignment.Stretch };
        repeatIn.Items.Add(new ComboBoxItem { Content = Loc.T("cal.jalali"), Tag = EventRecurrence.YearlyJalali });
        repeatIn.Items.Add(new ComboBoxItem { Content = Loc.T("cal.hijri"), Tag = EventRecurrence.YearlyHijri });
        repeatIn.Items.Add(new ComboBoxItem { Content = Loc.T("cal.gregorian"), Tag = EventRecurrence.YearlyGregorian });
        var wanted = ev.Recurrence == EventRecurrence.None ? EventRecurrence.YearlyJalali : ev.Recurrence;
        repeatIn.SelectedItem = repeatIn.Items.OfType<ComboBoxItem>().First(i => (EventRecurrence)i.Tag == wanted);

        var panel = new StackPanel { Spacing = 12, MinWidth = 340 };
        foreach (UIElement c in new UIElement[] { title, picker, allDay, start, end, notes, repeat, repeatIn })
            panel.Children.Add(c);

        var dialog = new ContentDialog
        {
            XamlRoot = root,
            Title = Loc.T(isNew ? "event.add" : "event.edit"),
            Content = panel,
            PrimaryButtonText = Loc.T("event.save"),
            SecondaryButtonText = isNew ? "" : Loc.T("event.delete"),
            CloseButtonText = Loc.T("event.cancel"),
            DefaultButton = ContentDialogButton.Primary,
            FlowDirection = Loc.Flow,
            FontFamily = (FontFamily)Application.Current.Resources["AppFontFamily"]
        };

        void Refresh()
        {
            bool timed = allDay.IsChecked != true;
            start.IsEnabled = end.IsEnabled = timed;
            repeatIn.IsEnabled = repeat.IsChecked == true;
            dialog.IsPrimaryButtonEnabled =
                !string.IsNullOrWhiteSpace(title.Text) && picker.Date is not null && (!timed || end.Time > start.Time);
        }
        title.TextChanged += (_, _) => Refresh();
        picker.DateChanged += (_, _) => Refresh();
        allDay.Click += (_, _) => Refresh();
        repeat.Click += (_, _) => Refresh();
        start.TimeChanged += (_, _) => Refresh();
        end.TimeChanged += (_, _) => Refresh();
        Refresh();

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Secondary)
            return App.PersonalEvents.Delete(ev.Id) ? Outcome.Deleted : Outcome.Cancelled;
        if (result != ContentDialogResult.Primary) return Outcome.Cancelled;

        var d = picker.Date!.Value.Date;
        ev.Title = title.Text.Trim();
        ev.Description = string.IsNullOrWhiteSpace(notes.Text) ? null : notes.Text.Trim();
        ev.IsAllDay = allDay.IsChecked == true;
        ev.Start = ev.IsAllDay ? d : d + start.Time;
        ev.End = ev.IsAllDay ? d.AddDays(1) : d + end.Time;
        ev.TimeZone = TimeZoneInfo.Local.Id;

        ev.Recurrence = EventRecurrence.None;
        ev.RecurrenceMonth = ev.RecurrenceDay = 0;
        if (repeat.IsChecked == true
            && (repeatIn.SelectedItem as ComboBoxItem)?.Tag is EventRecurrence kind
            && EventRecurrenceFields.Of(d, kind, x => HijriService.Get(x)) is { } fields)
        {
            ev.Recurrence = kind;
            ev.RecurrenceMonth = fields.Month;
            ev.RecurrenceDay = fields.Day;
        }

        return App.PersonalEvents.Save(ev) ? Outcome.Saved : Outcome.Cancelled;
    }
}
