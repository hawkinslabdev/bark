using System.Globalization;
using System.Text.RegularExpressions;
using Bark.Models;
using Bark.Services.Layout;
using Bark.Services.Theming;

namespace Bark.Tests;

public sealed class ThemeRegistryTests
{
    [Fact]
    public void Resolve_NullOrBlank_ReturnsDefault()
    {
        Assert.Same(ThemeRegistry.Default, ThemeRegistry.Resolve(null));
        Assert.Same(ThemeRegistry.Default, ThemeRegistry.Resolve("   "));
    }

    [Fact]
    public void Resolve_UnknownName_FallsBackToDefaultWithoutThrowing()
    {
        Assert.Same(ThemeRegistry.Default, ThemeRegistry.Resolve("no-such-theme"));
    }

    [Theory]
    [InlineData("forest")]
    [InlineData("FOREST")]
    [InlineData("  Forest  ")]
    public void Resolve_IsCaseAndWhitespaceInsensitive(string name)
    {
        Assert.Equal("forest", ThemeRegistry.Resolve(name).Name);
    }

    [Fact]
    public void All_NamesAreUniqueAndKebabCase()
    {
        var names = ThemeRegistry.All.Select(t => t.Name).ToList();
        Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(names, n => Assert.Matches("^[a-z][a-z0-9-]*$", n));
    }

    [Theory]
    [InlineData("signal-dark", "signal")]
    [InlineData("Blueprint-Grid", "blueprint")]
    [InlineData("forest-ledger", "forest")]
    [InlineData("deep-space", "space")]
    public void Resolve_RenamedTheme_StillResolves(string old, string current) =>
        Assert.Equal(current, ThemeRegistry.Resolve(old).Name);

    [Fact]
    public void Default_IsTheDefaultTheme() => Assert.Equal("default", ThemeRegistry.Default.Name);
}

public sealed class ThemeTokenTests
{
    public static TheoryData<string> ThemeNames() => [.. ThemeRegistry.All.Select(t => t.Name)];

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void RequiredPaletteKeys_PresentInBothModes(string name)
    {
        var theme = ThemeRegistry.Resolve(name);
        foreach (var key in IBarkTheme.RequiredPaletteKeys)
        {
            Assert.True(theme.LightTokens.ContainsKey(key), $"{name} light is missing {key}");
            Assert.True(theme.DarkTokens.ContainsKey(key), $"{name} dark is missing {key}");
        }
    }

    /// <summary>A literal declared in one mode bleeds into the other; alias values re-resolve, so only literals need parity.</summary>
    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void EveryLiteralColourHasBothModes(string name)
    {
        var theme = ThemeRegistry.Resolve(name);
        var lightLiterals = theme.LightTokens.Where(t => IsLiteral(t.Value)).Select(t => t.Key);
        var darkLiterals = theme.DarkTokens.Where(t => IsLiteral(t.Value)).Select(t => t.Key);

        Assert.Empty(lightLiterals.Except(theme.DarkTokens.Keys));
        Assert.Empty(darkLiterals.Except(theme.LightTokens.Keys));
    }

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void GeneratedColoursStayInSrgbGamut(string name)
    {
        var theme = ThemeRegistry.Resolve(name);
        var colours = theme.LightTokens.Concat(theme.DarkTokens).Where(t => t.Value.StartsWith("oklch(", StringComparison.Ordinal));
        Assert.All(colours, t => Assert.True(Oklch.Parse(t.Value).InSrgbGamut, $"{name} {t.Key} {t.Value} is outside sRGB"));
    }

    private static bool IsLiteral(string value) =>
        value.StartsWith('#') || value.StartsWith("oklch(", StringComparison.Ordinal);
}

public sealed class ThemeContrastTests
{
    public static TheoryData<string> ThemeNames() => [.. ThemeRegistry.All.Select(t => t.Name)];

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void LightMode_MeetsContrastFloor(string name) =>
        AssertContrastFloor(ThemeRegistry.Resolve(name), dark: false);

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void DarkMode_MeetsContrastFloor(string name) =>
        AssertContrastFloor(ThemeRegistry.Resolve(name), dark: true);

