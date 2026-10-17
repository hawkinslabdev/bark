using System.Text;
using Bark.Models;
using Bark.Services.Layout;

namespace Bark.Services.Rendering;

public static class LocaleSwitcherRenderer
{
    private const string TranslateIcon =
        "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" " +
        "stroke-linejoin=\"round\" aria-hidden=\"true\"><path d=\"m5 8 6 6\"/><path d=\"m4 14 6-6 2-3\"/><path d=\"M2 5h12\"/>" +
        "<path d=\"M7 2h1\"/><path d=\"m22 22-5-10-5 10\"/><path d=\"M14 18h6\"/></svg>";

    public static string Build(
        Config? config,
        string currentCode,
        string currentPath,
        string basePath,
        Localization localization)
    {
        var rootCode = LocaleRouting.RootCode(config);
        var codes = new List<string> { rootCode };
        codes.AddRange(LocaleRouting.LocaleCodes(config).Where(code => code != rootCode));
        if (codes.Count < 2)
            return string.Empty;

        var currentPrefix = currentCode == rootCode ? string.Empty : currentCode;
        var currentLocale = LocaleRouting.LocalePartOf(config, currentPrefix) is { Length: > 0 } locale ? locale : rootCode;
        var version = LocaleRouting.VersionPartOf(config, currentPrefix);
        var rootPath = LocaleRouting.Delocalize(currentPrefix, currentPath);

        var toggleLabel = LayoutProvider.HtmlEncode($"{localization.LocaleSwitcher} ({LocaleRouting.LabelOf(config, currentLocale)})");
        var html = new StringBuilder();
        html.Append("<div class=\"locale-switcher\">")
            .Append("<button type=\"button\" class=\"locale-toggle icon-btn\" id=\"locale-toggle\" aria-haspopup=\"menu\" aria-controls=\"locale-dropdown\" aria-expanded=\"false\" aria-label=\"")
            .Append(toggleLabel).Append("\" title=\"").Append(toggleLabel)
            .Append("\">")
            .Append(TranslateIcon)
            .Append("</button>")
            .Append("<div class=\"locale-dropdown\" id=\"locale-dropdown\" hidden role=\"menu\" aria-labelledby=\"locale-toggle\">");

        foreach (var code in codes)
        {
            var prefix = LocaleRouting.TreeOf(code == rootCode ? string.Empty : code, version);
            var target = LocaleRouting.Localize(prefix, rootPath);
            if (target.Equals("index", StringComparison.OrdinalIgnoreCase))
                target = string.Empty;
            var isCurrent = code == currentLocale;

            html.Append("<a class=\"locale-option")
                .Append(isCurrent ? " locale-option--current\"" : "\"")
                .Append(" role=\"menuitem\" hreflang=\"").Append(LayoutProvider.HtmlEncode(LocaleRouting.LangOf(config, code)))
                .Append("\" href=\"").Append(LayoutProvider.HtmlEncode(UrlPaths.Href(basePath, target))).Append('"');

            if (isCurrent)
                html.Append(" aria-current=\"true\"");

            html.Append('>').Append(LayoutProvider.HtmlEncode(LocaleRouting.LabelOf(config, code))).Append("</a>");
        }

        return html.Append("</div></div>").ToString();
    }
}
