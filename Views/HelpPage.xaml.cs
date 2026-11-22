using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Rooznama.Help;
using Rooznama.Services;

namespace Rooznama.Views;

/// <summary>The in-app help: one expandable topic per HelpSection, with a search box. The texts live in Help/Sections.</summary>
public sealed partial class HelpPage : Page
{
    readonly bool _fa = Loc.IsFa;
    readonly Dictionary<HelpSection, Expander> _boxes = new();

    public HelpPage()
    {
        InitializeComponent();
        TitleText.Text = HelpCatalog.PageTitle(_fa);
        SearchBox.PlaceholderText = HelpCatalog.SearchPlaceholder(_fa);
        NoResults.Text = HelpCatalog.NoResults(_fa);

        foreach (var section in HelpCatalog.All)
        {
            var box = Build(section);
            _boxes[section] = box;
            Topics.Children.Add(box);
        }
        if (Topics.Children.FirstOrDefault() is Expander first) first.IsExpanded = true;
    }

    Expander Build(HelpSection section)
    {
        var body = new StackPanel { Spacing = 8, Padding = new Thickness(4, 4, 4, 8) };
        if (section.Since.Length > 0)
            body.Children.Add(new TextBlock { Text = HelpCatalog.SinceLabel(_fa, section.Since), Opacity = 0.6, FontSize = 12 });
        foreach (var block in HelpMarkup.Parse(HelpCatalog.BodyOf(section, _fa)))
            body.Children.Add(Render(block));

        return new Expander
        {
            Header = new TextBlock { Text = HelpCatalog.TitleOf(section, _fa), FontWeight = FontWeights.SemiBold },
            Content = body,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
    }

    UIElement Render(HelpBlock block)
    {
        switch (block.Kind)
        {
            case HelpBlockKind.Heading:
            {
                var heading = Text(block.Text, 15);
                heading.FontWeight = FontWeights.SemiBold;
                heading.Margin = new Thickness(0, 8, 0, 0);
                return heading;
            }
            case HelpBlockKind.Bullet:
            {
                var row = new Grid { ColumnSpacing = 8 };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.Children.Add(new TextBlock { Text = "•", Opacity = 0.7 });
                var text = Text(block.Text);
                Grid.SetColumn(text, 1);
                row.Children.Add(text);
                return row;
            }
            case HelpBlockKind.Note:
            {
                var row = new Grid { ColumnSpacing = 8 };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.Children.Add(new FontIcon { Glyph = "\uE946", FontSize = 14, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 2, 0, 0) });
                var text = Text(block.Text);
                Grid.SetColumn(text, 1);
                row.Children.Add(text);
                return new Border { Style = (Style)Resources["NoteBox"], Child = row };
            }
            default:
                return Text(block.Text);
        }
    }

    // a wrapped, selectable line; **bold** pieces become bold runs
    static TextBlock Text(string markup, double size = 14)
    {
        var text = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = size, IsTextSelectionEnabled = true };
        foreach (var run in HelpMarkup.Runs(markup))
            text.Inlines.Add(new Run { Text = run.Text, FontWeight = run.Bold ? FontWeights.SemiBold : FontWeights.Normal });
        return text;
    }

    void Search_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        var hits = HelpCatalog.Search(sender.Text, _fa);
        bool searching = sender.Text.Trim().Length > 0;
        foreach (var (section, box) in _boxes)
        {
            bool show = hits.Contains(section);
            box.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (searching && show) box.IsExpanded = true;
        }
        NoResults.Visibility = hits.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}
