using System.Text;
using Bark.Models;
using Bark.Services.Layout;

namespace Bark.Services.Rendering;

public static class PageControlsHtmlRenderer
{
    private const string Divider = "<div class=\"page-controls-divider\" role=\"separator\" aria-hidden=\"true\"></div>";

    private const string DefaultEditLinkIcon =
        "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" " +
        "stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\" width=\"14\" height=\"14\">" +
        "<path d=\"M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6\"/>" +
        "<polyline points=\"15 3 21 3 21 9\"/>" +
        "<line x1=\"10\" y1=\"14\" x2=\"21\" y2=\"3\"/>" +
        "</svg>";

    private const string ExternalIcon =
        "<svg class=\"page-controls-external\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" " +
        "stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\" focusable=\"false\">" +
        "<path d=\"M7 17L17 7M9 7h8v8\"/></svg>";

    public static string BuildPageControlsHtml(
        DocumentationPage page,
        PageControlsConfig? config,
        EditLinkConfig? barkEditLink,
        string basePath,
        string docsRoot,
        string? resolvedEditIcon = null,
        Localization? localization = null)
    {
        if (config is null) return string.Empty;

        var hasRelPath = page.OriginalRelativePath is not null;
        var hasFileGroup = config.DownloadMarkdown && hasRelPath;
        var hasRss = config.SubscribeRss;
        var hasEditLink = config.EditLink is not null
                       && barkEditLink is { Pattern.Length: > 0 }
                       && hasRelPath;

        if (!hasFileGroup && !hasRss && !hasEditLink)
            return string.Empty;

        var l = localization ?? Localization.Default;
        var relPath = page.OriginalRelativePath ?? string.Empty;
        var sb = new StringBuilder();

        var newTab = $"<span class=\"sr-only\"> ({LayoutProvider.HtmlEncode(l.OpensInNewTab)})</span>{ExternalIcon}";

        sb.Append("<div class=\"page-controls\">");
        sb.Append("<button type=\"button\" class=\"page-controls-toggle icon-btn\" id=\"page-controls-toggle\" " +
                  $"aria-expanded=\"false\" aria-haspopup=\"menu\" aria-controls=\"page-controls-menu\" aria-label=\"{LayoutProvider.HtmlEncode(l.PageOptions)}\">" +
                  "<svg viewBox=\"0 0 24 24\" fill=\"currentColor\" aria-hidden=\"true\" width=\"16\" height=\"16\">" +
                  "<circle cx=\"12\" cy=\"5\" r=\"1.5\"/><circle cx=\"12\" cy=\"12\" r=\"1.5\"/><circle cx=\"12\" cy=\"19\" r=\"1.5\"/>" +
                  "</svg></button>");
        sb.Append("<div class=\"page-controls-menu\" id=\"page-controls-menu\" hidden role=\"menu\" aria-labelledby=\"page-controls-toggle\">");

        // ── Group 1: file access (Copy, View, RSS) ──
        if (hasFileGroup)
        {
            var rawHref = LayoutProvider.HtmlEncode($"{basePath}/raw/{relPath.TrimStart('/')}");
            var viewHref = LayoutProvider.HtmlEncode($"{basePath}/raw/{relPath.TrimStart('/')}?view=true");

            sb.Append($"<button type=\"button\" class=\"page-controls-item\" role=\"menuitem\" tabindex=\"-1\" data-copy-url=\"{rawHref}\">")
              .Append("<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\" width=\"14\" height=\"14\">")
              .Append("<rect x=\"9\" y=\"2\" width=\"6\" height=\"4\" rx=\"1\"/>")
              .Append("<path d=\"M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2\"/>")
              .Append($"</svg><span class=\"page-controls-label\">{LayoutProvider.HtmlEncode(l.CopyPage)}</span></button>");

            sb.Append($"<a class=\"page-controls-item\" role=\"menuitem\" tabindex=\"-1\" href=\"{viewHref}\" target=\"_blank\" rel=\"noopener noreferrer\">")
              .Append("<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\" width=\"14\" height=\"14\">")
              .Append("<path d=\"M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z\"/>")
              .Append("<circle cx=\"12\" cy=\"12\" r=\"3\"/>")
              .Append($"</svg><span class=\"page-controls-label\">{LayoutProvider.HtmlEncode(l.ViewAsMarkdown)}</span>{newTab}</a>");
        }

        if (hasRss)
        {
            var feedPath = LayoutProvider.HtmlEncode($"{basePath}/feed.xml");
            sb.Append($"<button type=\"button\" class=\"page-controls-item\" role=\"menuitem\" tabindex=\"-1\" data-copy-value=\"{feedPath}\">")
              .Append("<svg viewBox=\"0 0 24 24\" fill=\"currentColor\" aria-hidden=\"true\" width=\"14\" height=\"14\">")
              .Append("<path d=\"M6.18 15.64a2.18 2.18 0 0 1 2.18 2.18C8.36 19.01 7.38 20 6.18 20 4.98 20 4 19.01 4 17.82a2.18 2.18 0 0 1 2.18-2.18M4 4.44A15.56 15.56 0 0 1 19.56 20h-2.83A12.73 12.73 0 0 0 4 7.27V4.44m0 5.66a9.9 9.9 0 0 1 9.9 9.9h-2.83A7.07 7.07 0 0 0 4 12.93V10.1z\"/>")
              .Append($"</svg><span class=\"page-controls-label\">{LayoutProvider.HtmlEncode(l.CopyRssUrl)}</span></button>");
        }

        // ── Group 2: edit link (URL from top-level editLink config; label/icon from pageControls.editLink) ──
        if (hasEditLink)
        {
            if (hasFileGroup || hasRss) sb.Append(Divider);

            var encodedPath = string.Join("/", relPath.Split('/').Select(Uri.EscapeDataString));
            var editHref = LayoutProvider.HtmlEncode(barkEditLink!.Pattern.Replace(":path", encodedPath));
            var editLabel = LayoutProvider.HtmlEncode(config.EditLink!.Label);
            var editIcon = !string.IsNullOrWhiteSpace(resolvedEditIcon)
                ? resolvedEditIcon
                : DefaultEditLinkIcon;

            sb.Append($"<a class=\"page-controls-item\" role=\"menuitem\" tabindex=\"-1\" href=\"{editHref}\" target=\"_blank\" rel=\"noopener noreferrer nofollow\">")
              .Append(editIcon)
              .Append($"<span class=\"page-controls-label\">{editLabel}</span>")
              .Append(newTab)
              .Append("</a>");
        }

        sb.Append("</div><span class=\"sr-only\" role=\"status\" aria-live=\"polite\" data-page-controls-status></span></div>");
        return sb.ToString();
    }
}
