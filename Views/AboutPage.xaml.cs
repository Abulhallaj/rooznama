using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rooznama.Services;

namespace Rooznama.Views;

public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();

        // the number is forced left-to-right (LRE…PDF) so "0.6.0" / "0.0.0-dev.5" aren't re-ordered inside the RTL line
        VersionText.Text = $"{Loc.T("about.version")} \u202A{PersianDate.Digits(AppInfo.VersionText)}\u202C";
        ProjectLink.NavigateUri = new Uri(AppInfo.RepoUrl);
        IssuesLink.NavigateUri = new Uri(AppInfo.IssuesUrl);
        FontText.Text = $"{Loc.T("about.font")}: {App.FontSource}";

        if (AppPaths.IsPortable)
        {
            PortableText.Text = Loc.T("about.portable");
            PortableText.Visibility = Visibility.Visible;
        }

        if (UpdateService.Available is { } u)
        {
            UpdateText.Text = string.Format(Loc.T("upd.available"), u.Tag);
            UpdateLink.Content = Loc.T("upd.download");
            UpdateLink.NavigateUri = new Uri(u.Url);
            UpdateRow.Visibility = Visibility.Visible;
        }
    }
}
