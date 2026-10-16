using System.Text;
using Bark.Models;
using Bark.Services.Layout;

namespace Bark.Services.Rendering;

public static class SocialLinksHtmlRenderer
{
    public static async ValueTask<string> BuildSocialLinksHtmlAsync(IReadOnlyList<SocialLink>? links, string primaryIconsDir, string? fallbackIconsDir, Localization? localization = null)
    {
        if (links is not { Count: > 0 }) return string.Empty;

        var html = new StringBuilder();
        html.AppendLine("<div class=\"social-links\">");
        foreach (var link in links)
        {
            var iconSvg = await IconProvider.InlineSvgAsync(link.Icon, primaryIconsDir, fallbackIconsDir);
            var tooltip = localization?.Label(link.Title) ?? link.Title ?? link.Icon;
            var icon = iconSvg.Length > 0
                ? $"{iconSvg}<span class=\"social-label\" aria-hidden=\"true\">{LayoutProvider.HtmlEncode(tooltip)}</span>"
                : $"<span class=\"social-icon-text\" aria-hidden=\"true\">{LayoutProvider.HtmlEncode(link.Icon)}</span>";

            var label = $"{tooltip} (opens in new tab)";
            html.AppendLine($"<a href=\"{LayoutProvider.HtmlEncode(link.Url)}\" class=\"icon-btn\" target=\"_blank\" rel=\"noopener noreferrer\" title=\"{LayoutProvider.HtmlEncode(tooltip)}\" aria-label=\"{LayoutProvider.HtmlEncode(label)}\">{icon}</a>");
        }
        html.AppendLine("</div>");
        return html.ToString();
    }
}
