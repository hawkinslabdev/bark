using System.Text;
using Bark.Models;
using Bark.Services.Layout;

namespace Bark.Services.Rendering;

public static class VersionSwitcherRenderer
{
    private const string CaretIcon =
        "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" " +
        "stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"M6 9l6 6 6-6\"/></svg>";

    public static string Build(Config? config, string currentCode, string currentPath, string basePath, Localization localization, Func<string, bool> exists)
    {
        var versions = LocaleRouting.VersionCodes(config);
        if (versions.Count == 0)
            return string.Empty;

        var tree = TreeOf(config, currentCode);
        var locale = LocaleRouting.LocalePartOf(config, tree);
        var currentVersion = LocaleRouting.VersionPartOf(config, tree);
        var rootPath = LocaleRouting.Delocalize(tree, currentPath);
        var currentLabel = LocaleRouting.VersionLabelOf(config, currentVersion);
        var toggleLabel = LayoutProvider.HtmlEncode($"{localization.VersionSwitcher} ({currentLabel})");
        var html = new StringBuilder();
        html.Append("<div class=\"locale-switcher version-switcher\">")
            .Append("<button type=\"button\" class=\"version-toggle\" id=\"version-toggle\" aria-haspopup=\"menu\" aria-controls=\"version-dropdown\" aria-expanded=\"false\" aria-label=\"")
            .Append(toggleLabel).Append("\" title=\"").Append(toggleLabel).Append("\">")
            .Append(LayoutProvider.HtmlEncode(currentLabel)).Append(CaretIcon)
            .Append("</button>")
            .Append("<div class=\"locale-dropdown\" id=\"version-dropdown\" hidden role=\"menu\" aria-labelledby=\"version-toggle\">");

        foreach (var version in versions.Prepend(string.Empty))
        {
            var isCurrent = version == currentVersion;
            var href = Href(LocaleRouting.TreeOf(locale, version), rootPath, basePath, exists);
            html.Append("<a class=\"locale-option")
                .Append(isCurrent ? " locale-option--current\"" : "\"")
                .Append(" role=\"menuitem\" href=\"").Append(LayoutProvider.HtmlEncode(href)).Append('"');

            if (isCurrent)
                html.Append(" aria-current=\"true\"");

            html.Append('>').Append(LayoutProvider.HtmlEncode(LocaleRouting.VersionLabelOf(config, version))).Append("</a>");
        }

        return html.Append("</div></div>").ToString();
    }

    public static string Notice(Config? config, string currentCode, string currentPath, string basePath, Localization localization, Func<string, bool> exists)
    {
        var tree = TreeOf(config, currentCode);
        if (!LocaleRouting.IsVersion(config, tree))
            return string.Empty;

        var href = Href(LocaleRouting.LocalePartOf(config, tree), LocaleRouting.Delocalize(tree, currentPath), basePath, exists);
        return $"<div class=\"translation-notice version-notice\" role=\"note\"><span>{LayoutProvider.HtmlEncode(localization.VersionOutdated)}</span> " +
               $"<a href=\"{LayoutProvider.HtmlEncode(href)}\">{LayoutProvider.HtmlEncode(localization.VersionLatestLink)}</a></div>";
    }

    private static string TreeOf(Config? config, string currentCode) =>
        currentCode.Equals(LocaleRouting.RootCode(config), StringComparison.OrdinalIgnoreCase) ? string.Empty : currentCode;

    private static string Href(string prefix, string rootPath, string basePath, Func<string, bool> exists)
    {
        var target = LocaleRouting.Localize(prefix, rootPath);
        if (!exists(target.Length == 0 ? "index" : target))
            target = prefix;
        if (target.Equals("index", StringComparison.OrdinalIgnoreCase))
            target = string.Empty;
        return UrlPaths.Href(basePath, target);
    }
}
