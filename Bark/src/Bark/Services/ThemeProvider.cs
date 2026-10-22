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
        AddVar(brandVars, "--font-sans", theme.FontSans);
        AddVar(brandVars, "--font-mono", theme.FontMono);

        // A light-tuned brand hex fails contrast on dark surfaces, so it shares the surfaces' light-only scope.
        var surfaceVars = new List<string>();
        AddVar(surfaceVars, "--primary-color", theme.PrimaryColor);
        AddVar(surfaceVars, "--accent", theme.PrimaryColor);
        AddVar(surfaceVars, "--accent-light", theme.AccentLight);
        AddVar(surfaceVars, "--bg-color", theme.BgColor);
        AddVar(surfaceVars, "--sidebar-bg", theme.SidebarBg);
        AddVar(surfaceVars, "--text-color", theme.TextColor);
        AddVar(surfaceVars, "--text-muted", theme.TextMuted);
        AddVar(surfaceVars, "--border", theme.BorderColor);
        AddVar(surfaceVars, "--code-bg", theme.CodeBg);

        var darkVars = new List<string>();
        if (UseDarkMode(theme))
        {
            AddVar(darkVars, "--primary-color", theme.PrimaryColorDark);
            AddVar(darkVars, "--accent", theme.PrimaryColorDark);
            AddVar(darkVars, "--accent-light", theme.AccentLightDark);
        }

        if (brandVars.Count == 0 && surfaceVars.Count == 0 && darkVars.Count == 0)
            return string.Empty;

        var nonceAttr = nonce is { Length: > 0 } ? $" nonce=\"{nonce}\"" : "";
        var blocks = "";
        if (brandVars.Count > 0)
            blocks += ":root, :root[data-theme=\"dark\"] {\n" + string.Join("\n", brandVars) + "\n}\n";
        // Light-mode-only: with no data-theme set, an OS in dark mode must keep the theme's dark surfaces.
        if (surfaceVars.Count > 0)
        {
            var surface = string.Join("\n", surfaceVars);
            blocks += UseDarkMode(theme)
                ? ":root[data-theme=\"light\"] {\n" + surface + "\n}\n" +
                  "@media (prefers-color-scheme: light) {\n:root:not([data-theme=\"dark\"]) {\n" + surface + "\n}\n}\n"
                : ":root:not([data-theme=\"dark\"]) {\n" + surface + "\n}\n";
        }
        // Same selectors as ThemeCssBuilder's dark blocks; emitted later, so they win on equal specificity.
        if (darkVars.Count > 0)
        {
            var dark = string.Join("\n", darkVars);
            blocks += "@media (prefers-color-scheme: dark) {\n:root:not([data-theme=\"light\"]) {\n" + dark + "\n}\n}\n" +
                      ":root[data-theme=\"dark\"] {\n" + dark + "\n}\n";
        }

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
