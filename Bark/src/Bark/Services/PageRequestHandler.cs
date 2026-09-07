using System.Net;
using System.Security.Cryptography;
using System.Text;
using Bark.Configuration;
using Bark.Models;
using Bark.Services.Extensions;
using Bark.Services.Layout;
using Bark.Services.Rendering;
using Bark.Services.Theming;

namespace Bark.Services;

/// <summary>Startup-computed settings the catch-all page route needs on every request</summary>
public sealed record PageRequestSettings(
    string BasePath,
    string? CustomCsp,
    string ThemeDir,
    string WebRootPath,
    string DocsRootAbsolute,
    string? PublicBaseUrl,
    string? CliTheme = null)
{
    /// <summary>Blank means absent. An empty setting must not count as "configured" and mask a later source.</summary>
    public static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().TrimEnd('/');

    /// <summary>Precedence: <c>--base-url</c>, then <c>Docs:PublicBaseUrl</c>, then the bare <c>PublicBaseUrl</c> alias.</summary>
    public static string? ResolvePublicBaseUrl(string? cliBaseUrl, string? docsOption, string? alias) =>
        Normalize(cliBaseUrl) ?? Normalize(docsOption) ?? Normalize(alias);

    /// <summary>
    /// Absolute origin for canonical URLs, feeds and sitemaps; <c>PublicBaseUrl</c> wins because the Host header is caller-supplied and unfiltered unless <c>AllowedHosts</c> is set.
    /// </summary>
    public string Origin(HttpContext context) =>
        Normalize(PublicBaseUrl) ?? $"{context.Request.Scheme}://{context.Request.Host}";
}

/// <summary>Handles the catch-all documentation page route; lookup, ETag/304, CSP nonce, layout assembly</summary>
public sealed class PageRequestHandler
{
    private readonly DocumentationService _docs;
    private readonly MarkdownService _markdown;
    private readonly DocsOptions _docsOptions;
    private readonly ThemeOptions _themeOptions;
    private readonly PageRequestSettings _settings;
    private readonly ILogger<PageRequestHandler> _logger;
    private readonly string _iconsDir;
    private readonly string? _fallbackIconsDir;

    public PageRequestHandler(
        DocumentationService docs,
        MarkdownService markdown,
        DocsOptions docsOptions,
        ThemeOptions themeOptions,
        PageRequestSettings settings,
        ILogger<PageRequestHandler> logger)
    {
        _docs = docs;
        _markdown = markdown;
        _docsOptions = docsOptions;
        _themeOptions = themeOptions;
        _settings = settings;
        _logger = logger;
        _iconsDir = Path.Combine(settings.WebRootPath, "icons");
        var defaultIconsDir = Path.Combine(AppContext.BaseDirectory, "wwwroot-default", "icons");
        _fallbackIconsDir = Directory.Exists(defaultIconsDir) ? defaultIconsDir : null;
    }

    private static string? ResolveSocialImage(string? image, string origin, string basePath)
    {
        if (string.IsNullOrWhiteSpace(image))
            return null;
        if (image.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || image.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return image;
        var path = image.StartsWith('/') ? image : "/" + image;
        return $"{origin}{basePath}{path}";
    }

    internal static string ComputeETag(string origin, long buildVersion, string html)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(Encoding.UTF8.GetBytes($"{CspNonce.ProcessSalt}:{buildVersion}:{origin}:"));
        hash.AppendData(Encoding.UTF8.GetBytes(html));
        return Convert.ToBase64String(hash.GetHashAndReset()).TrimEnd('=');
    }

