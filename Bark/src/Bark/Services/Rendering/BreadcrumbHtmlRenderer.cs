using System.Text;
using Bark.Models;
using Bark.Services.Layout;

namespace Bark.Services.Rendering;

public static class BreadcrumbHtmlRenderer
{
    public static string BuildBreadcrumbHtml(IReadOnlyList<BreadcrumbItem> crumbs, string currentTitle, string basePath)
    {
        var html = new StringBuilder();
        for (var i = 0; i < crumbs.Count - 1; i++)
        {
            var crumb = crumbs[i];
            html.Append(crumb.Path is { } path
                ? $"<a href=\"{UrlPaths.Href(basePath, path)}\">{LayoutProvider.HtmlEncode(crumb.Title)}</a>"
                : $"<span class=\"crumb-text\">{LayoutProvider.HtmlEncode(crumb.Title)}</span>");
            html.Append("<span class=\"separator\">/</span>");
        }
        html.Append($"<span class=\"current\" aria-current=\"page\">{LayoutProvider.HtmlEncode(currentTitle)}</span>");
        return html.ToString();
    }
}
