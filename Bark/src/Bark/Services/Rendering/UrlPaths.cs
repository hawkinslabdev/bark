using Bark.Services.Layout;

namespace Bark.Services.Rendering;

public static class UrlPaths
{
    /// <summary>True for an absolute http/https nav target; every other scheme falls through to <see cref="Href"/> as a relative path, keeping <c>javascript:</c> inert.</summary>
    public static bool IsExternal(string? link) =>
        link is not null
        && (link.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || link.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

    public static string Href(string basePath, string path)
    {
        var trimmed = path.Trim('/');
        return trimmed.Length == 0
            ? (basePath.Length == 0 ? "/" : $"{basePath}/")
            : $"{basePath}/{LayoutProvider.HtmlEncode(trimmed)}/";
    }
}
