using System.Text;
using Bark.Models;
using Bark.Services.Rendering;
using Bark.Services.Theming;

namespace Bark.Services.Layout;

public static partial class LayoutProvider
{
    public static string GetLayout(
        string title,
        string content,
        string navigationHtml,
        string? tocHtml,
        string breadcrumbHtml,
        string paginationHtml,
        string? themeCss = null,
        string? brandText = null,
        string? brandImage = null,
        bool enableDarkMode = true,
        string? footerHtml = null,
        string? socialLinksHtml = null,
        string? repoWidgetHtml = null,
        bool enableLiveReload = false,
        bool staticSearch = false,
        long buildVersion = 0,
        string? favicon = null,
        string? description = null,
        string? topNavHtml = null,
        string? mobileTopNavHtml = null,
        bool isHomePage = false,
        string? lastUpdatedHtml = null,
        string? editLinkHtml = null,
        bool showScrollIndicator = true,
        string basePath = "",
        string lang = "en",
        string? headTagsHtml = null,
        string? keywordsHtml = null,
        string? canonicalUrl = null,
        string? nonce = null,
        bool hasMath = false,
        bool hasMermaid = false,
        string? pageControlsHtml = null,
        string? rssDiscoveryHtml = null,
        string? promoBarHtml = null,
        string? socialMetaHtml = null,
        string? structuredDataHtml = null,
        IBarkTheme? theme = null,
        ThemeMode themeMode = ThemeMode.Auto,
        Localization? localization = null,
        string? localeSwitcherHtml = null,
        bool isRootLocale = true,
        string localePrefix = "")
    {
        var l = localization ?? Localization.Default;
        // "dark"/"light" in the theme value pins the scheme; no toggle to show, no client sync to run against it
        var toggleEnabled = enableDarkMode && themeMode == ThemeMode.Auto;
        var forcedThemeAttr = themeMode switch
        {
            ThemeMode.Dark => " data-theme=\"dark\"",
            ThemeMode.Light => " data-theme=\"light\"",
            _ => "",
        };
        var scrollIndicatorHtml = showScrollIndicator ? @"<div id=""scroll-indicator""></div>" : "";
        var faviconHtml = BuildFaviconLink(favicon, basePath);
        var homeHref = LocaleRouting.Localize(localePrefix, string.Empty) is { Length: > 0 } localeHome
            ? $"{basePath}/{localeHome}/"
            : basePath.Length == 0 ? "/" : $"{basePath}/";
        var brandImageSrc = ResolveAssetUrl(brandImage, basePath) is { } resolvedBrandImage
            ? AssetVersioning.Current.Apply(resolvedBrandImage)
            : null;
        var descriptionHtml = !string.IsNullOrWhiteSpace(description)
            ? $"<meta name=\"description\" content=\"{HtmlEncode(description)}\">"
            : "";

        var hasLeftNav = !string.IsNullOrWhiteSpace(navigationHtml);
        var layoutClass = isHomePage
            ? "layout bark-home-layout"
            : hasLeftNav ? "layout" : "layout no-left-sidebar";
        var mobileSocialHtml = !string.IsNullOrWhiteSpace(socialLinksHtml)
            ? $@"<div class=""sidebar-social-links"">{socialLinksHtml}</div>"
            : "";
        // Home declares "no sidebar"; the mobile drawer must honour that too, so it carries top-nav sections only.
        var drawerNavigationHtml = isHomePage ? "" : navigationHtml;
        var drawerHasContent = !string.IsNullOrWhiteSpace(drawerNavigationHtml)
            || !string.IsNullOrWhiteSpace(mobileTopNavHtml)
            || !string.IsNullOrWhiteSpace(mobileSocialHtml);
        var menuToggleHtml = drawerHasContent
            ? $@"<button type=""button"" class=""menu-toggle icon-btn"" id=""menu-toggle""
                    aria-expanded=""false"" aria-controls=""sidebar-left"" aria-label=""{HtmlEncode(l.NavToggle)}"">
                <svg viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" aria-hidden=""true"">
                    <path d=""M3 6h18M3 12h18M3 18h18"" stroke-linecap=""round""/>
                </svg>
            </button>"
            : "";
        var searchTriggerHtml = $@"<button type=""button"" class=""search-trigger"" id=""search-trigger""
                aria-haspopup=""dialog"" aria-controls=""search-modal"" aria-label=""{HtmlEncode(l.SearchAria)}"">
            <svg viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" aria-hidden=""true""><circle cx=""11"" cy=""11"" r=""7""/><path d=""M21 21l-4.3-4.3""/></svg>
            <span class=""search-trigger-label"">{HtmlEncode(l.SearchTrigger)}</span>
            <kbd class=""search-trigger-kbd"" id=""search-trigger-kbd"" aria-hidden=""true"">Ctrl K</kbd>
        </button>";
        var sidebarLeftHtml = $@"
        <aside class=""sidebar-left"" id=""sidebar-left"" tabindex=""-1"" aria-label=""{HtmlEncode(l.SidebarAria)}"">
            {mobileTopNavHtml}
            {drawerNavigationHtml}
            {mobileSocialHtml}
        </aside>";
        var breadcrumbAndTocHtml = isHomePage ? "" : $@"
            <nav class=""breadcrumb"" aria-label=""{HtmlEncode(l.BreadcrumbAria)}"">
                {breadcrumbHtml}
                {pageControlsHtml}
            </nav>
            {(tocHtml is null ? "" : $@"<details class=""toc-inline"">
                <summary>{HtmlEncode(l.TocMobileSummary)}</summary>
                <ul class=""toc-list"">
                    {tocHtml}
                </ul>
            </details>")}";
        var sidebarRightHtml = isHomePage || string.IsNullOrWhiteSpace(tocHtml)
            ? ""
            : $@"
        <div class=""toc-column"">
            <button type=""button"" class=""toc-collapse-toggle"" id=""toc-collapse-toggle"" aria-expanded=""true"" aria-controls=""sidebar-right""
                    data-label-hide=""{HtmlEncode(l.TocCollapse)}"" data-label-show=""{HtmlEncode(l.TocExpand)}""
                    aria-label=""{HtmlEncode(l.TocCollapse)}"" title=""{HtmlEncode(l.TocCollapse)}"">
                <svg viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2.2"" stroke-linecap=""round"" stroke-linejoin=""round"" class=""toc-chevron"" aria-hidden=""true""><path d=""M9 18l6-6-6-6""/></svg>
                <svg viewBox=""0 0 18 18"" fill=""none"" stroke=""currentColor"" stroke-width=""1.6"" stroke-linecap=""round"" class=""toc-glyph"" aria-hidden=""true""><path d=""M2.75 14.25H15.25""/><path d=""M2.75 3.75H15.25""/><path d=""M2.75 9H8.25""/></svg>
            </button>
            <nav class=""sidebar-right"" id=""sidebar-right"" aria-label=""{HtmlEncode(l.TocAria)}"">
                <h2 class=""toc-title"">
                    <svg viewBox=""0 0 18 18"" fill=""none"" stroke=""currentColor"" stroke-width=""1.6"" stroke-linecap=""round"" aria-hidden=""true""><path d=""M2.75 14.25H15.25""/><path d=""M2.75 3.75H15.25""/><path d=""M2.75 9H8.25""/></svg>
                    {HtmlEncode(l.TocTitle)}
                </h2>
                <div class=""toc-list-wrapper"">
                    <div class=""toc-indicator"" aria-hidden=""true""></div>
                    <ul class=""toc-list"">
                        {tocHtml}
                    </ul>
                </div>
            </nav>
        </div>";
        var contentClass = isHomePage ? "content bark-home-content" : "content";
        // Home pages never show "last updated" or prev/next pagination, regardless of caller input.
        var paginationBlock = isHomePage ? "" : paginationHtml;
        var lastUpdatedBlock = isHomePage ? "" : lastUpdatedHtml;
        var editLinkBlock = isHomePage ? "" : editLinkHtml;
        var pageMetaBlock = string.IsNullOrEmpty(editLinkBlock) && string.IsNullOrEmpty(lastUpdatedBlock)
            ? ""
            : $@"<div class=""page-meta""><div class=""page-meta-left"">{editLinkBlock}</div><div class=""page-meta-right"">{lastUpdatedBlock}</div></div>";

        var activeTheme = theme ?? ThemeRegistry.Default;
        var themeTokenCss = ThemeCssBuilder.BuildTokenCss(activeTheme, enableDarkMode);

        var nonceAttr = nonce is { Length: > 0 } ? $" nonce=\"{nonce}\"" : "";
        // Pre-<style> so no transition can fire; without a stored theme, data-theme stays unset so CSS follows live OS changes.
        var themeInitScript = toggleEnabled
            ? "<script" + nonceAttr + ">(function(){try{var t=localStorage.getItem('bark-theme');if(t==='dark'||t==='light'){var r=document.documentElement;r.setAttribute('data-theme',t);r.style.colorScheme=t;}}catch(e){}})();</script>"
            : "";

        var themeToggleHtml = toggleEnabled
            ? $@"<button type=""button"" class=""icon-btn theme-toggle"" id=""theme-toggle"" role=""switch"" aria-checked=""false"" aria-label=""{HtmlEncode(l.ThemeToggle)}"">
                <svg class=""icon-sun"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"" aria-hidden=""true""><circle cx=""12"" cy=""12"" r=""4""/><path d=""M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M4.93 19.07l1.41-1.41M17.66 6.34l1.41-1.41""/></svg>
                <svg class=""icon-moon"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"" aria-hidden=""true""><path d=""M21 12.79A9 9 0 1111.21 3 7 7 0 0021 12.79z""/></svg>
            </button>"
            : "";

        var colorSchemeMeta = themeMode switch
        {
            ThemeMode.Dark => "<meta name=\"color-scheme\" content=\"dark\">",
            ThemeMode.Light => "<meta name=\"color-scheme\" content=\"light\">",
            _ when enableDarkMode => "<meta name=\"color-scheme\" content=\"light dark\">",
            _ => "<meta name=\"color-scheme\" content=\"light\">",
        };

        var canonicalLink = canonicalUrl is { Length: > 0 }
            ? $"<link rel=\"canonical\" href=\"{HtmlEncode(canonicalUrl)}\">"
            : string.Empty;

        return CollapseBlankLines($@"
<!DOCTYPE html>
<html lang=""{HtmlEncode(lang)}"" class=""theme-{HtmlEncode(activeTheme.Name)}""{forcedThemeAttr}>
<head>
    <meta charset=""UTF-8"">
    {themeInitScript}
    {colorSchemeMeta}
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{HtmlEncode(title)}</title>
    {descriptionHtml}
    {keywordsHtml}
    {canonicalLink}
    {socialMetaHtml}
    {structuredDataHtml}
    {rssDiscoveryHtml}
    {faviconHtml}
    {headTagsHtml}
    {GetStylesLink(themeTokenCss, activeTheme.ComponentCss, basePath)}
    {themeCss}
    {(hasMath ? $"<link rel=\"stylesheet\" href=\"{basePath}/css/katex.min.css\">" : "")}
    {(hasMermaid ? $"<script defer src=\"{basePath}/js/mermaid.min.js\"></script>" : "")}
</head>
<body>
    <a href=""#main-content"" class=""skip-link"">{HtmlEncode(l.SkipToContent)}</a>
    {promoBarHtml}
    {scrollIndicatorHtml}
    <header class=""topbar"">
        <div class=""topbar-inner"">
            <div class=""topbar-left"">
                {menuToggleHtml}
                <div class=""brand""><a href=""{homeHref}"">{(brandImageSrc is not null ? $"<img src=\"{HtmlEncode(brandImageSrc)}\" alt=\"\">" : "")}{brandText ?? "Bark"}</a></div>
                {searchTriggerHtml}
            </div>
            {topNavHtml}
            <div class=""topbar-right"">
                <button type=""button"" class=""search-trigger-mobile icon-btn"" id=""search-trigger-mobile""
                        aria-haspopup=""dialog"" aria-controls=""search-modal"" aria-label=""{HtmlEncode(l.SearchAria)}"">
                    <svg viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" aria-hidden=""true""><circle cx=""11"" cy=""11"" r=""7""/><path d=""M21 21l-4.3-4.3""/></svg>
                </button>
                {socialLinksHtml}
                {repoWidgetHtml}
                {localeSwitcherHtml}
                {themeToggleHtml}
            </div>
        </div>
    </header>
    <div class=""search-overlay"" id=""search-overlay"" hidden>
        <div class=""search-modal"" id=""search-modal"" role=""dialog"" aria-modal=""true"" aria-labelledby=""search-modal-label"">
            <h2 id=""search-modal-label"" class=""sr-only"">{HtmlEncode(l.SearchHeading)}</h2>
            <div class=""search-modal-header"">
                <svg viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" aria-hidden=""true""><circle cx=""11"" cy=""11"" r=""7""/><path d=""M21 21l-4.3-4.3""/></svg>
                <input type=""search"" class=""search-modal-input"" id=""search-modal-input""
                       placeholder=""{HtmlEncode(l.SearchPlaceholder)}"" autocomplete=""off"" enterkeyhint=""search""
                       role=""combobox"" aria-expanded=""false"" aria-controls=""search-modal-results""
                       aria-autocomplete=""list"" aria-label=""Search documentation"">
                <button type=""button"" class=""search-modal-close icon-btn"" id=""search-modal-close"" aria-label=""{HtmlEncode(l.SearchClose)}"">
                    <svg viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" aria-hidden=""true""><path d=""M18 6L6 18M6 6l12 12""/></svg>
                </button>
            </div>
            <div class=""search-modal-results"" id=""search-modal-results"" role=""listbox"" aria-label=""{HtmlEncode(l.SearchResultsAria)}""></div>
            <div class=""sr-only"" id=""search-modal-status"" role=""status"" aria-live=""polite""></div>
            <ul class=""DocSearch-Commands"" aria-hidden=""true"">
                <li>
                    <kbd class=""DocSearch-Commands-Key""><svg width=""20"" height=""20"" aria-label=""{HtmlEncode(l.KeyArrowDown)}"" viewBox=""0 0 24 24"" role=""img""><g fill=""none"" stroke=""currentColor"" stroke-linecap=""round"" stroke-linejoin=""round"" stroke-width=""1.4""><path d=""M12 5v14""></path><path d=""m19 12-7 7-7-7""></path></g></svg></kbd>
                    <kbd class=""DocSearch-Commands-Key""><svg width=""20"" height=""20"" aria-label=""{HtmlEncode(l.KeyArrowUp)}"" viewBox=""0 0 24 24"" role=""img""><g fill=""none"" stroke=""currentColor"" stroke-linecap=""round"" stroke-linejoin=""round"" stroke-width=""1.4""><path d=""m5 12 7-7 7 7""></path><path d=""M12 19V5""></path></g></svg></kbd>
                    <span class=""DocSearch-Label"">{HtmlEncode(l.SearchNavigate)}</span>
                </li>
                <li>
                    <kbd class=""DocSearch-Commands-Key""><svg width=""20"" height=""20"" aria-label=""{HtmlEncode(l.KeyEnter)}"" viewBox=""0 0 24 24"" role=""img""><g fill=""none"" stroke=""currentColor"" stroke-linecap=""round"" stroke-linejoin=""round"" stroke-width=""1.4""><polyline points=""9 10 4 15 9 20""></polyline><path d=""M20 4v7a4 4 0 0 1-4 4H4""></path></g></svg></kbd>
                    <span class=""DocSearch-Label"">{HtmlEncode(l.SearchSelect)}</span>
                </li>
                <li>
                    <kbd class=""DocSearch-Commands-Key""><span class=""DocSearch-Escape-Key"">ESC</span></kbd>
                    <span aria-label=""{HtmlEncode(l.KeyEscape)}"" class=""DocSearch-Label"">{HtmlEncode(l.SearchEsc)}</span>
                </li>
            </ul>
        </div>
    </div>
    <div class=""sidebar-overlay"" id=""sidebar-overlay""></div>
    <div class=""{layoutClass}"">
        {sidebarLeftHtml}
        <div class=""content-shell"">
            <div class=""main-column"">
                <main class=""main-container"" id=""main-content"" tabindex=""-1"">
                    {breadcrumbAndTocHtml}
                    <article class=""{contentClass}"">
                        {content}
                        {pageMetaBlock}
                        {paginationBlock}
                    </article>
                </main>
                {(string.IsNullOrWhiteSpace(footerHtml) ? "" : $@"<footer class=""site-footer"">{footerHtml}</footer>")}
            </div>
            {sidebarRightHtml}
        </div>
    </div>
    {GetScriptsTag(enableLiveReload, toggleEnabled, buildVersion, basePath, staticSearch, l, isRootLocale)}
</body>
</html>");
    }

    public static string Get404Layout(Func<string?, string> htmlEncode, string basePath = "", string lang = "en", IBarkTheme? theme = null, ThemeMode themeMode = ThemeMode.Auto, Localization? localization = null, string localePrefix = "")
    {
        var l = localization ?? Localization.Default;
        var homeHref = LocaleRouting.Localize(localePrefix, string.Empty) is { Length: > 0 } localeHome
            ? $"{basePath}/{localeHome}/"
            : basePath.Length == 0 ? "/" : $"{basePath}/";
        var activeTheme = theme ?? ThemeRegistry.Default;
        // Built outside the interpolated block so JS/CSS braces don't need escaping.
        var darkVars = ThemeCssBuilder.BuildMinimalTokenCss(activeTheme);
        var lightVars = ThemeCssBuilder.BuildMinimalLightTokenCss(activeTheme);
        var forcedThemeAttr = themeMode switch
        {
            ThemeMode.Dark => " data-theme=\"dark\"",
            ThemeMode.Light => " data-theme=\"light\"",
            _ => "",
        };
        // forced mode: no client sync, the SSR data-theme attribute already pins it
        var themeInit = themeMode == ThemeMode.Auto
            ? "<script>(function(){" +
              "function apply(){try{var t=localStorage.getItem('bark-theme');var r=document.documentElement;" +
              "if(t==='dark'||t==='light'){r.setAttribute('data-theme',t);r.style.colorScheme=t;}" +
              "else{r.removeAttribute('data-theme');r.style.colorScheme='';}" +
              "}catch(e){}}" +
              "apply();" +
              "window.addEventListener('pageshow',function(e){if(e.persisted)apply();});" +
              "})()</script>"
            : "";
        var darkCss = "@media (prefers-color-scheme: dark) {" +
            ":root:not([data-theme=\"light\"]) {" + darkVars + "}" +
            "}" +
            ":root[data-theme=\"dark\"] {" + darkVars + "}";
        return $@"
<!DOCTYPE html>
<html lang=""{htmlEncode(lang)}""{forcedThemeAttr}>
<head>
    <meta charset=""UTF-8"">
    {themeInit}
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{htmlEncode(l.NotFoundTitle)}</title>
    <style>
        :root {{
{lightVars}            --font-sans: system-ui, -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, sans-serif;
        }}
        {darkCss}
        * {{ box-sizing: border-box; margin: 0; padding: 0; }}
        body {{
            font-family: var(--font-sans);
            background-color: var(--bg-color);
            color: var(--text-color);
            display: flex;
            align-items: center;
            justify-content: center;
            min-height: 100vh;
            line-height: 1.6;
        }}
        .not-found {{
            text-align: center;
        }}
        .not-found h1 {{
            font-size: 4rem;
            font-weight: 600;
            letter-spacing: -0.03em;
            margin-bottom: 0.5rem;
        }}
        .not-found p {{
            color: var(--text-muted);
            margin-bottom: 2rem;
        }}
        .not-found a {{
            color: var(--accent);
            text-decoration: none;
            font-weight: 500;
        }}
        .not-found a:hover {{
            text-decoration: underline;
        }}
    </style>
</head>
<body>
    <div class=""not-found"">
        <h1>404</h1>
        <p>{htmlEncode(l.NotFoundMessage)}</p>
        <a href=""{homeHref}"">{htmlEncode(l.NotFoundHome)}</a>
    </div>
</body>
</html>";
    }

    public static string HtmlEncode(string? value) =>
        value != null ? System.Net.WebUtility.HtmlEncode(value) : string.Empty;

    // collapses blank lines left by empty {placeholder}s, but never inside <article>...</article> where markdown code blocks can carry real blank lines
    private static string CollapseBlankLines(string html)
    {
        var start = html.IndexOf("<article", StringComparison.Ordinal);
        var end = html.IndexOf("</article>", StringComparison.Ordinal);
        if (start < 0 || end < 0)
            return CollapseBlankLinesRegex(html);
        end += "</article>".Length;
        return CollapseBlankLinesRegex(html[..start]) + html[start..end] + CollapseBlankLinesRegex(html[end..]);
    }

    private static string CollapseBlankLinesRegex(string s) =>
        System.Text.RegularExpressions.Regex.Replace(s, @"(\r?\n[ \t]*)+\r?\n", "\n");

    internal sealed record GeneratedAsset(string Key, string Body, string Version);

    // one mutable slot, not a dictionary keyed by build version, so long uptime with many hot-reloads can't leak
    private static GeneratedAsset GetOrBuildAsset(ref GeneratedAsset? slot, string key, Func<string> build)
    {
        var current = slot;
        if (current is not null && current.Key == key)
            return current;

        var next = BuildAsset(key, build);
        slot = next;
        return next;
    }

    // one slot per locale: bounded by the configured locales, so hot-reloads still cannot grow it
    private static GeneratedAsset GetOrBuildAsset(
        System.Collections.Concurrent.ConcurrentDictionary<string, GeneratedAsset> slots,
        string slot,
        string key,
        Func<string> build)
    {
        if (slots.TryGetValue(slot, out var current) && current.Key == key)
            return current;

        var next = BuildAsset(key, build);
        slots[slot] = next;
        return next;
    }

    private static GeneratedAsset BuildAsset(string key, Func<string> build)
    {
        var body = build();
        var version = Convert.ToHexStringLower(
            System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(body)))[..10];
        return new GeneratedAsset(key, body, version);
    }

    public static string? ResolveAssetUrl(string? url, string basePath)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return url;

        if (url.StartsWith('/'))
        {
            if (HasBasePathPrefix(url, basePath))
                return url;
            return $"{basePath}{url}";
        }

        return url;
    }

    private static bool HasBasePathPrefix(string path, string basePath)
    {
        if (string.IsNullOrEmpty(basePath))
            return false;
        return path.StartsWith(basePath, StringComparison.Ordinal)
            && (path.Length == basePath.Length || path[basePath.Length] == '/');
    }

    private static string BuildFaviconLink(string? favicon, string basePath = "")
    {
        if (string.IsNullOrWhiteSpace(favicon))
            return string.Empty;

        // Emoji or plain text fallback; not a URL or path
        if (!favicon.StartsWith('/')
            && !favicon.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !favicon.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            var svg = $"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100'><text y='.9em' font-size='90'>{favicon}</text></svg>";
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
            return $"<link rel=\"icon\" href=\"data:image/svg+xml;base64,{base64}\">";
        }

        var href = ResolveAssetUrl(favicon, basePath);
        return $"<link rel=\"icon\" href=\"{HtmlEncode(href)}\">";
    }
}
