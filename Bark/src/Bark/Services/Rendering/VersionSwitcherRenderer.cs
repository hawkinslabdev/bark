using System.Text;
using Bark.Models;
using Bark.Services.Layout;

namespace Bark.Services.Rendering;

/// <summary>
/// Each version is its own deploy. Links are relative to the site root (the latest deploy's base path): the first
/// entry is the latest at <c>"/"</c>, an older one at e.g. <c>"/v1/"</c> runs with base path <c>{site root}/v1</c>.
/// </summary>
public static class VersionSwitcherRenderer
{
    private const string ChevronIcon =
        "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.2\" stroke-linecap=\"round\" " +
        "stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M6 9l6 6 6-6\"/></svg>";

    public static IReadOnlyList<TopNavItem>? Active(Config? config) =>
        config?.Versions is { Enabled: true, Items: { Count: > 1 } items } ? items : null;

    public static int CurrentIndex(IReadOnlyList<TopNavItem>? versions, string basePath)
    {
        if (versions is not { Count: > 0 })
            return -1;

        var current = basePath.Trim('/');
        for (var i = 1; i < versions.Count; i++)
        {
            if (RelativePath(versions[i].Link) is { Length: > 0 } path
                && (current.Equals(path, StringComparison.OrdinalIgnoreCase)
                    || current.EndsWith("/" + path, StringComparison.OrdinalIgnoreCase)))
                return i;
        }

        return 0;
    }

    public static string Href(IReadOnlyList<TopNavItem> versions, int current, string basePath, string? link)
    {
        if (RelativePath(link) is not { } path)
            return link ?? "/";

        var siteRoot = basePath.TrimEnd('/');
        if (current > 0 && RelativePath(versions[current].Link) is { Length: > 0 } own)
            siteRoot = siteRoot[..^own.Length].TrimEnd('/');

        return path.Length == 0 ? $"{siteRoot}/" : $"{siteRoot}/{path}/";
    }

    public static string Build(IReadOnlyList<TopNavItem>? versions, int current, string basePath, Localization localization)
    {
        if (versions is not { Count: > 1 } || current < 0)
            return string.Empty;

        var currentText = versions[current].Text;
        var toggleLabel = LayoutProvider.HtmlEncode($"{localization.VersionSwitcher} ({currentText})");
        var html = new StringBuilder();
        html.Append("<div class=\"version-switcher\">")
            .Append("<button type=\"button\" class=\"version-toggle\" id=\"version-toggle\" aria-haspopup=\"menu\" aria-controls=\"version-dropdown\" aria-expanded=\"false\" aria-label=\"")
            .Append(toggleLabel).Append("\" title=\"").Append(toggleLabel).Append("\">")
            .Append(LayoutProvider.HtmlEncode(currentText))
            .Append(ChevronIcon)
            .Append("</button>")
            .Append("<div class=\"locale-dropdown version-dropdown\" id=\"version-dropdown\" hidden role=\"menu\" aria-labelledby=\"version-toggle\">");

        for (var i = 0; i < versions.Count; i++)
        {
            var isCurrent = i == current;
            html.Append("<a class=\"locale-option")
                .Append(isCurrent ? " locale-option--current\" aria-current=\"true\"" : "\"")
                .Append(" role=\"menuitem\" href=\"").Append(LayoutProvider.HtmlEncode(Href(versions, current, basePath, versions[i].Link))).Append("\">")
                .Append(LayoutProvider.HtmlEncode(versions[i].Text)).Append("</a>");
        }

        return html.Append("</div></div>").ToString();
    }

    public static string Notice(IReadOnlyList<TopNavItem>? versions, int current, string basePath, Localization localization) =>
        versions is { Count: > 1 } && current > 0
            ? TranslationNoticeRenderer.Build("translation-notice version-notice", localization.VersionOutdated, localization.VersionLatestLink, Href(versions, current, basePath, versions[0].Link))
            : string.Empty;

    /// <summary>On the latest deploy, maps "/{latest text}/rest" to "/rest" so version-pinned links work before the version moves.</summary>
    public static string? LatestAlias(IReadOnlyList<TopNavItem>? versions, string basePath, string path)
    {
        if (versions is not { Count: > 1 } || CurrentIndex(versions, basePath) != 0)
            return null;

        var trimmed = path.Trim('/');
        var slash = trimmed.IndexOf('/');
        var head = slash < 0 ? trimmed : trimmed[..slash];
        if (!head.Equals(versions[0].Text, StringComparison.OrdinalIgnoreCase))
            return null;

        // Browsers read "//host" and "/\host" as another origin
        var rest = slash < 0 ? string.Empty : trimmed[(slash + 1)..].TrimStart('/', '\\');
        return $"{basePath}/{rest}";
    }

    public static IEnumerable<string> Validate(VersionsConfig? versions)
    {
        if (versions is not { Enabled: true, Items: { } items })
            yield break;

        if (items.Count < 2)
            yield return "list at least two versions, newest first; the switcher stays hidden until then";

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (string.IsNullOrWhiteSpace(item.Text) || string.IsNullOrWhiteSpace(item.Link))
                yield return $"entry {i + 1} needs both \"text\" and \"link\"";
            else if (!seen.Add(item.Text))
                yield return $"\"{item.Text}\" is listed more than once";

            var path = RelativePath(item.Link);
            if (item.Link is { Length: > 0 } link && path is null && !link.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                yield return $"\"{link}\" must start with \"/\" (relative to the site root) or be an http(s) URL";
            else if (i == 0 && path is { Length: > 0 })
                yield return $"the first entry is the latest and must link to \"/\", not \"{item.Link}\"";
            else if (i > 0 && path is { Length: 0 })
                yield return $"only the first entry may link to \"/\"; \"{item.Text}\" needs its own path";
        }
    }

    private static string? RelativePath(string? link) =>
        link is { Length: > 0 } && link[0] == '/' && !link.StartsWith("//", StringComparison.Ordinal)
            ? link.Trim('/')
            : null;
}
