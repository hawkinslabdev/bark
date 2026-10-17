using Markdig;
using Markdig.Extensions.Abbreviations;
using Markdig.Extensions.Emoji;
using Markdig.Extensions.Yaml;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Bark.Models;
using Bark.Services.Layout;
using Bark.Services.MarkdownExtensions;
using Bark.Services.Rendering;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Bark.Services;

public sealed partial class MarkdownService
{
    private const string BARK_LCB = "##BARK_LCB##";
    private const string BARK_RCB = "##BARK_RCB##";

    private readonly MarkdownPipeline _pipeline;
    private readonly IDeserializer _yamlDeserializer;
    private readonly string _basePath;
    private readonly ILogger<MarkdownService>? _logger;

    public MarkdownService(
        ISyntaxHighlighter? syntaxHighlighter = null,
        string basePath = "",
        CodeGroupIconOptions? codeGroupIcons = null,
        MathRenderer? mathRenderer = null,
        ILogger<MarkdownService>? logger = null)
    {
        _basePath = basePath;
        _logger = logger;
        var builder = new MarkdownPipelineBuilder()
            .UseAbbreviations()
            .UseAlertBlocks()
            .UseAutoIdentifiers()
            .UseAutoLinks()
            .UseCitations()
            .UseEmphasisExtras()
            .UseDefinitionLists()
            .UseEmojiAndSmiley(EmojiMapping.DefaultEmojisOnlyMapping)
            .UseFootnotes()
            .UseGridTables()
            .UseListExtras()
            .UseMediaLinks()
            .UseMathematics()
            .UsePipeTables()
            .UseTaskLists()
            .UseYamlFrontMatter()

            .UseMarkdownExtensions(syntaxHighlighter, codeGroupIcons, basePath, mathRenderer)

            // UseGenericAttributes() should be the last extension added to the pipeline, as it modifies other parsers to recognize attribute syntax (see https://xoofx.github.io/markdig/docs/extensions/generic-attributes)
            .UseGenericAttributes();

        // Generic attributes allow `{onclick="..."}` on any element, which DisableHtml does not cover.
        builder.DocumentProcessed += SanitizeGenericAttributes;
        builder.DocumentProcessed += UnwrapAbbreviationsInsideLinks;

        _pipeline = builder.Build();

        _yamlDeserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
            .Build();
    }

    public MarkdownParseResult Parse(
        string markdown,
        string? defaultTitle = null,
        string? filePath = null,
        string localePrefix = "",
        string versionPrefix = "",
        Localization? localization = null)
    {
        markdown = EscapeBracesInCodeSpans(markdown);
        var document = Markdown.Parse(markdown, _pipeline);

        var frontMatter = ParseFrontMatter(document, filePath);

        var headings = new List<HeadingInfo>();
        var usedIds = document.Descendants<HeadingBlock>().Select(h => h.GetAttributes().Id).OfType<string>().ToHashSet();
        foreach (var block in document.Descendants())
        {
            if (block is HeadingBlock heading)
            {
                var headingText = UnescapeBraces(ExtractInlineText(heading.Inline));
                if (string.IsNullOrEmpty(headingText))
                    continue;

                var attributes = heading.GetAttributes();
                // auto-identifiers ran on the escaped text, so a code span with braces left the placeholder in the id
                if (attributes.Id is { } autoId && (autoId.Contains("bark_lcb") || autoId.Contains("bark_rcb")))
                {
                    var slug = Slugify(headingText);
                    var unique = slug;
                    for (var n = 1; usedIds.Contains(unique); n++)
                        unique = $"{slug}-{n}";
                    usedIds.Add(unique);
                    attributes.Id = unique;
                }
                var id = attributes.Id ?? Slugify(headingText);
                headings.Add(new HeadingInfo(headingText, id, heading.Level));
            }
        }

        var html = UnescapeBraces(AddHeadingAnchors(document.ToHtml(_pipeline)));
        html = PrefixVersionLinks(html, versionPrefix);
        html = PrefixBodyContent(html, _basePath);
        html = RewriteAbbreviations(html);

        if (frontMatter?.Layout == "home")
            html = RenderHomePage(frontMatter, _basePath, localePrefix) + html;
        else if (frontMatter?.Layout == "help")
            html = RenderHelpPage(frontMatter, _basePath, localePrefix, localization ?? Localization.Default) + html;

        return new MarkdownParseResult(
            html,
            frontMatter?.Title ?? defaultTitle,
            frontMatter?.Description is { Length: > 0 } d ? ToPlainText(d) : null,
            headings,
            frontMatter?.Layout,
            frontMatter?.LastUpdated ?? true,
            frontMatter?.Keywords is { Count: > 0 } kw ? kw.AsReadOnly() : null,
            frontMatter?.Pagination ?? true,
            frontMatter?.Redirect,
            frontMatter?.Updated ?? frontMatter?.Date,
            frontMatter?.Toc ?? true,
            frontMatter?.Image,
            frontMatter?.MachineTranslated ?? false,
            frontMatter);
    }

