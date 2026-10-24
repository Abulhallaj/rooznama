using Microsoft.UI.Xaml.Controls;

namespace Rooznama.Views;

// Home page of the shell window: the same panel, in its large variant (Gregorian day per cell, Today button).
public sealed partial class CalendarPage : Page
{
    public CalendarPage()
    {
        InitializeComponent();
        BigCal.IsLarge = true;
    }
}
