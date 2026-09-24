using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Bark.Models;

namespace Bark.Services.Rendering;

public sealed class Localization
{
    private static readonly JsonSerializerOptions LocaleJsonOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = false
    };

    private static readonly Dictionary<string, string> Defaults = new(StringComparer.Ordinal)
    {
        ["skipToContent"] = "Skip to content",
        ["navToggle"] = "Toggle navigation menu",
        ["sidebarAria"] = "Documentation navigation",
        ["topNavAria"] = "Main navigation",
        ["footerNavAria"] = "Footer navigation",
        ["breadcrumbAria"] = "Breadcrumb",
        ["breadcrumbHome"] = "Home",
        ["themeToggle"] = "Toggle dark mode",
        ["tocTitle"] = "On this page",
        ["tocMobileSummary"] = "On this page",
        ["tocAria"] = "Table of contents",
        ["tocCollapse"] = "Hide table of contents",
        ["tocExpand"] = "Show table of contents",
        ["searchTrigger"] = "Search",
        ["searchAria"] = "Search documentation",
        ["searchHeading"] = "Search documentation",
        ["searchPlaceholder"] = "Search documentation...",
        ["searchClose"] = "Close search",
        ["searchResultsAria"] = "Search results",
        ["searchNavigate"] = "Navigate",
        ["searchSelect"] = "Select",
        ["searchEsc"] = "Close",
        ["keyArrowDown"] = "Arrow down",
        ["keyArrowUp"] = "Arrow up",
        ["keyEnter"] = "Enter key",
        ["keyEscape"] = "Escape key",
        ["searchSearching"] = "Searching…",
        ["searchNoResults"] = "No results found.",
        ["searchError"] = "Something went wrong. Try again.",
        ["searchFailed"] = "Search failed.",
        ["searchResultSingular"] = "result found.",
        ["searchResultPlural"] = "results found.",
        ["pagerPrevious"] = "Previous",
        ["pagerNext"] = "Next",
        ["pagerAria"] = "Pagination",
        ["pageOptions"] = "Page options",
        ["copyPage"] = "Copy page",
        ["viewAsMarkdown"] = "View as Markdown",
        ["copyRssUrl"] = "Copy RSS feed URL",
        ["copied"] = "Copied!",
        ["opensInNewTab"] = "opens in a new tab",
        ["copyFailed"] = "Failed",
        ["promoAria"] = "Announcement",
        ["promoDismiss"] = "Dismiss announcement",
        ["lastUpdated"] = "Last updated:",
        ["permalinkTo"] = "Permalink to \"{0}\"",
        ["notFoundTitle"] = "Page not found",
        ["notFoundMessage"] = "The page you're looking for doesn't exist.",
        ["notFoundHome"] = "Return home",
        ["setupHeading"] = "Let's set up your homepage",
        ["setupBody"] = "Bark is running. To choose what appears on this page, create an <code>index.md</code> file in your docs folder. It will be shown here as your homepage.",
        ["setupExistingPages"] = "In the meantime, here are the pages you have already written:",
        ["setupNoFiles"] = "There are no Markdown files yet. Add a <code>.md</code> file to your docs folder and it appears here at once. There is no build step to wait for.",
        ["localeSwitcher"] = "Change language",
        ["translationMissing"] = "This page has not been translated yet, so you are reading the original.",
        ["translationMissingLink"] = "Open the original page",
        ["translationStale"] = "The original of this page changed after this translation was written, so parts of it may be out of date.",
        ["translationStaleLink"] = "Compare with the original",
        ["translationMachine"] = "This page was translated by a machine, so some sentences may read badly. Corrections are welcome.",
        ["repoWidgetAria"] = "{0} repository"
    };

    private readonly IReadOnlyDictionary<string, string> _map;
    private readonly IReadOnlyDictionary<string, string> _labels;

    private Localization(IReadOnlyDictionary<string, string> map, string code, IReadOnlyDictionary<string, string>? labels = null)
    {
        _map = map;
        _labels = labels ?? EmptyLabels;
        Code = code;
    }

    private static readonly Dictionary<string, string> EmptyLabels = new(StringComparer.Ordinal);

    public const string LabelSection = "config";

    [return: NotNullIfNotNull(nameof(text))]
    public string? Label(string? text) =>
        text is { Length: > 0 } && _labels.TryGetValue(text, out var translated) ? translated : text;

    public string Code { get; }

    public static Localization Default { get; } = new(Defaults, "en");

    public static IReadOnlyCollection<string> Keys => Defaults.Keys;

    public static LocaleTables BuildAll(string docsPath, Config? config, ILogger logger)
    {
        var root = From(docsPath, config, logger);
        var tables = new Dictionary<string, Localization>(StringComparer.OrdinalIgnoreCase) { [root.Code] = root };

        foreach (var code in LocaleRouting.TreeCodes(config))
            if (!tables.ContainsKey(code))
                tables[code] = FromCode(docsPath, code, logger);

        return new LocaleTables(tables, root);
    }

    private string this[string key] =>
        _map.TryGetValue(key, out var value) ? value
        : Defaults.TryGetValue(key, out var fallback) ? fallback
        : key;

    private string Format(string key, params object[] args) =>
        string.Format(CultureInfo.InvariantCulture, this[key], args);

    public string SkipToContent => this["skipToContent"];
    public string NavToggle => this["navToggle"];
    public string SidebarAria => this["sidebarAria"];
    public string TopNavAria => this["topNavAria"];
    public string FooterNavAria => this["footerNavAria"];
    public string BreadcrumbAria => this["breadcrumbAria"];
    public string BreadcrumbHome => this["breadcrumbHome"];
    public string ThemeToggle => this["themeToggle"];
    public string TocTitle => this["tocTitle"];
    public string TocMobileSummary => this["tocMobileSummary"];
    public string TocAria => this["tocAria"];
    public string TocCollapse => this["tocCollapse"];
    public string TocExpand => this["tocExpand"];
    public string SearchTrigger => this["searchTrigger"];
    public string SearchAria => this["searchAria"];
    public string SearchHeading => this["searchHeading"];
    public string SearchPlaceholder => this["searchPlaceholder"];
    public string SearchClose => this["searchClose"];
    public string SearchResultsAria => this["searchResultsAria"];
    public string SearchNavigate => this["searchNavigate"];
    public string SearchSelect => this["searchSelect"];
    public string SearchEsc => this["searchEsc"];
    public string KeyArrowDown => this["keyArrowDown"];
    public string KeyArrowUp => this["keyArrowUp"];
    public string KeyEnter => this["keyEnter"];
    public string KeyEscape => this["keyEscape"];
    public string SearchSearching => this["searchSearching"];
    public string SearchNoResults => this["searchNoResults"];
    public string SearchError => this["searchError"];
    public string SearchFailed => this["searchFailed"];
    public string SearchResultSingular => this["searchResultSingular"];
    public string SearchResultPlural => this["searchResultPlural"];
    public string PagerPrevious => this["pagerPrevious"];
    public string PagerNext => this["pagerNext"];
    public string PagerAria => this["pagerAria"];
    public string PageOptions => this["pageOptions"];
    public string CopyPage => this["copyPage"];
    public string ViewAsMarkdown => this["viewAsMarkdown"];
    public string CopyRssUrl => this["copyRssUrl"];
    public string Copied => this["copied"];
    public string OpensInNewTab => this["opensInNewTab"];
    public string CopyFailed => this["copyFailed"];
    public string PromoAria => this["promoAria"];
    public string PromoDismiss => this["promoDismiss"];
    public string LastUpdated => this["lastUpdated"];
    public string PermalinkTo(string title) => Format("permalinkTo", title);
    public string NotFoundTitle => this["notFoundTitle"];
    public string NotFoundMessage => this["notFoundMessage"];
    public string NotFoundHome => this["notFoundHome"];
    public string SetupHeading => this["setupHeading"];
    public string SetupBody => this["setupBody"];
    public string SetupExistingPages => this["setupExistingPages"];
    public string SetupNoFiles => this["setupNoFiles"];
    public string LocaleSwitcher => this["localeSwitcher"];
    public string TranslationMissing => this["translationMissing"];
    public string TranslationMissingLink => this["translationMissingLink"];
    public string TranslationStale => this["translationStale"];
    public string TranslationStaleLink => this["translationStaleLink"];
    public string TranslationMachine => this["translationMachine"];
    public string RepoWidgetAria => this["repoWidgetAria"];

    private static Localization From(string docsPath, Config? config, ILogger logger) =>
        FromCode(docsPath, ResolveCode(config), logger);

    private static Localization FromCode(string docsPath, string code, ILogger logger)
    {
        var path = Path.Combine(docsPath, "locale", $"{code}.json");
        if (!File.Exists(path))
            return code == "en" ? Default : new Localization(Defaults, code);

        var filename = Path.GetFileName(path);

        Dictionary<string, JsonElement>? raw;
        try
        {
            raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(path), LocaleJsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning("Locale file {Filename} is invalid. Falling back to default strings. Reason: {Message}", filename, ex.Message);
            return new Localization(Defaults, code);
        }

        if (raw is null || raw.Count == 0)
            return new Localization(Defaults, code);

        var map = new Dictionary<string, string>(Defaults, StringComparer.Ordinal);
        var labels = new Dictionary<string, string>(StringComparer.Ordinal);
        var deadKeys = new List<string>();
        foreach (var (key, element) in raw)
        {
            if (key == LabelSection)
            {
                if (element.ValueKind is JsonValueKind.Object)
                    foreach (var label in element.EnumerateObject())
                        if (label.Value.ValueKind is JsonValueKind.String && label.Value.GetString() is { Length: > 0 } translated)
                            labels[label.Name] = translated;

                continue;
            }

            if (!Defaults.ContainsKey(key))
            {
                deadKeys.Add(key);
                continue;
            }

            if (element.ValueKind is JsonValueKind.String && element.GetString() is { Length: > 0 } value)
                map[key] = value;
        }

        if (deadKeys.Count > 0)
            logger.LogWarning("Locale file {Filename} has unknown keys (no such string, ignored): {Keys}",
                filename, string.Join(", ", deadKeys.Order()));

        return new Localization(map, code, labels);
    }

    private static string ResolveCode(Config? config)
    {
        var raw = Config.ResolveLocale(config)?.Code ?? "en";
        return IsValidCode(raw) ? raw.ToLowerInvariant() : "en";
    }

    private static bool IsValidCode(string value)
    {
        if (value.Length is < 2 or > 12)
            return false;

        foreach (var c in value)
            if (!char.IsAsciiLetterOrDigit(c) && c != '-')
                return false;

        return true;
    }

    public static string JsEncode(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var sb = new StringBuilder(value.Length + 8);
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '\'': sb.Append("\\'"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '<': sb.Append("\\u003C"); break;
                default: sb.Append(c); break;
            }
        }

        return sb.ToString();
    }
}

public sealed record LocaleTables(IReadOnlyDictionary<string, Localization> ByCode, Localization Root)
{
    public static LocaleTables Empty { get; } = new(
        new Dictionary<string, Localization>(StringComparer.OrdinalIgnoreCase) { ["en"] = Localization.Default },
        Localization.Default);

    public Localization For(string? code) =>
        code is { Length: > 0 } && ByCode.TryGetValue(code, out var localization) ? localization : Root;
}
