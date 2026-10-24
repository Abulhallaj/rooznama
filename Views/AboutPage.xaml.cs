using System.Reflection;
using Microsoft.UI.Xaml.Controls;
using Rooznama.Services;

namespace Rooznama.Views;

public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();
        var v = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
        VersionText.Text = $"{Loc.T("about.version")} {PersianDate.Digits(v)}";
        FontText.Text = $"{Loc.T("about.font")}: {App.FontSource}";
    }
}
