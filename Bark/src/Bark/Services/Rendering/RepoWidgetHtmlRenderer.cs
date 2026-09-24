using System.Globalization;
using Bark.Services.Layout;

namespace Bark.Services.Rendering;

public static class RepoWidgetHtmlRenderer
{
    public static async ValueTask<string> BuildAsync(
        string? repoUrl,
        RepoStats? stats,
        string primaryIconsDir,
        string? fallbackIconsDir,
        Localization localization)
    {
        if (string.IsNullOrWhiteSpace(repoUrl) || !Uri.TryCreate(repoUrl, UriKind.Absolute, out var uri))
            return string.Empty;

        var icon = await IconProvider.InlineSvgAsync("github", primaryIconsDir, fallbackIconsDir);
        var repoPath = uri.AbsolutePath.Trim('/');

        var titleAttr = stats?.Version is { Length: > 0 } version
            ? $" title=\"{LayoutProvider.HtmlEncode(version)}\""
            : "";

        var label = string.Format(CultureInfo.InvariantCulture, localization.RepoWidgetAria, repoPath);

        return $"<a href=\"{LayoutProvider.HtmlEncode(repoUrl)}\" class=\"repo-widget\" " +
            $"target=\"_blank\" rel=\"noopener noreferrer\" aria-label=\"{LayoutProvider.HtmlEncode(label)}\"{titleAttr}>{icon}</a>";
    }
}