    private static void AssertContrastFloor(IBarkTheme theme, bool dark)
    {
        var tokens = dark
            ? Merge(theme.LightTokens, theme.DarkTokens)
            : theme.LightTokens;

        var bg = tokens["--bg-color"];
        var mode = dark ? "dark" : "light";

        AssertRatio(tokens["--text-color"], bg, 4.5, $"{theme.Name}/{mode} body text");
        AssertRatio(tokens["--text-muted"], bg, 4.5, $"{theme.Name}/{mode} muted text");
        AssertRatio(tokens["--accent"], bg, 4.5, $"{theme.Name}/{mode} accent");
        AssertRatio(tokens["--border"], bg, 1.2, $"{theme.Name}/{mode} hairline border");
        AssertRatio(tokens["--text-color"], tokens["--sidebar-bg"], 4.5, $"{theme.Name}/{mode} text on sidebar");
        AssertRatio(tokens["--text-color"], tokens["--code-bg"], 4.5, $"{theme.Name}/{mode} text on code");
        AssertRatio(tokens["--text-color"], tokens["--accent-light"], 4.5, $"{theme.Name}/{mode} text on accent tint");
        AssertRatio(tokens["--accent"], tokens["--accent-light"], 4.5, $"{theme.Name}/{mode} accent on accent tint");

        foreach (var alert in new[] { "--accent", "--alert-note", "--alert-tip", "--alert-important", "--alert-warning", "--alert-caution" })
        {
            if (!tokens.TryGetValue(alert, out var colour))
                continue;
            AssertRatio(colour, bg, 4.5, $"{theme.Name}/{mode} {alert}");
            if (colour.StartsWith("oklch(", StringComparison.Ordinal) && bg.StartsWith("oklch(", StringComparison.Ordinal))
            {
                var tint = MixOklab(Oklch.Parse(colour), Oklch.Parse(bg), dark ? 0.14 : 0.08).ToString();
                AssertRatio(colour, tint, 4.5, $"{theme.Name}/{mode} {alert} on its callout tint");
                AssertRatio(tokens["--text-color"], tint, 4.5, $"{theme.Name}/{mode} text on {alert} callout tint");
            }
        }

        // Only when both are literals; otherwise they alias tokens the assertions above already cover.
        if (tokens.TryGetValue("--promo-bg", out var promoBg) && IsLiteral(promoBg)
            && tokens.TryGetValue("--promo-text", out var promoText) && IsLiteral(promoText))
            AssertRatio(promoText, promoBg, 4.5, $"{theme.Name}/{mode} promo bar");
    }

    private static Dictionary<string, string> Merge(
        IReadOnlyDictionary<string, string> baseTokens,
        IReadOnlyDictionary<string, string> overrides)
    {
        var merged = new Dictionary<string, string>(baseTokens, StringComparer.Ordinal);
        foreach (var (key, value) in overrides)
            merged[key] = value;
        return merged;
    }

    private static bool IsLiteral(string value) =>
        value.StartsWith('#') || value.StartsWith("oklch(", StringComparison.Ordinal);

    private static Oklch MixOklab(Oklch a, Oklch b, double amountOfA)
    {
        static (double L, double A, double B) Lab(Oklch c) =>
            (c.L, c.C * Math.Cos(c.H * Math.PI / 180), c.C * Math.Sin(c.H * Math.PI / 180));

        var (la, aa, ba) = Lab(a);
        var (lb, ab, bb) = Lab(b);
        var l = (la * amountOfA) + (lb * (1 - amountOfA));
        var x = (aa * amountOfA) + (ab * (1 - amountOfA));
        var y = (ba * amountOfA) + (bb * (1 - amountOfA));
        return new Oklch(l, Math.Sqrt((x * x) + (y * y)), Math.Atan2(y, x) * 180 / Math.PI);
    }

    private static void AssertRatio(string foreground, string background, double floor, string label)
    {
        var ratio = ContrastRatio(foreground, background);
        Assert.True(ratio >= floor, $"{label}: {foreground} on {background} is {ratio:F2}:1, needs {floor}:1");
    }

    internal static double ContrastRatio(string a, string b)
    {
        var (high, low) = (RelativeLuminance(a), RelativeLuminance(b));
        if (low > high)
            (high, low) = (low, high);
        return (high + 0.05) / (low + 0.05);
    }

    private static double RelativeLuminance(string colour)
    {
        if (colour.StartsWith("oklch(", StringComparison.Ordinal))
            return Oklch.Parse(colour).RelativeLuminance;

        var value = colour.TrimStart('#');
        if (value.Length == 3)
            value = string.Concat(value.Select(c => new string(c, 2)));

        var r = Channel(value[..2]);
        var g = Channel(value.Substring(2, 2));
        var b = Channel(value.Substring(4, 2));
        return (0.2126 * r) + (0.7152 * g) + (0.0722 * b);

        static double Channel(string pair)
        {
            var srgb = int.Parse(pair, NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
            return srgb <= 0.03928 ? srgb / 12.92 : Math.Pow((srgb + 0.055) / 1.055, 2.4);
        }
    }
}

public sealed partial class ThemeCssIntegrityTests
{
    /// <summary>Set inline on the elements that read them, not by any theme.</summary>
    private static readonly string[] ExternallyDefined =
        ["--shiki-light", "--shiki-dark", "--tip-shift", "--icon"];

    public static TheoryData<string> ThemeNames() => [.. ThemeRegistry.All.Select(t => t.Name)];

