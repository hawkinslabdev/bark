using System.Text;
using Bark.Models;
using Bark.Services.Layout;

namespace Bark.Services.Rendering;

public static class LocaleSwitcherRenderer
{
    private const string GlobeIcon =
        "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" " +
        "stroke-linejoin=\"round\" aria-hidden=\"true\"><circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M3 12h18\"/>" +
        "<path d=\"M12 3a15 15 0 0 1 0 18a15 15 0 0 1 0-18z\"/></svg>";

    public static string Build(
        Config? config,
        string currentCode,
        string currentPath,
        string basePath,
        Localization localization)
    {
        var rootCode = LocaleRouting.RootCode(config);
        var codes = new List<string> { rootCode };
        codes.AddRange(LocaleRouting.TreeCodes(config).Where(code => code != rootCode));
        if (codes.Count < 2)
            return string.Empty;

        var currentPrefix = currentCode == rootCode ? string.Empty : currentCode;
        var rootPath = LocaleRouting.Delocalize(currentPrefix, currentPath);

        var toggleLabel = LayoutProvider.HtmlEncode($"{localization.LocaleSwitcher} ({LocaleRouting.LabelOf(config, currentCode)})");
        var html = new StringBuilder();
        html.Append("<div class=\"locale-switcher\">")
            .Append("<button type=\"button\" class=\"locale-toggle icon-btn\" id=\"locale-toggle\" aria-haspopup=\"menu\" aria-controls=\"locale-dropdown\" aria-expanded=\"false\" aria-label=\"")
            .Append(toggleLabel).Append("\" title=\"").Append(toggleLabel)
            .Append("\">")
            .Append(GlobeIcon)
            .Append("</button>")
            .Append("<div class=\"locale-dropdown\" id=\"locale-dropdown\" hidden role=\"menu\" aria-labelledby=\"locale-toggle\">");

        foreach (var code in codes)
        {
            var prefix = code == rootCode ? string.Empty : code;
            var target = LocaleRouting.Localize(prefix, rootPath);
            if (target.Equals("index", StringComparison.OrdinalIgnoreCase))
                target = string.Empty;
            var isCurrent = code == currentCode;

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
