using System.Text;
using Bark.Models;
using Bark.Services.Layout;

namespace Bark.Services.Rendering;

public static class NavigationHtmlRenderer
{
    internal const string ExternalLinkIcon = "<svg class=\"external-link-icon\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" " +
        "stroke-width=\"2\" aria-hidden=\"true\"><path d=\"M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>" +
        "<path d=\"M15 3h6v6\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/><path d=\"M10 14 21 3\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/></svg>";

    private const string CaretSvg = "<span class=\"caret-icon\"><svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" " +
        "stroke-width=\"2\" aria-hidden=\"true\"><path d=\"M9 6l6 6-6 6\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/></svg></span>";
    internal const string ExternalLinkRel = " target=\"_blank\" rel=\"noopener noreferrer\"";

    internal static string LocalizeLink(string localePrefix, string path)
    {
        if (localePrefix.Length == 0 || path.Length == 0 || path.StartsWith('#') || UrlPaths.IsExternal(path))
            return path;

        return LocaleRouting.Localize(localePrefix, path);
    }

    public static string BuildNavigationHtml(NavigationNode node, string currentPath, Config? config, string basePath, string localePrefix = "", Localization? localization = null)
    {
        var l = localization ?? Localization.Default;
        var configPath = LocaleRouting.Delocalize(localePrefix, currentPath);

        if (config?.Sidebar is { Count: > 0 } sidebars)
        {
            var matchedSections = SidebarResolver.Resolve(sidebars, configPath);
            if (matchedSections is not null)
                return BuildNavFromConfig(matchedSections, configPath, basePath, localePrefix, l);
        }

        if (config?.Nav is { Count: > 0 } sections)
            return BuildNavFromConfig(sections, configPath, basePath, localePrefix, l);

        if (node.Children.Count == 0) return string.Empty;

        var html = new StringBuilder();
        html.AppendLine("<div class=\"nav-groups\">");

        foreach (var child in node.Children)
        {
            if (child.Children.Count > 0)
            {
                html.AppendLine("<div class=\"nav-group\">");
                var displayTitle = ToDisplayName(child.Title);
                html.AppendLine($"<div class=\"nav-group-title\">{LayoutProvider.HtmlEncode(displayTitle)}</div>");
                html.AppendLine("<ul class=\"nav-list\">");
                foreach (var sub in child.Children.OrderBy(c => c.Title))
                {
                    var isActive = sub.Path == currentPath;
                    html.AppendLine($"<li class=\"nav-item{(isActive ? " active" : "")}\">");
                    html.AppendLine($"<a href=\"{UrlPaths.Href(basePath, sub.Path ?? "")}\">{LayoutProvider.HtmlEncode(sub.Title)}</a>");
                    html.AppendLine("</li>");
                }
                html.AppendLine("</ul>");
                html.AppendLine("</div>");
            }
        }

        html.AppendLine("</div>");
        return html.ToString();
    }

    public static string BuildNavFromConfig(List<NavEntry> entries, string currentPath, string basePath, string localePrefix = "", Localization? localization = null)
    {
        var l = localization ?? Localization.Default;
        var html = new StringBuilder();
        html.AppendLine("<div class=\"sidebar-tree\">");
        foreach (var entry in entries)
            AppendSidebarEntry(html, entry, currentPath, level: 0, basePath, localePrefix, l);
        html.AppendLine("</div>");
        return html.ToString();
    }

    public static bool SidebarPathMatches(string entryPath, string currentPath)
    {
        // An external target is never "the current page", so it can't highlight or auto-expand its group.
        if (UrlPaths.IsExternal(entryPath)) return false;

        var normalized = entryPath.Trim('/').ToLowerInvariant();
        return normalized == currentPath || (normalized.Length == 0 && currentPath == "index");
    }

    public static bool ContainsActiveDescendant(NavEntry entry, string currentPath)
    {
        if (entry.Path is not null && SidebarPathMatches(entry.Path, currentPath))
            return true;

        return entry.Items?.Any(child => ContainsActiveDescendant(child, currentPath)) ?? false;
    }

