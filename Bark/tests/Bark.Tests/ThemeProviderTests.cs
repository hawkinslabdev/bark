using Bark.Models;
using Bark.Services;

namespace Bark.Tests;

public sealed class ThemeProviderTests : IDisposable
{
    private readonly string _themeDir = Path.Combine(Path.GetTempPath(), $"bark-theme-test-{Guid.NewGuid():N}");

    public ThemeProviderTests() => Directory.CreateDirectory(_themeDir);
    public void Dispose() => Directory.Delete(_themeDir, recursive: true);

    [Fact]
    public void BuildThemeCss_PrimaryColor_AlsoEmitsAccent()
    {
        // --primary-color is the documented field name; --accent is what the stylesheet reads.
        var css = ThemeProvider.BuildThemeCss(new ThemeOptions { PrimaryColor = "#7c3aed" });
        Assert.Contains("--primary-color: #7c3aed;", css);
        Assert.Contains("--accent: #7c3aed;", css);
    }

    [Fact]
    public void BuildThemeCss_Font_TargetsBothLightAndDarkRoots()
    {
        var css = ThemeProvider.BuildThemeCss(new ThemeOptions { FontSans = "Inter" });
        Assert.Contains(":root, :root[data-theme=\"dark\"] {\n    --font-sans: Inter;", css);
    }

    [Fact]
    public void BuildThemeCss_PrimaryColor_StaysOutOfDarkMode()
    {
        // A light-tuned brand hex on a dark surface fails AA contrast.
        var css = ThemeProvider.BuildThemeCss(new ThemeOptions { PrimaryColor = "#7c3aed" });
        Assert.Contains(":root[data-theme=\"light\"] {\n    --primary-color: #7c3aed;", css);
        Assert.DoesNotContain(":root[data-theme=\"dark\"]", css);
        Assert.DoesNotContain("prefers-color-scheme: dark", css);
    }

    [Fact]
    public void BuildThemeCss_DarkValues_MirrorTheThemesDarkSelectors()
    {
        var css = ThemeProvider.BuildThemeCss(new ThemeOptions { PrimaryColorDark = "#c4b5fd", AccentLightDark = "#2e1065" });
        const string vars = "    --primary-color: #c4b5fd;\n    --accent: #c4b5fd;\n    --accent-light: #2e1065;\n}\n";
        Assert.Contains("@media (prefers-color-scheme: dark) {\n:root:not([data-theme=\"light\"]) {\n" + vars + "}\n", css);
        Assert.Contains(":root[data-theme=\"dark\"] {\n" + vars, css);
    }

    [Fact]
    public void BuildThemeCss_DarkValues_DroppedWhenDarkModeDisabled()
    {
        Assert.Equal("", ThemeProvider.BuildThemeCss(new ThemeOptions { PrimaryColorDark = "#c4b5fd", DarkMode = false }));
    }

    [Fact]
    public void BuildThemeCss_SurfaceColor_TargetsLightRootOnly()
    {
        var css = ThemeProvider.BuildThemeCss(new ThemeOptions { BgColor = "#fff" });
        Assert.Contains(":root:not([data-theme=\"dark\"])", css);
        Assert.DoesNotContain(":root, :root[data-theme=\"dark\"]", css);
    }

    [Fact]
    public void BuildThemeCss_SurfaceColor_DoesNotApplyWhenOsIsDark()
    {
        // Unset data-theme + OS dark must not pick up light surfaces, or the page renders half light, half dark.
        var css = ThemeProvider.BuildThemeCss(new ThemeOptions { BgColor = "#fff" });
        Assert.Contains(":root[data-theme=\"light\"] {\n    --bg-color: #fff;", css);
        Assert.Contains("@media (prefers-color-scheme: light) {\n:root:not([data-theme=\"dark\"]) {\n    --bg-color: #fff;", css);
        Assert.DoesNotContain("<style>\n:root:not([data-theme=\"dark\"])", css);
    }

    [Fact]
    public void BuildThemeCss_SurfaceColor_DarkModeDisabled_AppliesRegardlessOfOs()
    {
        var css = ThemeProvider.BuildThemeCss(new ThemeOptions { BgColor = "#fff", DarkMode = false });
        Assert.Contains("<style>\n:root:not([data-theme=\"dark\"]) {\n    --bg-color: #fff;", css);
        Assert.DoesNotContain("prefers-color-scheme", css);
    }

    [Fact]
    public void BuildThemeCss_NoValuesSet_ReturnsEmpty()
    {
        Assert.Equal("", ThemeProvider.BuildThemeCss(new ThemeOptions()));
    }

    [Fact]
    public void BuildCustomCssLink_ConfiguredRootRelative_ResolvesBasePath()
    {
        var theme = new ThemeOptions { CustomCssUrl = "/theme/custom.css" };
        var result = ThemeProvider.BuildCustomCssLink(theme, _themeDir, basePath: "/docs");
        Assert.Contains("href=\"/docs/theme/custom.css\"", result);
    }

    [Fact]
    public void BuildCustomCssLink_ConfiguredAbsoluteUrl_Unchanged()
    {
        var theme = new ThemeOptions { CustomCssUrl = "https://cdn.example.com/styles.css" };
        var result = ThemeProvider.BuildCustomCssLink(theme, _themeDir, basePath: "/docs");
        Assert.Contains("href=\"https://cdn.example.com/styles.css\"", result);
    }

    [Fact]
    public void BuildCustomCssLink_NoConfiguredUrl_FallsBackToAutoDetected()
    {
        File.WriteAllText(Path.Combine(_themeDir, "custom.css"), "");
        var result = ThemeProvider.BuildCustomCssLink(new ThemeOptions(), _themeDir, basePath: "/docs");
        Assert.Contains("href=\"/docs/theme/custom.css\"", result);
    }

    [Fact]
    public void BuildCustomCssLink_NullThemeAndNoFile_ReturnsEmpty()
    {
        var result = ThemeProvider.BuildCustomCssLink(null, _themeDir, basePath: "/docs");
        Assert.Equal("", result);
    }

    [Fact]
    public void BuildCustomJsScript_ConfiguredRootRelative_ResolvesBasePath()
    {
        var theme = new ThemeOptions { CustomJsUrl = "/theme/custom.js" };
        var result = ThemeProvider.BuildCustomJsScript(theme, _themeDir, basePath: "/docs");
        Assert.Contains("src=\"/docs/theme/custom.js\"", result);
    }

    [Fact]
    public void BuildCustomJsScript_ConfiguredAbsoluteUrl_Unchanged()
    {
        var theme = new ThemeOptions { CustomJsUrl = "https://cdn.example.com/script.js" };
        var result = ThemeProvider.BuildCustomJsScript(theme, _themeDir, basePath: "/docs");
        Assert.Contains("src=\"https://cdn.example.com/script.js\"", result);
    }

    [Fact]
    public void BuildCustomJsScript_NoConfiguredUrl_FallsBackToAutoDetected()
    {
        File.WriteAllText(Path.Combine(_themeDir, "custom.js"), "");
        var result = ThemeProvider.BuildCustomJsScript(new ThemeOptions(), _themeDir, basePath: "/docs");
        Assert.Contains("src=\"/docs/theme/custom.js\"", result);
    }

    [Fact]
    public void BuildCustomJsScript_NullThemeAndNoFile_ReturnsEmpty()
    {
        var result = ThemeProvider.BuildCustomJsScript(null, _themeDir, basePath: "/docs");
        Assert.Equal("", result);
    }
}