    // Attributes `{...}` may set. Anything added here is contributor-writable: no handlers, no href/src.
    private static readonly HashSet<string> AllowedAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "id", "class", "title", "target", "rel", "width", "height", "align", "lang", "dir", "role",
    };

    private static void SanitizeGenericAttributes(MarkdownObject document)
    {
        foreach (var node in document.Descendants())
        {
            if (node.TryGetAttributes() is not { Properties: { Count: > 0 } properties })
                continue;
            properties.RemoveAll(p => !AllowedAttributes.Contains(p.Key));
        }
    }

    // abbr should skip link text
    private static void UnwrapAbbreviationsInsideLinks(MarkdownObject document)
    {
        foreach (var link in document.Descendants<LinkInline>())
        {
            foreach (var abbr in link.Descendants<AbbreviationInline>().ToList())
                abbr.ReplaceBy(new LiteralInline(abbr.Abbreviation.Label ?? string.Empty));
        }
    }

    private const string ArrowIcon =
        "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M5 12h14M13 6l6 6-6 6\"/></svg>";

    private const string ExternalIcon =
        "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M14 4h6v6M20 4l-9 9M18 14v5a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V7a1 1 0 0 1 1-1h5\"/></svg>";

    private const string ChevronIcon =
        "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M9 6l6 6-6 6\"/></svg>";

    private const string SearchIcon =
        "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" aria-hidden=\"true\"><circle cx=\"11\" cy=\"11\" r=\"7\"/><path d=\"M21 21l-4.3-4.3\"/></svg>";

    private static string RenderHomePage(FrontMatter frontMatter, string basePath, string localePrefix)
    {
        var hero = frontMatter.Hero;
        var isBanner = hero?.Variant == "banner";
        var sb = new StringBuilder();
        sb.Append(isBanner ? "<div class=\"bark-home bark-home--banner\">" : "<div class=\"bark-home\">");

        if (hero is not null)
        {
            var hasImageFile = !string.IsNullOrWhiteSpace(hero.Image)
                && (hero.Image.Contains('/') || hero.Image.StartsWith("http", StringComparison.OrdinalIgnoreCase));
            sb.Append(hasImageFile ? "<div class=\"bark-hero bark-hero--split\">" : "<div class=\"bark-hero\">");

            if (!string.IsNullOrWhiteSpace(hero.Image))
            {
                sb.Append("<div class=\"bark-hero-image\">")
                    .Append(hasImageFile
                        ? $"<img src=\"{WebUtility.HtmlEncode(PrefixInternalAsset(hero.Image, basePath))}\" alt=\"\">"
                        : WebUtility.HtmlEncode(hero.Image))
                    .Append("</div>");
            }

            sb.Append("<div class=\"bark-hero-body\">");
            // `name` is the first headline line, so `text` takes the <h1>; a name-only hero keeps it.
            var hasHeadline = !string.IsNullOrWhiteSpace(hero.Text);
            if (!string.IsNullOrWhiteSpace(hero.Name))
            {
                var tag = hasHeadline ? "p" : "h1";
                sb.Append($"<{tag} class=\"bark-hero-name\">").Append(WebUtility.HtmlEncode(hero.Name)).Append($"</{tag}>");
            }
            if (hasHeadline)
                sb.Append("<h1 class=\"bark-hero-text\">").Append(WebUtility.HtmlEncode(hero.Text)).Append("</h1>");
            if (!string.IsNullOrWhiteSpace(hero.Tagline))
                sb.Append("<p class=\"bark-hero-tagline\">").Append(WebUtility.HtmlEncode(hero.Tagline)).Append("</p>");

            if (hero.Actions is { Count: > 0 } actions)
            {
                sb.Append("<div class=\"bark-hero-actions\">");
                foreach (var action in actions)
                {
                    var theme = action.Theme is "alt" or "minimal" ? action.Theme : "brand";
                    var link = action.Link ?? "#";
                    var external = action.External ?? UrlPaths.IsExternal(link);
                    var icon = theme switch
                    {
                        "brand" => ArrowIcon,
                        "minimal" when external => ExternalIcon,
                        _ => "",
                    };
                    sb.Append("<a class=\"bark-hero-action ").Append(theme).Append("\" href=\"")
                        .Append(WebUtility.HtmlEncode(PrefixInternalLink(link, basePath, localePrefix))).Append('"')
                        .Append(external ? " target=\"_blank\" rel=\"noopener noreferrer\">" : ">")
                        .Append(WebUtility.HtmlEncode(action.Text ?? string.Empty)).Append(icon).Append("</a>");
                }
                sb.Append("</div>");
            }

            sb.Append("</div></div>");
        }

        if (frontMatter.Features is { Count: > 0 } features)
        {
            sb.Append("<div class=\"bark-features\">");
            foreach (var feature in features)
            {
                var hasLink = !string.IsNullOrWhiteSpace(feature.Link);
                sb.Append(hasLink
                    ? $"<a class=\"bark-feature\" href=\"{WebUtility.HtmlEncode(PrefixInternalLink(feature.Link!, basePath, localePrefix))}\">"
                    : "<div class=\"bark-feature\">");

                AppendFeatureIcon(sb, feature, basePath);
                if (!string.IsNullOrWhiteSpace(feature.Title))
                    sb.Append("<h2 class=\"bark-feature-title\">").Append(WebUtility.HtmlEncode(feature.Title)).Append("</h2>");
                if (!string.IsNullOrWhiteSpace(feature.Details))
                    sb.Append("<p class=\"bark-feature-details\">").Append(WebUtility.HtmlEncode(feature.Details)).Append("</p>");

                sb.Append(hasLink ? "</a>" : "</div>");
            }
            sb.Append("</div>");
        }

        sb.Append("</div>");
        return sb.ToString();
    }

    private string RenderHelpPage(FrontMatter frontMatter, string basePath, string localePrefix, Localization l)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"bark-help\"><div class=\"bark-help-hero\">");
        if (frontMatter.Hero?.Text is { Length: > 0 } heading)
            sb.Append("<h1 class=\"bark-help-title\">").Append(WebUtility.HtmlEncode(heading)).Append("</h1>");
        if (frontMatter.Hero?.Tagline is { Length: > 0 } tagline)
            sb.Append("<p class=\"bark-help-tagline\">").Append(WebUtility.HtmlEncode(tagline)).Append("</p>");
        sb.Append("<button type=\"button\" class=\"bark-help-search\" data-bark-search aria-haspopup=\"dialog\" aria-controls=\"search-modal\">")
            .Append(SearchIcon)
            .Append("<span class=\"bark-help-search-label\">").Append(WebUtility.HtmlEncode(l.HelpSearch)).Append("</span>")
            .Append("<kbd aria-hidden=\"true\">Ctrl K</kbd></button></div>");

        if (frontMatter.Features is { Count: > 0 } topics)
        {
            sb.Append("<section class=\"bark-help-section\" aria-labelledby=\"bark-help-topics\">")
                .Append("<h2 id=\"bark-help-topics\" class=\"bark-help-heading\">").Append(WebUtility.HtmlEncode(l.HelpTopics)).Append("</h2>")
                .Append("<ul class=\"bark-help-topics\">");
            foreach (var topic in topics.Where(t => !string.IsNullOrWhiteSpace(t.Title)))
            {
                var link = topic.Link;
                var section = string.IsNullOrWhiteSpace(link) || UrlPaths.IsExternal(link)
                    ? null
                    : LocaleRouting.Localize(localePrefix, link.Trim('/')).ToLowerInvariant();
                sb.Append("<li>")
                    .Append(link is { Length: > 0 }
                        ? $"<a class=\"bark-help-topic\" href=\"{WebUtility.HtmlEncode(PrefixInternalLink(link, basePath, localePrefix))}\">"
                        : "<div class=\"bark-help-topic\">")
                    .Append("<span class=\"bark-help-topic-body\"><span class=\"bark-help-topic-title\">").Append(WebUtility.HtmlEncode(topic.Title)).Append("</span>");
                if (!string.IsNullOrWhiteSpace(topic.Details))
                    sb.Append("<span class=\"bark-help-topic-details\">").Append(WebUtility.HtmlEncode(topic.Details)).Append("</span>");
                if (section is { Length: > 0 })
                    sb.Append("<span class=\"bark-help-count\" data-section=\"").Append(WebUtility.HtmlEncode(section))
                        .Append("\" data-one=\"").Append(WebUtility.HtmlEncode(l.HelpArticleCountOne))
                        .Append("\" data-many=\"").Append(WebUtility.HtmlEncode(l.HelpArticleCount)).Append("\"></span>");
                sb.Append("</span>").Append(link is { Length: > 0 } ? ChevronIcon + "</a>" : "</div>").Append("</li>");
            }
            sb.Append("</ul></section>");
        }

        if (frontMatter.Popular is { Count: > 0 } popular)
        {
            sb.Append("<section class=\"bark-help-section\" aria-labelledby=\"bark-help-popular\">")
                .Append("<h2 id=\"bark-help-popular\" class=\"bark-help-heading\">").Append(WebUtility.HtmlEncode(l.HelpPopular)).Append("</h2>")
                .Append("<ul class=\"bark-help-popular\">");
            foreach (var item in popular.Where(p => !string.IsNullOrWhiteSpace(p.Text) && !string.IsNullOrWhiteSpace(p.Link)))
                sb.Append("<li><a href=\"").Append(WebUtility.HtmlEncode(PrefixInternalLink(item.Link!, basePath, localePrefix))).Append("\">")
                    .Append(WebUtility.HtmlEncode(item.Text)).Append("</a></li>");
            sb.Append("</ul></section>");
        }

        if (frontMatter.Contact is { Length: > 0 } contact)
            sb.Append("<div class=\"bark-help-contact\">").Append(ToHtml(contact)).Append("</div>");

        return sb.Append("</div>").ToString();
    }

    private static void AppendFeatureIcon(StringBuilder sb, Models.FeatureFrontMatter feature, string basePath)
    {
        if (feature.IconImage is { } cfg)
        {
            var alt = WebUtility.HtmlEncode(cfg.Alt ?? "");
            sb.Append("<div class=\"bark-feature-icon\">");
            if (!string.IsNullOrEmpty(cfg.Light) && !string.IsNullOrEmpty(cfg.Dark))
            {
                sb.Append($"<img class=\"bark-icon-light\" src=\"{WebUtility.HtmlEncode(PrefixInternalAsset(cfg.Light, basePath))}\" alt=\"{alt}\" aria-hidden=\"true\">")
                  .Append($"<img class=\"bark-icon-dark\" src=\"{WebUtility.HtmlEncode(PrefixInternalAsset(cfg.Dark, basePath))}\" alt=\"{alt}\" aria-hidden=\"true\">");
            }
            else if (!string.IsNullOrEmpty(cfg.Src))
            {
                sb.Append($"<img src=\"{WebUtility.HtmlEncode(PrefixInternalAsset(cfg.Src, basePath))}\" alt=\"{alt}\" aria-hidden=\"true\">");
            }
            sb.Append("</div>");
            return;
        }

        if (string.IsNullOrWhiteSpace(feature.Icon))
            return;

        sb.Append("<div class=\"bark-feature-icon\">");
        var icon = feature.Icon.TrimStart();
        if (icon.StartsWith("<svg", StringComparison.OrdinalIgnoreCase))
            sb.Append(icon);
        else if (icon.StartsWith('/') || icon.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || icon.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            sb.Append($"<img src=\"{WebUtility.HtmlEncode(PrefixInternalAsset(icon, basePath))}\" alt=\"\" aria-hidden=\"true\">");
        else
            sb.Append(EscapeIconText(icon));
        sb.Append("</div>");
    }

    private static string AddHeadingAnchors(string html) =>
        HeadingRegex().Replace(html, match =>
        {
            var level = match.Groups[1].Value;
            var id = match.Groups[2].Value;
            var inner = match.Groups[3].Value;
            var plainText = TagRegex().Replace(inner, string.Empty);
            return $"<h{level} id=\"{id}\" tabindex=\"-1\">{inner} " +
                   $"<a class=\"header-anchor\" href=\"#{id}\" aria-label=\"{LayoutProvider.HtmlEncode(Localization.Default.PermalinkTo(plainText))}\">&#8203;</a></h{level}>";
        });

    [GeneratedRegex(@"<h([2-6]) id=""([^""]+)"">(.*?)</h\1>", RegexOptions.Singleline)]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"<[^>]*>")]
    private static partial Regex TagRegex();

    private FrontMatter? ParseFrontMatter(MarkdownDocument document, string? filePath = null)
    {
        if (document.FirstOrDefault() is not YamlFrontMatterBlock yamlBlock)
            return null;

        var yaml = UnescapeBraces(yamlBlock.Lines.ToString().Trim());
        if (string.IsNullOrWhiteSpace(yaml))
            return null;

        try
        {
            return _yamlDeserializer.Deserialize<FrontMatter>(yaml);
        }
        catch (YamlException ex)
        {
            _logger?.LogWarning(ex, "Malformed YAML frontmatter in {FilePath} — frontmatter will be ignored", filePath ?? "(unknown)");
            return null;
        }
    }

    private static string ExtractInlineText(ContainerInline? container)
    {
        if (container == null) return string.Empty;
        var sb = new StringBuilder();
        var inline = container.FirstChild;
        while (inline != null)
        {
            if (inline is LiteralInline lit)
                sb.Append(lit.Content);
            else if (inline is CodeInline code)
                sb.Append(code.Content);
            else if (inline is ContainerInline child)
                sb.Append(ExtractInlineText(child));
            inline = inline.NextSibling;
        }
        return sb.ToString().Trim();
    }

    public string ToHtml(string markdown) =>
        PrefixBodyContent(UnescapeBraces(Markdown.ToHtml(markdown, _pipeline)), _basePath);

    // The browser's own abbr tooltip is hover-only, so touch never sees it, and it cannot be styled or
    // suppressed. The expansion moves to data-tip for the CSS tooltip, tabindex lets a tap open it, and
    // the visually hidden copy carries it to screen readers, which read a title attribute inconsistently.
    private static string RewriteAbbreviations(string html) =>
        AbbrTagRegex().Replace(html, m =>
            $"<abbr tabindex=\"0\" data-tip=\"{m.Groups[1].Value}\">{m.Groups[2].Value}" +
            $"<span class=\"sr-only\"> ({m.Groups[1].Value})</span></abbr>");

    [GeneratedRegex(@"<abbr title=""([^""]*)"">(.*?)</abbr>", RegexOptions.Singleline)]
    private static partial Regex AbbrTagRegex();

    [GeneratedRegex(@"``[^`\n]*``|`[^`\n]*`")]
    private static partial Regex InlineCodeSpanRegex();

    private static string EscapeBracesInCodeSpans(string markdown) =>
        InlineCodeSpanRegex().Replace(markdown, m =>
            m.Value.Replace("{", BARK_LCB).Replace("}", BARK_RCB));

    private static string UnescapeBraces(string html) =>
        html.Replace(BARK_LCB, "{").Replace(BARK_RCB, "}");

    // Body content links/images: rewrite root-relative href/src to include basePath.
    private static string PrefixBodyContent(string html, string basePath)
    {
        if (string.IsNullOrEmpty(basePath))
            return html;

        return BodyContentUrlRegex().Replace(html, m =>
        {
            var url = m.Groups[2].Value;
            if (PrefixHasBasePath(url, basePath))
                return m.Value;
            return $"{m.Groups[1].Value}=\"{basePath}{url}\"";
        });
    }

    // Root-relative page links in a version page stay inside the version tree; file links such as /assets/x.pdf do not.
    private static string PrefixVersionLinks(string html, string versionPrefix) =>
        versionPrefix.Length == 0
            ? html
            : RootRelativeHrefRegex().Replace(html, m => Path.HasExtension(m.Groups[1].Value)
                ? m.Value
                : $"href=\"/{LocaleRouting.LocalizeInTree(versionPrefix, m.Groups[1].Value)}{m.Groups[2].Value}\"");

    [GeneratedRegex(@"href=""/(?!/)([^""#?]*)([^""]*)""")]
    private static partial Regex RootRelativeHrefRegex();

    [GeneratedRegex(@"(href|src)=""(/(?!/)[^""]*)""")]
    private static partial Regex BodyContentUrlRegex();

    // Frontmatter hero/feature links resolve against the page's own tree, so a bare "guide/x" stays inside /nl/.
    private static string PrefixInternalLink(string path, string basePath, string localePrefix = "")
    {
        if (path.Length == 0 || path.StartsWith('#') || path.StartsWith("//") || path.Contains("://", StringComparison.Ordinal)
            || path.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("tel:", StringComparison.OrdinalIgnoreCase))
            return path;

        if (path.StartsWith('/') && PrefixHasBasePath(path, basePath))
            return path;

        var trimmed = path.Trim('/');
        if (localePrefix.Length > 0)
            trimmed = LocaleRouting.Localize(localePrefix, trimmed);

        return trimmed.Length == 0 ? $"{basePath}/" : $"{basePath}/{trimmed}/";
    }

    private static string PrefixInternalAsset(string path, string basePath)
    {
        if (!path.StartsWith('/') || path.StartsWith("//"))
            return path;

        if (PrefixHasBasePath(path, basePath))
            return path;

        return $"{basePath}{path}";
    }

    // Strips inline markup so frontmatter descriptions are searchable and safe for <meta name="description">.
    private static readonly MarkdownPipeline _plainTextPipeline = new MarkdownPipelineBuilder().Build();

    public static string ToPlainText(string markdown)
    {
        var html = Markdown.ToHtml(markdown, _plainTextPipeline);
        return WebUtility.HtmlDecode(TagRegex().Replace(html, string.Empty)).Trim();
    }

    // Escapes only HTML-unsafe chars; leaves Unicode (including emoji) as raw UTF-8.
    private static string EscapeIconText(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static bool PrefixHasBasePath(string path, string basePath)
    {
        if (string.IsNullOrEmpty(basePath))
            return false;
        return path.StartsWith(basePath, StringComparison.Ordinal)
            && (path.Length == basePath.Length || path[basePath.Length] == '/');
    }

    public static string Slugify(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var builder = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_')
            {
                builder.Append(char.ToLowerInvariant(c));
            }
            else if (char.IsWhiteSpace(c) || c is '.' or ',' or '/' or '\\' or '+' or '~' or '#'
                     or '(' or ')' or '[' or ']' or '{' or '}' or '*' or '&' or '^' or '%'
                     or '$' or '!' or '@' or '`' or '\'' or '"' or ':' or ';' or '|'
                     or '<' or '>' or '?' or '=' or '~')
            {
                if (builder.Length > 0 && builder[^1] != '-')
                    builder.Append('-');
            }
        }

        while (builder.Length > 0 && builder[^1] == '-')
            builder.Length--;

        return builder.ToString();
    }
}
