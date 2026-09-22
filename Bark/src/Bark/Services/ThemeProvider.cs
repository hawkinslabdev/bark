using Bark.Models;
using Bark.Services.Layout;

namespace Bark.Services;

public static class ThemeProvider
{
    public static string BuildThemeCss(ThemeOptions? theme, string? nonce = null)
    {
        if (theme is null)
            return string.Empty;

        var brandVars = new List<string>();
        AddVar(brandVars, "--primary-color", theme.PrimaryColor);
        AddVar(brandVars, "--accent", theme.PrimaryColor);
        AddVar(brandVars, "--accent-light", theme.AccentLight);
        AddVar(brandVars, "--font-sans", theme.FontSans);
        AddVar(brandVars, "--font-mono", theme.FontMono);

        var surfaceVars = new List<string>();
        AddVar(surfaceVars, "--bg-color", theme.BgColor);
        AddVar(surfaceVars, "--sidebar-bg", theme.SidebarBg);
        AddVar(surfaceVars, "--text-color", theme.TextColor);
        AddVar(surfaceVars, "--text-muted", theme.TextMuted);
        AddVar(surfaceVars, "--border", theme.BorderColor);
        AddVar(surfaceVars, "--code-bg", theme.CodeBg);

        if (brandVars.Count == 0 && surfaceVars.Count == 0)
            return string.Empty;

        var nonceAttr = nonce is { Length: > 0 } ? $" nonce=\"{nonce}\"" : "";
        var blocks = "";
        if (brandVars.Count > 0)
            blocks += ":root, :root[data-theme=\"dark\"] {\n" + string.Join("\n", brandVars) + "\n}\n";
        // Light-mode-only selector: the theme's own [data-theme="dark"] block outranks this on toggle.
        if (surfaceVars.Count > 0)
            blocks += ":root:not([data-theme=\"dark\"]) {\n" + string.Join("\n", surfaceVars) + "\n}\n";

        return $"<style{nonceAttr}>\n{blocks}</style>";
    }

    public static string BuildCustomCssLink(ThemeOptions? theme, string themeDir, string basePath = "")
    {
        var url = theme?.CustomCssUrl is { Length: > 0 } configured
            ? LayoutProvider.ResolveAssetUrl(configured, basePath)
            : (File.Exists(Path.Combine(themeDir, "custom.css")) ? $"{basePath}/theme/custom.css" : null);
        return url is { Length: > 0 }
            ? $"<link rel=\"stylesheet\" href=\"{url}\">"
            : string.Empty;
    }

    public static string BuildCustomJsScript(ThemeOptions? theme, string themeDir, string basePath = "")
    {
        var url = theme?.CustomJsUrl is { Length: > 0 } configured
            ? LayoutProvider.ResolveAssetUrl(configured, basePath)
            : (File.Exists(Path.Combine(themeDir, "custom.js")) ? $"{basePath}/theme/custom.js" : null);
        return url is { Length: > 0 }
            ? $"<script defer src=\"{url}\"></script>"
            : string.Empty;
    }

    public static string GetBrandText(ThemeOptions? theme)
    {
        if (theme?.BrandText is { Length: > 0 } brand)
            return System.Net.WebUtility.HtmlEncode(brand);
        return "Bark";
    }

    public static bool UseDarkMode(ThemeOptions? theme) => theme?.DarkMode ?? true;

    public static bool ShowScrollIndicator(ThemeOptions? theme) => theme?.ShowScrollIndicator ?? true;

    private static void AddVar(List<string> vars, string name, string? value)
    {
        if (value is { Length: > 0 })
            vars.Add($"    {name}: {value};");
    }
}