    /// <summary>A theme dropping a variable the base stylesheet reads renders as an unstyled element, not an error.</summary>
    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void EveryReferencedVariableIsDefined(string name)
    {
        var css = RenderThemeCss(ThemeRegistry.Resolve(name));

        var defined = DefinitionPattern().Matches(css).Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
        var referenced = ReferencePattern().Matches(css).Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
        referenced.ExceptWith(ExternallyDefined);
        referenced.ExceptWith(defined);

        Assert.Empty(referenced);
    }

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void ThemeCssIsServedFromTheExternalStylesheet(string name)
    {
        var html = Render(ThemeRegistry.Resolve(name), nonce: "test-nonce");
        Assert.Equal(0, StylePattern().Count(html));
        Assert.Contains("<link rel=\"stylesheet\" href=\"/bark.css?v=", html, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void ThemeClassIsOnTheHtmlElement(string name)
    {
        var html = Render(ThemeRegistry.Resolve(name));
        Assert.Contains($"class=\"theme-{name}\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void DarkModeDisabled_EmitsNoDarkBlock()
    {
        var css = ThemeCssBuilder.BuildTokenCss(ThemeRegistry.Default, enableDarkMode: false);
        Assert.DoesNotContain("prefers-color-scheme", css, StringComparison.Ordinal);
        Assert.DoesNotContain("data-theme", css, StringComparison.Ordinal);
    }

    [Fact]
    public void DarkModeEnabled_EmitsBothOsQueryAndExplicitToggle()
    {
        var css = ThemeCssBuilder.BuildTokenCss(ThemeRegistry.Default, enableDarkMode: true);
        Assert.Contains("@media (prefers-color-scheme: dark)", css, StringComparison.Ordinal);
        Assert.Contains(":root[data-theme=\"dark\"]", css, StringComparison.Ordinal);
    }

    private static string Render(IBarkTheme theme, string? nonce = null) =>
        LayoutProvider.GetLayout(
            title: "Test",
            content: "<p>body</p>",
            navigationHtml: "",
            tocHtml: null,
            breadcrumbHtml: "",
            paginationHtml: "",
            nonce: nonce,
            theme: theme);

    /// <summary>The stylesheet the layout links to, built exactly as the /bark.css endpoint builds it.</summary>
    private static string RenderThemeCss(IBarkTheme theme) =>
        LayoutProvider.GetStylesAsset(
            ThemeCssBuilder.BuildTokenCss(theme, enableDarkMode: true),
            theme.ComponentCss,
            basePath: "").Body;

    [GeneratedRegex(@"(--[a-z0-9-]+)\s*:")]
    private static partial Regex DefinitionPattern();

    [GeneratedRegex(@"var\((--[a-z0-9-]+)")]
    private static partial Regex ReferencePattern();

    [GeneratedRegex(@"<style[^>]*>")]
    private static partial Regex StylePattern();

}

public sealed class ThemeSelectionTests
{
    [Theory]
    [InlineData("forest dark", "forest", ThemeMode.Dark)]
    [InlineData("forest light", "forest", ThemeMode.Light)]
    [InlineData("dark forest", "forest", ThemeMode.Dark)]
    [InlineData("forest", "forest", ThemeMode.Auto)]
    [InlineData("dark", null, ThemeMode.Dark)]
    [InlineData(null, null, ThemeMode.Auto)]
    [InlineData("  ", null, ThemeMode.Auto)]
    public void Split_SeparatesNameFromMode(string? value, string? expectedName, ThemeMode expectedMode)
    {
        var (name, mode) = ThemeSelection.Split(value);
        Assert.Equal(expectedName, name);
        Assert.Equal(expectedMode, mode);
    }

    [Theory]
    [InlineData(ThemeMode.Dark, "data-theme=\"dark\"")]
    [InlineData(ThemeMode.Light, "data-theme=\"light\"")]
    public void GetLayout_ForcedMode_SetsHtmlAttributeAndHidesToggle(ThemeMode mode, string expectedAttr)
    {
        var html = LayoutProvider.GetLayout(
            title: "Test",
            content: "<p>body</p>",
            navigationHtml: "",
            tocHtml: null,
            breadcrumbHtml: "",
            paginationHtml: "",
            theme: ThemeRegistry.Default,
            themeMode: mode);

        Assert.Contains(expectedAttr, html, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"theme-toggle\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void GetLayout_AutoMode_ShowsToggleAndNoForcedAttribute()
    {
        var html = LayoutProvider.GetLayout(
            title: "Test",
            content: "<p>body</p>",
            navigationHtml: "",
            tocHtml: null,
            breadcrumbHtml: "",
            paginationHtml: "",
            theme: ThemeRegistry.Default,
            themeMode: ThemeMode.Auto);

        Assert.Contains("id=\"theme-toggle\"", html, StringComparison.Ordinal);
        var htmlTag = html.Substring(html.IndexOf("<html", StringComparison.Ordinal), 200);
        Assert.DoesNotContain("data-theme=", htmlTag, StringComparison.Ordinal);
    }
}
