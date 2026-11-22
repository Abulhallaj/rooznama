using System.Text;

namespace Rooznama.Help;

public enum HelpBlockKind { Heading, Paragraph, Bullet, Note }

public sealed record HelpBlock(HelpBlockKind Kind, string Text);

public readonly record struct HelpRun(string Text, bool Bold);

/// <summary>
/// One topic of the in-app help. A topic is one small class in Help/Sections; it is found automatically, so adding a feature to
/// the help means adding one file and touching nothing else. Titles and bodies are written in Persian and English.
/// </summary>
public abstract class HelpSection
{
    /// <summary>Unique, stable name of the topic.</summary>
    public abstract string Id { get; }
    /// <summary>Position in the list (lower first).</summary>
    public abstract int Order { get; }
    public abstract (string Fa, string En) Title { get; }
    /// <summary>The text, in the tiny markup of <see cref="HelpMarkup"/>.</summary>
    public abstract (string Fa, string En) Body { get; }
    /// <summary>The version that brought the feature ("0.8.0"); empty for what was always there.</summary>
    public virtual string Since => "";
}

/// <summary>
/// The help markup: "## heading", "- bullet", "> note", a blank line between paragraphs, and **bold** inside a line.
/// Lines of one paragraph are joined with a space.
/// </summary>
public static class HelpMarkup
{
    public static IReadOnlyList<HelpBlock> Parse(string text)
    {
        var blocks = new List<HelpBlock>();
        var paragraph = new StringBuilder();

        void Flush()
        {
            if (paragraph.Length == 0) return;
            blocks.Add(new HelpBlock(HelpBlockKind.Paragraph, paragraph.ToString()));
            paragraph.Clear();
        }

        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) { Flush(); continue; }

            if (line.StartsWith("## ", StringComparison.Ordinal)) { Flush(); blocks.Add(new HelpBlock(HelpBlockKind.Heading, line[3..].Trim())); }
            else if (line.StartsWith("- ", StringComparison.Ordinal)) { Flush(); blocks.Add(new HelpBlock(HelpBlockKind.Bullet, line[2..].Trim())); }
            else if (line.StartsWith("> ", StringComparison.Ordinal)) { Flush(); blocks.Add(new HelpBlock(HelpBlockKind.Note, line[2..].Trim())); }
            else
            {
                if (paragraph.Length > 0) paragraph.Append(' ');
                paragraph.Append(line);
            }
        }
        Flush();
        return blocks;
    }

    /// <summary>Splits a line at the ** markers into normal and bold pieces.</summary>
    public static IReadOnlyList<HelpRun> Runs(string line)
    {
        var runs = new List<HelpRun>();
        var parts = line.Split("**");
        for (int i = 0; i < parts.Length; i++)
            if (parts[i].Length > 0) runs.Add(new HelpRun(parts[i], i % 2 == 1));
        return runs;
    }

    public static string Plain(string text) => text.Replace("**", "");
}
