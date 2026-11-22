using Rooznama.Help;
using Xunit;

namespace Rooznama.Tests;

public sealed class HelpTests
{
    // ---------- the markup ----------
    [Fact]
    public void Headings_bullets_notes_and_paragraphs_are_told_apart()
    {
        var blocks = HelpMarkup.Parse("""
            first line
            second line

            ## A heading
            - one
            - two
            > a note
            last
            """);

        Assert.Equal(new[]
        {
            (HelpBlockKind.Paragraph, "first line second line"),
            (HelpBlockKind.Heading, "A heading"),
            (HelpBlockKind.Bullet, "one"),
            (HelpBlockKind.Bullet, "two"),
            (HelpBlockKind.Note, "a note"),
            (HelpBlockKind.Paragraph, "last")
        }, blocks.Select(b => (b.Kind, b.Text)));
    }

    [Fact]
    public void Blank_lines_and_windows_line_endings_are_handled()
    {
        Assert.Empty(HelpMarkup.Parse(""));
        Assert.Empty(HelpMarkup.Parse("\n\n  \n"));
        var blocks = HelpMarkup.Parse("a\r\n\r\nb");
        Assert.Equal(new[] { "a", "b" }, blocks.Select(b => b.Text));
    }

    [Fact]
    public void Bold_pieces_alternate_with_normal_ones()
    {
        var runs = HelpMarkup.Runs("a **b** c **d**");
        Assert.Equal(new[] { ("a ", false), ("b", true), (" c ", false), ("d", true) }, runs.Select(r => (r.Text, r.Bold)));
        Assert.Equal("a b c d", HelpMarkup.Plain("a **b** c **d**"));
        Assert.Empty(HelpMarkup.Runs(""));
    }

    // ---------- the topics ----------
    [Fact]
    public void Topics_are_found_automatically_in_order_with_unique_ids()
    {
        var all = HelpCatalog.All;
        Assert.True(all.Count >= 8);
        Assert.Equal(all.Select(s => s.Order).OrderBy(o => o), all.Select(s => s.Order));
        Assert.Equal(all.Count, all.Select(s => s.Id).Distinct().Count());
        Assert.Contains(all, s => s.Id == "hijri");
    }

    [Fact]
    public void Every_topic_has_a_title_and_a_text_in_both_languages()
    {
        foreach (var s in HelpCatalog.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(s.Title.Fa), s.Id);
            Assert.False(string.IsNullOrWhiteSpace(s.Title.En), s.Id);
            foreach (var fa in new[] { true, false })
            {
                var blocks = HelpMarkup.Parse(HelpCatalog.BodyOf(s, fa));
                Assert.NotEmpty(blocks);
                Assert.All(blocks, b => Assert.False(string.IsNullOrWhiteSpace(b.Text), $"{s.Id} {(fa ? "fa" : "en")}"));
            }
        }
    }

    [Fact]
    public void Both_languages_of_a_topic_have_the_same_shape()
    {
        // a translation that lost a bullet or a note is a typo: compare the kinds of blocks
        foreach (var s in HelpCatalog.All)
        {
            var fa = HelpMarkup.Parse(s.Body.Fa).Select(b => b.Kind);
            var en = HelpMarkup.Parse(s.Body.En).Select(b => b.Kind);
            Assert.Equal(fa, en);
        }
    }

    [Fact]
    public void Bold_markers_are_balanced_in_every_line()
    {
        foreach (var s in HelpCatalog.All)
            foreach (var body in new[] { s.Body.Fa, s.Body.En })
                foreach (var block in HelpMarkup.Parse(body))
                    Assert.True((block.Text.Split("**").Length - 1) % 2 == 0, $"{s.Id}: {block.Text}");
    }

    [Fact]
    public void The_version_a_topic_names_looks_like_a_version()
    {
        foreach (var s in HelpCatalog.All.Where(s => s.Since.Length > 0))
            Assert.Matches(@"^\d+\.\d+\.\d+$", s.Since);
    }

    // ---------- search ----------
    [Fact]
    public void Search_finds_topics_by_a_word_of_the_title_or_the_text_in_either_language()
    {
        Assert.Contains(HelpCatalog.Search("قمری", true), s => s.Id == "hijri");
        Assert.Contains(HelpCatalog.Search("Hijri", false), s => s.Id == "hijri");
        Assert.Contains(HelpCatalog.Search("clipboard", false), s => s.Id == "converter");
        Assert.Contains(HelpCatalog.Search("%AppData%", false), s => s.Id == "troubleshooting");
    }

    [Fact]
    public void Search_needs_every_word_and_ignores_case_arabic_letters_and_digits()
    {
        Assert.Contains(HelpCatalog.Search("\u0642\u0645\u0631\u064a", true), s => s.Id == "hijri");   // Arabic ye
        Assert.Contains(HelpCatalog.Search("HIJRI umm", false), s => s.Id == "hijri");
        Assert.Empty(HelpCatalog.Search("hijri zzzzqq", false));
        Assert.Contains(HelpCatalog.Search("1405", false), s => s.Id == "occasions");
        Assert.Contains(HelpCatalog.Search("\u06f1\u06f4\u06f0\u06f5", true), s => s.Id == "occasions");   // Persian digits
    }

    [Fact]
    public void An_empty_search_lists_everything()
    {
        Assert.Equal(HelpCatalog.All.Count, HelpCatalog.Search("", true).Count);
        Assert.Equal(HelpCatalog.All.Count, HelpCatalog.Search("   ", false).Count);
    }
}