    public static void AppendSidebarEntry(StringBuilder html, NavEntry entry, string currentPath, int level, string basePath, string localePrefix = "", Localization? localization = null)
    {
        var l = localization ?? Localization.Default;
        if (entry.Items is not { Count: > 0 } children)
        {
            var path = entry.Path ?? string.Empty;
            var isExternal = UrlPaths.IsExternal(path);
            var isActive = SidebarPathMatches(path, currentPath);
            var href = isExternal ? LayoutProvider.HtmlEncode(path) : UrlPaths.Href(basePath, LocalizeLink(localePrefix, path));
            html.AppendLine(
                $"<div class=\"sidebar-link level-{level}{(isActive ? " is-active" : "")}\">" +
                $"<a href=\"{href}\"{(isExternal ? ExternalLinkRel : "")}>" +
                $"{LayoutProvider.HtmlEncode(l.Label(entry.Title))}{(isExternal ? ExternalLinkIcon : "")}</a></div>");
            return;
        }

        if (string.IsNullOrWhiteSpace(entry.Title))
        {
            html.AppendLine("<div class=\"sidebar-group no-caret\">")
                .AppendLine("<div class=\"sidebar-group-items\">");
            foreach (var child in children)
                AppendSidebarEntry(html, child, currentPath, level + 1, basePath, localePrefix, l);
            html.AppendLine("</div>").AppendLine("</div>");
            return;
        }

        var hasActiveDescendant = ContainsActiveDescendant(entry, currentPath);
        var isCollapsible = entry.Collapsed.HasValue;
        var startsOpen = !isCollapsible || entry.Collapsed == false || hasActiveDescendant;
        var headerClass = $"sidebar-group-title level-{level}{(hasActiveDescendant ? " has-active" : "")}";
        var headingTag = level == 0 ? "h2" : "h3";

        // .sidebar-group-title stays a plain <div>; UA-default <summary> styling can't be fully overridden, so <summary> only wraps it as a near-invisible click target.
        if (isCollapsible)
            html.AppendLine($"<details class=\"sidebar-group\"{(startsOpen ? " open" : "")}>")
                .AppendLine("<summary class=\"sidebar-group-summary\">")
                .AppendLine($"<div class=\"{headerClass}\"><{headingTag}>{LayoutProvider.HtmlEncode(l.Label(entry.Title))}</{headingTag}>{CaretSvg}</div>")
                .AppendLine("</summary>");
        else
            html.AppendLine("<div class=\"sidebar-group no-caret\">")
                .AppendLine($"<div class=\"{headerClass}\"><{headingTag}>{LayoutProvider.HtmlEncode(l.Label(entry.Title))}</{headingTag}></div>");

        html.AppendLine("<div class=\"sidebar-group-items\">");
        foreach (var child in children)
            AppendSidebarEntry(html, child, currentPath, level + 1, basePath, localePrefix, l);
        html.AppendLine("</div>");
        html.AppendLine(isCollapsible ? "</details>" : "</div>");
    }

    public static string BuildTopNavHtml(List<TopNavItem>? topNav, string currentPath, string basePath, Localization? localization = null, string localePrefix = "")
    {
        var l = localization ?? Localization.Default;
        if (topNav is null || topNav.Count == 0)
            return string.Empty;

        var configPath = LocaleRouting.Delocalize(localePrefix, currentPath);
        var html = new StringBuilder();
        html.AppendLine($"<nav class=\"top-nav\" aria-label=\"{LayoutProvider.HtmlEncode(l.TopNavAria)}\">");
        foreach (var item in topNav)
            AppendTopNavItem(html, item, configPath, isMobile: false, basePath, localePrefix, l);
        html.AppendLine("</nav>");
        return html.ToString();
    }

    public static string BuildMobileTopNavHtml(List<TopNavItem>? topNav, string currentPath, string basePath, Localization? localization = null, string localePrefix = "")
    {
        var l = localization ?? Localization.Default;
        if (topNav is null || topNav.Count == 0)
            return string.Empty;

        var configPath = LocaleRouting.Delocalize(localePrefix, currentPath);
        var html = new StringBuilder();
        html.AppendLine($"<nav class=\"mobile-top-nav\" aria-label=\"{LayoutProvider.HtmlEncode(l.TopNavAria)}\">");
        foreach (var item in topNav)
            AppendTopNavItem(html, item, configPath, isMobile: true, basePath, localePrefix, l);
        html.AppendLine("</nav>");
        return html.ToString();
    }

    public static void AppendTopNavItem(StringBuilder html, TopNavItem item, string currentPath, bool isMobile, string basePath, string localePrefix = "", Localization? localization = null)
    {
        var l = localization ?? Localization.Default;
        if (item.Items is { Count: > 0 } children)
        {
            if (isMobile)
            {
                html.AppendLine("<details class=\"mobile-top-nav-group\">");
                html.AppendLine($"<summary><span>{LayoutProvider.HtmlEncode(l.Label(item.Text))}</span>{CaretSvg}</summary>");
                foreach (var child in children)
                    AppendTopNavLink(html, child, currentPath, "mobile-top-nav-link", basePath, localePrefix: localePrefix, localization: l);
                html.AppendLine("</details>");
            }
            else
            {
                html.AppendLine("<div class=\"top-nav-item has-dropdown\">");
                html.AppendLine($"<button type=\"button\" class=\"top-nav-link\" aria-expanded=\"false\" aria-haspopup=\"true\">{LayoutProvider.HtmlEncode(l.Label(item.Text))} " +
                    "<svg class=\"top-nav-chevron\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" aria-hidden=\"true\"><path d=\"M6 9l6 6 6-6\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/></svg></button>");
                html.AppendLine("<div class=\"top-nav-dropdown-menu\">");
                foreach (var child in children)
                    AppendTopNavLink(html, child, currentPath, "top-nav-dropdown-link", basePath, localePrefix: localePrefix, localization: l);
                html.AppendLine("</div>");
                html.AppendLine("</div>");
            }
            return;
        }

        AppendTopNavLink(html, item, currentPath, isMobile ? "mobile-top-nav-link" : "top-nav-link", basePath, wrapInItemDiv: !isMobile, localePrefix: localePrefix, localization: l);
    }