    /// <summary>Absolute front-matter redirects need the target host declared in <c>config.json</c>'s <c>redirectHosts</c>; anything else is an open redirect on the docs domain.</summary>
    internal static bool IsAllowedRedirectHost(string target, string requestHost, IReadOnlyList<string>? allowedHosts)
    {
        if (!Uri.TryCreate(target, UriKind.Absolute, out var uri))
            return false;

        if (uri.Host.Equals(requestHost, StringComparison.OrdinalIgnoreCase))
            return true;

        return allowedHosts is { Count: > 0 }
            && allowedHosts.Any(h => uri.Host.Equals(h.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    internal static string ExpandFooterVariables(string footer, string brand, string? title) =>
        footer
            .Replace("{year}", DateTime.UtcNow.Year.ToString())
            .Replace("{brand}", brand)
            .Replace("{title}", title ?? string.Empty);

    public async Task HandleAsync(string? path, HttpContext context)
    {
        var basePath = _settings.BasePath;

        var isRootRequest = path == null || path == "" || path == "/";
        if (isRootRequest)
            path = _docsOptions.DefaultPage ?? "index";

        path = (path ?? "").Trim('/');

        var config = _docs.SiteConfig;

        // Per request, not at startup: config.json hot-reloads, appsettings does not.
        var (themeName, themeMode) = ThemeSelection.Split(_settings.CliTheme ?? _themeOptions.Name ?? config?.Theme);
        var theme = ThemeRegistry.Resolve(themeName);

        var route = LocaleRouting.Resolve(config, path);
        var l = _docs.Locales.For(route.Code);
        var htmlLang = LocaleRouting.LangOf(config, route.Code);
        path = route.ContentPath;

        var page = await _docs.GetPageAsync(route.ContentPath, context.RequestAborted);
        var servedFallback = false;
        if (page == null && route.FallbackPath is { Length: > 0 } fallbackPath)
        {
            page = await _docs.GetPageAsync(fallbackPath, context.RequestAborted);
            servedFallback = page != null;
        }

        if (page == null && isRootRequest)
            page = await BuildSafeRootPage(_docs, basePath, l, context.RequestAborted);

        if (page == null)
        {
            context.Response.StatusCode = 404;
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(LayoutProvider.Get404Layout(LayoutProvider.HtmlEncode, basePath, htmlLang, theme, themeMode, l, route.Prefix));
            return;
        }

        if (page.Redirect is { Length: > 0 } redirectTarget)
        {
            var isAbsolute = redirectTarget.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || redirectTarget.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

            if (isAbsolute && !IsAllowedRedirectHost(redirectTarget, context.Request.Host.Host, config?.RedirectHosts))
            {
                _logger.LogWarning(
                    "Page {Page} redirects to {Target}, whose host is not listed in config.json redirectHosts; redirect ignored",
                    page.Path, redirectTarget);
            }
            else
            {
                string resolvedRedirect;
                if (isAbsolute)
                {
                    resolvedRedirect = redirectTarget;
                }
                else
                {
                    var trimmed = LocaleRouting.Localize(route.Prefix, redirectTarget);
                    resolvedRedirect = trimmed.Length == 0
                        ? (basePath.Length == 0 ? "/" : $"{basePath}/")
                        : (basePath.Length == 0 ? $"/{trimmed}/" : $"{basePath}/{trimmed}/");
                }

                context.Response.Redirect(resolvedRedirect, permanent: false);
                return;
            }
        }

        // Folds BuildVersion, CspNonce.ProcessSalt, and Origin into the ETag to invalidate the cache on edits, process restarts, or host changes.
        var origin = _settings.Origin(context);
        var etag = ComputeETag(origin, _docs.BuildVersion, page.HtmlContent);
        context.Response.Headers.ETag = $"\"{etag}\"";
        context.Response.Headers.CacheControl = "no-cache";
        if (_settings.PublicBaseUrl is null)
            context.Response.Headers.Vary = "Host";

        var extensions = _docs.Extensions;

        var nonce = CspNonce.Derive(etag);
        var hasMermaid = page.HtmlContent.Contains("class=\"mermaid\"", StringComparison.Ordinal);
        var baseCsp = SecurityHeaders.WithExtraSources(_settings.CustomCsp ?? SecurityHeaders.DefaultCsp, extensions.CspSources);
        var pageCsp = SecurityHeaders.BuildNonceCsp(baseCsp, nonce);
        context.Response.Headers.ContentSecurityPolicy = hasMermaid
            ? SecurityHeaders.WithInlineStyleElements(pageCsp)
            : pageCsp;

        if (context.Request.Headers.IfNoneMatch.ToString() == $"\"{etag}\"")
        {
            context.Response.StatusCode = 304;
            return;
        }

        var nav = await _docs.GetNavigationAsync(route.Code, context.RequestAborted);
        var navHtml = NavigationHtmlRenderer.BuildNavigationHtml(nav, path, config, basePath, route.Prefix, l);
        var topNavHtml = NavigationHtmlRenderer.BuildTopNavHtml(config?.TopNav, path, basePath, l, route.Prefix);
        var mobileTopNavHtml = NavigationHtmlRenderer.BuildMobileTopNavHtml(config?.TopNav, path, basePath, l, route.Prefix);

        var tocHtml = page.ShowToc ? TocHtmlRenderer.BuildTocHtml(page.Headings) : null;

        var crumbs = await _docs.GetBreadcrumbsAsync(path, l, context.RequestAborted);
        var breadcrumbHtml = BreadcrumbHtmlRenderer.BuildBreadcrumbHtml(crumbs, page.Title, basePath);

        var isHomePage = page.Layout == "home";
        var paginationHtml = string.Empty;
        if (!isHomePage && page.ShowPagination)
        {
            var indexPath = LocaleRouting.Localize(route.Prefix, "index");
            var orderedPaths = NavigationHtmlRenderer.GetOrderedPaths(nav, config, path, route.Prefix)
                .Where(p => p != null && p != "index" && p != indexPath)
                .ToList();
            var currentIndex = orderedPaths.IndexOf(path);
            string? prevPath = currentIndex > 0 ? orderedPaths[currentIndex - 1] : null;
            string? nextPath = currentIndex < orderedPaths.Count - 1 ? orderedPaths[currentIndex + 1] : null;
            string? prevTitle = prevPath != null ? (await ResolveInLocaleAsync(prevPath, route.Prefix, context.RequestAborted))?.Title : null;
            string? nextTitle = nextPath != null ? (await ResolveInLocaleAsync(nextPath, route.Prefix, context.RequestAborted))?.Title : null;

            paginationHtml = PaginationHtmlRenderer.BuildPaginationHtml(prevTitle, prevPath, nextTitle, nextPath, basePath, l);
        }

        var themeCss = ThemeProvider.BuildThemeCss(_themeOptions, nonce);
        var customCssLink = ThemeProvider.BuildCustomCssLink(_themeOptions, _settings.ThemeDir, basePath);
        var customJsScript = ThemeProvider.BuildCustomJsScript(_themeOptions, _settings.ThemeDir, basePath);
        var brandText = l.Label(config?.Brand ?? config?.Title ?? ThemeProvider.GetBrandText(_themeOptions));
        var brandImage = config?.BrandImage;
        var combinedThemeCss = themeCss + customCssLink + customJsScript;

        var socialLinksHtml = await SocialLinksHtmlRenderer.BuildSocialLinksHtmlAsync(config?.SocialLinks, _iconsDir, _fallbackIconsDir, l);
        var footerHtml = config?.Footer is { } footer
            ? $"<div class=\"content-footer\">{_markdown.ToHtml(ExpandFooterVariables(l.Label(footer), brandText, config?.Title))}</div>"
            : string.Empty;

        var lastUpdatedHtml = !isHomePage && config?.LastUpdated == true && page.ShowLastUpdated && page.LastModified is { } lastModified
            ? $"<div class=\"last-updated\">{LayoutProvider.HtmlEncode(l.LastUpdated)} {lastModified:yyyy-MM-dd}</div>"
            : string.Empty;

        const string editLinkIcon = "<svg class=\"edit-link-icon\" viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" fill=\"currentColor\" aria-hidden=\"true\">" +
            "<path d=\"M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zM20.71 7.04c.39-.39.39-1.02 0-1.41l-2.34-2.34c-.39-.39-1.02-.39-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z\"/></svg>";

        var editPath = page.OriginalRelativePath ?? $"{page.Path}.md";
        var encodedEditPath = string.Join("/", editPath.Split('/').Select(Uri.EscapeDataString));
        var editLinkHtml = !isHomePage && config?.EditLink is { Pattern: { Length: > 0 } pattern } editLink
            ? $"<a class=\"edit-link\" href=\"{LayoutProvider.HtmlEncode(pattern.Replace(":path", encodedEditPath))}\" " +
              $"target=\"_blank\" rel=\"noopener noreferrer nofollow\">{editLinkIcon}{LayoutProvider.HtmlEncode(l.Label(editLink.Text))}</a>"
            : string.Empty;

        var keywordsHtml = page.Keywords is { Count: > 0 } kw
            ? $"<meta name=\"keywords\" content=\"{LayoutProvider.HtmlEncode(string.Join(", ", kw.Take(20)))}\">"
            : string.Empty;

        var pageControlsEditIcon = !isHomePage && config?.PageControls?.EditLink?.Icon is { Length: > 0 } pcIconName
            ? await IconProvider.InlineSvgAsync(pcIconName, _iconsDir, _fallbackIconsDir)
            : null;
        var pageControlsHtml = !isHomePage
            ? PageControlsHtmlRenderer.BuildPageControlsHtml(page, config?.PageControls, config?.EditLink, basePath, _settings.DocsRootAbsolute, pageControlsEditIcon, l)
            : string.Empty;

        var promoBarHtml = config?.Promo is { Length: > 0 } promoSource
            ? PromoBarHtmlRenderer.BuildPromoBarHtml(_markdown.ToHtml(l.Label(promoSource)), promoSource, nonce, l)
            : string.Empty;

        var feedUrl = $"{origin}{basePath}/feed.xml";
        var rssDiscoveryHtml = $"<link rel=\"alternate\" type=\"application/rss+xml\" title=\"{LayoutProvider.HtmlEncode(config?.Brand ?? config?.Title ?? "RSS Feed")}\" href=\"{LayoutProvider.HtmlEncode(feedUrl)}\">";

        var pageSegment = page.Path == "index" ? string.Empty : $"{page.Path}/";
        var rawPath = $"{basePath}/{pageSegment}".TrimStart('/');
        var canonicalUrl = $"{origin}/{rawPath}";

        var metaDescription = string.IsNullOrEmpty(page.Description) ? config?.Description : page.Description;
        var socialImageUrl = ResolveSocialImage(page.Image ?? config?.Image ?? config?.BrandImage, origin, basePath);
        var siteName = l.Label(config?.Brand ?? config?.Title);
        var modified = isHomePage ? null : page.LastModified;

        var socialMetaHtml = SocialMetaRenderer.BuildSocialMeta(
            canonicalUrl, page.Title, metaDescription, isHomePage, socialImageUrl, siteName, htmlLang, modified);
        var structuredDataHtml = StructuredDataRenderer.BuildJsonLd(
            canonicalUrl, page.Title, metaDescription, isHomePage, origin, basePath, crumbs, socialImageUrl, siteName, modified, nonce);

        var originalHref = route.FallbackPath is { Length: > 0 } original
            ? UrlPaths.Href(basePath, original)
            : null;

        var translatedOriginal = !servedFallback && route.Prefix.Length > 0 && route.FallbackPath is { Length: > 0 } sourcePath
            ? await _docs.GetPageAsync(sourcePath, context.RequestAborted)
            : null;

        var noticeHtml = isHomePage
            ? string.Empty
            : servedFallback && originalHref is not null
            ? TranslationNoticeRenderer.Missing(l, originalHref)
            : page.MachineTranslated && originalHref is not null
            ? TranslationNoticeRenderer.Machine(l, originalHref)
            : translatedOriginal is { LastModified: { } sourceModified }
              && page.LastModified is { } translationModified
              && sourceModified > translationModified
              && originalHref is not null
                ? TranslationNoticeRenderer.Stale(l, originalHref)
                : string.Empty;

        var localeSwitcherHtml = LocaleSwitcherRenderer.Build(
            config, route.Code, route.ContentPath, basePath, l);

        var fullHtml = LayoutProvider.GetLayout(
            title: PageTitleRenderer.ComputeTitle(page.Title, config, l),
            content: noticeHtml + page.HtmlContent,
            navigationHtml: navHtml,
            topNavHtml: topNavHtml,
            mobileTopNavHtml: mobileTopNavHtml,
            tocHtml: tocHtml,
            breadcrumbHtml: breadcrumbHtml,
            paginationHtml: paginationHtml,
            themeCss: combinedThemeCss,
            brandText: brandText,
            brandImage: brandImage,
            enableDarkMode: ThemeProvider.UseDarkMode(_themeOptions),
            showScrollIndicator: ThemeProvider.ShowScrollIndicator(_themeOptions),
            footerHtml: footerHtml,
            socialLinksHtml: socialLinksHtml,
            enableLiveReload: _docsOptions.EnableHotReload,
            staticSearch: _docsOptions.IsStaticExport,
            buildVersion: _docs.BuildVersion,
            favicon: config?.Favicon,
            description: metaDescription,
            isHomePage: isHomePage,
            lastUpdatedHtml: lastUpdatedHtml,
            editLinkHtml: editLinkHtml,
            basePath: basePath,
            lang: htmlLang,
            headTagsHtml: HeadTagHtmlRenderer.BuildHeadTagsHtml(config?.Head) + ExtensionHeadRenderer.Build(extensions, nonce),
            keywordsHtml: keywordsHtml,
            canonicalUrl: canonicalUrl,
            nonce: nonce,
            hasMath: page.HtmlContent.Contains("class=\"katex\"", StringComparison.Ordinal),
            hasMermaid: hasMermaid,
            pageControlsHtml: pageControlsHtml,
            rssDiscoveryHtml: rssDiscoveryHtml,
            promoBarHtml: promoBarHtml,
            socialMetaHtml: socialMetaHtml,
            structuredDataHtml: structuredDataHtml,
            theme: theme,
            themeMode: themeMode,
            localization: l,
            localeSwitcherHtml: localeSwitcherHtml,
            isRootLocale: route.Prefix.Length == 0,
            localePrefix: route.Prefix
        );

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(fullHtml);
    }

    private async Task<DocumentationPage?> ResolveInLocaleAsync(string path, string localePrefix, CancellationToken cancellationToken)
    {
        var page = await _docs.GetPageAsync(path, cancellationToken);
        if (page is not null || localePrefix.Length == 0)
            return page;

        return await _docs.GetPageAsync(LocaleRouting.Delocalize(localePrefix, path), cancellationToken);
    }

    private static async Task<DocumentationPage> BuildSafeRootPage(DocumentationService docs, string basePath, Localization l, CancellationToken cancellationToken)
    {
        var pages = await docs.GetAllPagesAsync(cancellationToken);
        var linksHtml = pages.Count > 0
            ? $"<p>{l.SetupExistingPages}</p><ul>" + string.Join("", pages.OrderBy(p => p.Path)
                .Select(p => $"<li><a href=\"{UrlPaths.Href(basePath, p.Path)}\">{LayoutProvider.HtmlEncode(p.Title)}</a></li>")) + "</ul>"
            : $"<p>{l.SetupNoFiles}</p>";

        var html = $"""
            <h1>
                {l.SetupHeading}
            </h1>
            <p>
                {l.SetupBody}
            </p>
            {linksHtml}
            """;

        return new DocumentationPage(
            Path: "index",
            Title: "Let's set up your homepage",
            HtmlContent: html,
            Description: null,
            LastModified: null,
            Headings: []
        );
    }
}
