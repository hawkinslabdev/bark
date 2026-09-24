using System.Text;
using Bark.Services.Layout;

namespace Bark.Services.Rendering;

public static class PaginationHtmlRenderer
{
    public static string BuildPaginationHtml(string? prevTitle, string? prevPath, string? nextTitle, string? nextPath, string basePath, Localization? localization = null)
    {
        var l = localization ?? Localization.Default;
        var html = new StringBuilder();
        html.AppendLine($"<nav class=\"pagination\" aria-label=\"{LayoutProvider.HtmlEncode(l.PagerAria)}\">");

        if (prevPath != null)
        {
            var prevUrl = prevPath == "index" ? UrlPaths.Href(basePath, "") : UrlPaths.Href(basePath, prevPath);
            html.AppendLine($"<a href=\"{prevUrl}\" class=\"pagination-link prev\" rel=\"prev\">");
            html.AppendLine($"{ArrowSvg("M19 12H5M11 18l-6-6 6-6")}<span class=\"sr-only\">{LayoutProvider.HtmlEncode(l.PagerPrevious)}: </span>");
            html.AppendLine($"<span class=\"title\">{LayoutProvider.HtmlEncode(prevTitle)}</span>");
            html.AppendLine("</a>");
        }
        else
        {
            html.AppendLine("<span></span>");
        }

        if (nextPath != null)
        {
            var nextUrl = nextPath == "index" ? UrlPaths.Href(basePath, "") : UrlPaths.Href(basePath, nextPath);
            html.AppendLine($"<a href=\"{nextUrl}\" class=\"pagination-link next\" rel=\"next\">");
            html.AppendLine($"<span class=\"sr-only\">{LayoutProvider.HtmlEncode(l.PagerNext)}: </span><span class=\"title\">{LayoutProvider.HtmlEncode(nextTitle)}</span>");
            html.AppendLine(ArrowSvg("M5 12h14M13 6l6 6-6 6"));
            html.AppendLine("</a>");
        }
        else
        {
            html.AppendLine("<span></span>");
        }

        html.AppendLine("</nav>");
        return html.ToString();
    }

    private static string ArrowSvg(string path) =>
        $"<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\" focusable=\"false\"><path d=\"{path}\"/></svg>";
}