    public static string BuildBottomNavHtml(List<TopNavItem> bottomNav, string basePath, Localization? localization = null, string localePrefix = "")
    {
        var l = localization ?? Localization.Default;
        var links = bottomNav.Where(item => !string.IsNullOrWhiteSpace(item.Text) && !string.IsNullOrWhiteSpace(item.Link)).ToList();
        if (links.Count == 0)
            return string.Empty;

        var html = new StringBuilder();
        html.Append($"<nav class=\"footer-nav\" aria-label=\"{LayoutProvider.HtmlEncode(l.FooterNavAria)}\">");
        foreach (var item in links)
        {
            var isExternal = UrlPaths.IsExternal(item.Link!);
            var href = isExternal ? item.Link! : UrlPaths.Href(basePath, LocalizeLink(localePrefix, item.Link!));
            html.Append($"<a href=\"{LayoutProvider.HtmlEncode(href)}\"{(isExternal ? ExternalLinkRel : "")}>")
                .Append(LayoutProvider.HtmlEncode(l.Label(item.Text)))
                .Append(isExternal ? ExternalLinkIcon : "")
                .Append("</a>");
        }
        return html.Append("</nav>").ToString();
    }

    public static void AppendTopNavLink(StringBuilder html, TopNavItem item, string currentPath, string cssClass, string basePath, bool wrapInItemDiv = false, string localePrefix = "", Localization? localization = null)
    {
        var l = localization ?? Localization.Default;
        var link = item.Link ?? "#";
        var isExternal = UrlPaths.IsExternal(link);
        var normalizedLink = isExternal ? link : UrlPaths.Href(basePath, LocalizeLink(localePrefix, link));
        var isActive = !isExternal && IsTopNavActive(link, currentPath);
        var activeClass = isActive ? " active" : "";
        var relAttr = isExternal ? ExternalLinkRel : "";
        const string externalIcon = ExternalLinkIcon;

        if (wrapInItemDiv)
            html.AppendLine("<div class=\"top-nav-item\">");

        var label = LayoutProvider.HtmlEncode(l.Label(item.Text));
        html.AppendLine(
            $"<a href=\"{LayoutProvider.HtmlEncode(normalizedLink)}\" class=\"{cssClass}{activeClass}\"{relAttr}>" +
            $"<span class=\"top-nav-label\" data-label=\"{label}\">{label}</span>{(isExternal ? externalIcon : "")}</a>");

        if (wrapInItemDiv)
            html.AppendLine("</div>");
    }

    /// <summary>A section link stays active across the whole section, not just its landing page.</summary>
    private static bool IsTopNavActive(string link, string currentPath)
    {
        var linkPath = link.Trim('/');
        var current = currentPath.Trim('/');

        if (linkPath.Equals(current, StringComparison.OrdinalIgnoreCase))
            return true;

        // A root link ("/") has no section of its own and would otherwise match every page.
        var section = linkPath.Split('/')[0];
        return section.Length > 0
            && current.StartsWith(section, StringComparison.OrdinalIgnoreCase)
            && (current.Length == section.Length || current[section.Length] == '/');
    }

    public static string ToDisplayName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var display = name.Replace('-', ' ').Replace('_', ' ');
        return char.ToUpperInvariant(display[0]) + display[1..];
    }

    // Prev/next walks the sidebar actually showing for this page, same precedence as BuildNavigationHtml.
    public static List<string?> GetOrderedPaths(NavigationNode node, Config? config, string currentPath, string localePrefix = "")
    {
        var configPath = LocaleRouting.Delocalize(localePrefix, currentPath);

        if (config?.Sidebar is { Count: > 0 } sidebars)
        {
            var matchedSections = SidebarResolver.Resolve(sidebars, configPath);
            if (matchedSections is not null)
                return Localize(FlattenNavEntries(matchedSections), localePrefix);
        }

        if (config?.Nav is { Count: > 0 } sections)
            return Localize(FlattenNavEntries(sections), localePrefix);

        return FlattenNavigation(node);
    }

    private static List<string?> Localize(List<string?> paths, string localePrefix) =>
        localePrefix.Length == 0
            ? paths
            : paths.Select(path => path is null ? null : LocalizeLink(localePrefix, path)).ToList();

    public static List<string?> FlattenNavigation(NavigationNode node)
    {
        var list = new List<string?>();
        foreach (var child in node.Children)
        {
            if (child.Path != null)
                list.Add(child.Path);
            if (child.Children.Count > 0)
                list.AddRange(FlattenNavigation(child));
        }
        return list;
    }

    public static List<string?> FlattenNavEntries(List<NavEntry> entries)
    {
        var list = new List<string?>();
        foreach (var entry in entries)
        {
            // External targets aren't pages, so they must not land in the prev/next sequence.
            if (entry.Path != null && !UrlPaths.IsExternal(entry.Path))
                list.Add(entry.Path.Trim('/'));
            if (entry.Items is { Count: > 0 } children)
                list.AddRange(FlattenNavEntries(children));
        }
        return list;
    }
}
