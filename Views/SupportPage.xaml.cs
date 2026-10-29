using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rooznama.Services;

namespace Rooznama.Views;

public sealed partial class SupportPage : Page
{
    public SupportPage()
    {
        InitializeComponent();
        StarLink.NavigateUri = new Uri(AppInfo.RepoUrl);
    }

    async void Support_Click(object sender, RoutedEventArgs e) =>
        await Windows.System.Launcher.LaunchUriAsync(new Uri(AppInfo.SupportUrl));
}
