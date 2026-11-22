using System.Text;

namespace Rooznama.Help;

/// <summary>All help topics, found by looking for every HelpSection class, plus the words the help page itself needs.</summary>
public static class HelpCatalog
{
    static readonly Lazy<IReadOnlyList<HelpSection>> Found = new(Discover);

    public static IReadOnlyList<HelpSection> All => Found.Value;

    static IReadOnlyList<HelpSection> Discover() =>
        typeof(HelpSection).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(HelpSection)))
            .Select(t => (HelpSection)Activator.CreateInstance(t)!)
            .OrderBy(s => s.Order).ThenBy(s => s.Id, StringComparer.Ordinal)
            .ToList();

    // ---- the page's own words (here, so the shared strings file does not change)
    public static string PageTitle(bool fa) => fa ? "راهنما" : "Help";
    public static string SearchPlaceholder(bool fa) => fa ? "جستجو در راهنما" : "Search the help";
    public static string NoResults(bool fa) => fa ? "موردی پیدا نشد." : "Nothing found.";
    public static string SinceLabel(bool fa, string version) => fa ? $"از نسخه {version}" : $"Since version {version}";

    public static string TitleOf(HelpSection s, bool fa) => fa ? s.Title.Fa : s.Title.En;
    public static string BodyOf(HelpSection s, bool fa) => fa ? s.Body.Fa : s.Body.En;

    /// <summary>The topics whose title or text contains every word of the query (all topics for an empty query).</summary>
    public static IReadOnlyList<HelpSection> Search(string query, bool fa)
    {
        var words = Normalize(query).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return All;
        return All.Where(s =>
        {
            var haystack = Normalize(TitleOf(s, fa) + " " + HelpMarkup.Plain(BodyOf(s, fa)));
            return words.All(w => haystack.Contains(w, StringComparison.Ordinal));
        }).ToList();
    }

    /// <summary>Lower case, Persian ye/kaf for the Arabic ones, no ZWNJ, no digits difference: so "قمري" finds "قمری".</summary>
    public static string Normalize(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var c in s.Trim())
        {
            if (c is '\u200c' or '\u200d' or '\u200f' or '\u200e') continue;
            sb.Append(c switch
            {
                '\u064a' => '\u06cc',                              // Arabic ye -> Persian ye
                '\u0643' => '\u06a9',                              // Arabic kaf -> Persian kaf
                >= '\u0660' and <= '\u0669' => (char)('0' + (c - '\u0660')),   // Arabic-Indic digits
                >= '\u06f0' and <= '\u06f9' => (char)('0' + (c - '\u06f0')),   // Persian digits
                _ => char.ToLowerInvariant(c)
            });
        }
        return sb.ToString();
    }
}
