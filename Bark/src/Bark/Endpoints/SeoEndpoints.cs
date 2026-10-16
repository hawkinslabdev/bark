using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.HttpResults;
using Bark.Configuration;
using Bark.Models;
using Bark.Services;
using Bark.Services.Rendering;

namespace Bark.Endpoints;

internal static partial class SeoEndpoints
{
    public static IEndpointRouteBuilder MapSeoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapMethods("/robots.txt", HttpVerbs.GetAndHead, GetRobots);
        app.MapMethods("/llms.txt", HttpVerbs.GetAndHead, GetLlms);
        app.MapMethods("/sitemap.xml", HttpVerbs.GetAndHead, GetSitemap);
        app.MapMethods("/feed.xml", HttpVerbs.GetAndHead, GetFeed).RequireRateLimiting(RateLimitPolicies.Search);
        return app;
    }

    internal static ContentHttpResult GetRobots(HttpContext context, PageRequestSettings settings)
    {
        var baseUrl = settings.Origin(context);
        var body = $"User-agent: *\nAllow: /\nSitemap: {baseUrl}{settings.BasePath}/sitemap.xml\n";
        return TypedResults.Text(body, "text/plain", Encoding.UTF8);
    }

    internal static async Task<ContentHttpResult> GetLlms(DocumentationService docs, PageRequestSettings settings, HttpContext context)
    {
        var basePath = settings.BasePath;
        var baseUrl = settings.Origin(context);
        var allPages = await docs.GetAllPagesAsync(context.RequestAborted);
        var config = docs.SiteConfig;
        var pages = allPages
            .Where(p => LocaleRouting.LocaleOf(p.Path, docs.LocaleCodes, docs.RootLocale) == docs.RootLocale)
            .ToList();
        var sb = new StringBuilder();
        sb.AppendLine($"# {config?.Brand ?? "Bark"}");
        sb.AppendLine();
        foreach (var page in pages.OrderBy(p => p.Path))
        {
            var url = page.Path == "index" ? $"{baseUrl}{basePath}/" : $"{baseUrl}{basePath}/{page.Path}/";
            var line = $"- [{page.Title}]({url})";
            if (!string.IsNullOrWhiteSpace(page.Description))
                line += $": {page.Description}";
            sb.AppendLine(line);
        }

        return TypedResults.Text(sb.ToString(), "text/plain", Encoding.UTF8);
    }

    internal static async Task<ContentHttpResult> GetSitemap(DocumentationService docs, PageRequestSettings settings, HttpContext context)
    {
        var basePath = settings.BasePath;
        var pages = await docs.GetAllPagesAsync(context.RequestAborted);
        var config = docs.SiteConfig;
        var localeCodes = docs.LocaleCodes;
        var rootLocale = docs.RootLocale;
        var pagePaths = pages.Select(p => p.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine(localeCodes.Count > 0
            ? "<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\" xmlns:xhtml=\"http://www.w3.org/1999/xhtml\">"
            : "<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
        sb.AppendLine($"  <url><loc>{UrlPaths.Href(basePath, "")}</loc><priority>1.0</priority></url>");

        // Older versions are noindex; the sitemap lists the latest version only.
        foreach (var page in pages.Where(p => !LocaleRouting.IsVersion(config, LocaleRouting.LocaleOf(p.Path, localeCodes, rootLocale))).OrderBy(p => p.Path))
        {
            if (page.Path == "index") continue;
            var lastMod = page.LastModified?.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
            sb.Append($"  <url><loc>{UrlPaths.Href(basePath, page.Path)}</loc><lastmod>{lastMod}</lastmod><priority>0.8</priority>");
            sb.Append(BuildAlternates(page.Path, localeCodes, rootLocale, pagePaths, config, basePath));
            sb.AppendLine("</url>");
        }

        sb.AppendLine("</urlset>");
        return TypedResults.Text(sb.ToString(), "application/xml", Encoding.UTF8);
    }

    private static string BuildAlternates(
        string path,
        IReadOnlyList<string> localeCodes,
        string rootLocale,
        HashSet<string> pagePaths,
        Config? config,
        string basePath)
    {
        if (localeCodes.Count == 0)
            return string.Empty;

        var pageLocale = LocaleRouting.LocaleOf(path, localeCodes, rootLocale);
        var tree = pageLocale == rootLocale ? string.Empty : pageLocale;
        var version = LocaleRouting.VersionPartOf(config, tree);
        var rootPath = LocaleRouting.Delocalize(tree, path);

        var sb = new StringBuilder();
        foreach (var locale in LocaleRouting.LocaleCodes(config).Prepend(string.Empty))
        {
            var candidate = LocaleRouting.Localize(LocaleRouting.TreeOf(locale, version), rootPath);
            if (!pagePaths.Contains(candidate))
                continue;

            var lang = LocaleRouting.LangOf(config, locale.Length == 0 ? rootLocale : locale);
            sb.Append($"<xhtml:link rel=\"alternate\" hreflang=\"{lang}\" href=\"{UrlPaths.Href(basePath, candidate)}\"/>");
        }

        return sb.ToString();
    }

    internal static async Task<ContentHttpResult> GetFeed(DocumentationService docs, PageRequestSettings settings, HttpContext context)
    {
        var basePath = settings.BasePath;
        var baseUrl = settings.Origin(context);
        var pages = await docs.GetAllPagesAsync(context.RequestAborted);
        var config = docs.SiteConfig;

        var feedTitle = WebUtility.HtmlEncode(config?.Brand ?? config?.Title ?? "Bark");
        var feedDesc = WebUtility.HtmlEncode(config?.Description ?? feedTitle);
        var feedLink = $"{baseUrl}{basePath}/";
        var feedLang = WebUtility.HtmlEncode(config?.Lang ?? "en");

        var recentPages = pages
            .Where(p => p.Layout is not ("home" or "help") && !LocaleRouting.IsVersion(config, LocaleRouting.LocaleOf(p.Path, docs.LocaleCodes, docs.RootLocale)))
            .OrderByDescending(p => p.LastModified ?? DateTime.MinValue)
            .Take(20)
            .ToList();

        var lastBuildDate = recentPages.Count > 0 && recentPages[0].LastModified.HasValue
            ? recentPages[0].LastModified!.Value.ToUniversalTime().ToString("R")
            : DateTime.UtcNow.ToString("R");

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<rss version=\"2.0\" xmlns:atom=\"http://www.w3.org/2005/Atom\">");
        sb.AppendLine("  <channel>");
        sb.AppendLine($"    <title>{feedTitle}</title>");
        sb.AppendLine($"    <link>{feedLink}</link>");
        sb.AppendLine($"    <description>{feedDesc}</description>");
        sb.AppendLine($"    <language>{feedLang}</language>");
        sb.AppendLine($"    <lastBuildDate>{lastBuildDate}</lastBuildDate>");
        sb.AppendLine($"    <atom:link href=\"{WebUtility.HtmlEncode($"{baseUrl}{basePath}/feed.xml")}\" rel=\"self\" type=\"application/rss+xml\"/>");

        foreach (var page in recentPages)
        {
            var pageUrl = page.Path == "index"
                ? $"{baseUrl}{basePath}/"
                : $"{baseUrl}{basePath}/{page.Path}/";
            var title = WebUtility.HtmlEncode(page.Title);
            var excerpt = page.Description is { Length: > 0 }
                ? WebUtility.HtmlEncode(page.Description)
                : WebUtility.HtmlEncode(PlainTextExcerpt(page.HtmlContent, 200));

            sb.AppendLine("    <item>");
            sb.AppendLine($"      <title>{title}</title>");
            sb.AppendLine($"      <link>{WebUtility.HtmlEncode(pageUrl)}</link>");
            sb.AppendLine($"      <guid isPermaLink=\"true\">{WebUtility.HtmlEncode(pageUrl)}</guid>");
            if (!string.IsNullOrEmpty(excerpt))
                sb.AppendLine($"      <description>{excerpt}</description>");
            if (page.LastModified.HasValue)
                sb.AppendLine($"      <pubDate>{page.LastModified.Value.ToUniversalTime():R}</pubDate>");
            sb.AppendLine("    </item>");
        }

        sb.AppendLine("  </channel>");
        sb.AppendLine("</rss>");
        return TypedResults.Text(sb.ToString(), "application/rss+xml", Encoding.UTF8);
    }

    [GeneratedRegex("<[^>]*>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    internal static string PlainTextExcerpt(string html, int maxLength)
    {
        var text = HtmlTagRegex().Replace(html, " ");
        text = WebUtility.HtmlDecode(WhitespaceRegex().Replace(text, " ").Trim());
        return text.Length <= maxLength ? text : text[..maxLength].TrimEnd() + "…";
    }
}
