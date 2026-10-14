using Bark.Configuration;
using Bark.Models;
using Bark.Services;
using Bark.Services.Layout;
using Bark.Services.Theming;

namespace Bark.Endpoints;

internal static class AssetEndpoints
{
    public static IEndpointRouteBuilder MapAssetEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapMethods("/bark.css", HttpVerbs.GetAndHead, GetStylesheet);
        app.MapMethods("/bark.js", HttpVerbs.GetAndHead, GetScript);
        return app;
    }

    private static async Task GetStylesheet(HttpContext ctx, DocumentationService docs, ThemeOptions themeOptions, PageRequestSettings settings)
    {
        var config = docs.SiteConfig;
        var (themeName, _) = ThemeSelection.Split(settings.CliTheme ?? themeOptions.Name ?? config?.Theme);
        var theme = ThemeRegistry.Resolve(themeName);
        var enableDarkMode = ThemeProvider.UseDarkMode(themeOptions);
        var themeTokenCss = ThemeCssBuilder.BuildTokenCss(theme, enableDarkMode);

        var asset = LayoutProvider.GetStylesAsset(themeTokenCss, theme.ComponentCss, settings.BasePath);
        WriteCacheHeaders(ctx);
        ctx.Response.ContentType = "text/css; charset=utf-8";
        await ctx.Response.WriteAsync(asset.Body);
    }

    private static async Task GetScript(HttpContext ctx, DocumentationService docs, ThemeOptions themeOptions, DocsOptions docsOptions, PageRequestSettings settings)
    {
        var config = docs.SiteConfig;
        var (themeName, themeMode) = ThemeSelection.Split(settings.CliTheme ?? themeOptions.Name ?? config?.Theme);
        var enableDarkMode = ThemeProvider.UseDarkMode(themeOptions) && themeMode == ThemeMode.Auto;

        var localization = docs.Locales.For(ctx.Request.Query["locale"].ToString());
        var isRootLocale = string.Equals(localization.Code, docs.RootLocale, StringComparison.OrdinalIgnoreCase);
        var asset = LayoutProvider.GetScriptsAsset(docsOptions.EnableHotReload, enableDarkMode, docs.BuildVersion, settings.BasePath, docsOptions.IsStaticExport, localization, isRootLocale);
        WriteCacheHeaders(ctx);
        ctx.Response.ContentType = "text/javascript; charset=utf-8";
        await ctx.Response.WriteAsync(asset.Body);
    }

    private static void WriteCacheHeaders(HttpContext ctx) =>
        ctx.Response.Headers.CacheControl = ctx.Request.Query.ContainsKey("v")
            ? "public,max-age=31536000,immutable"
            : "no-cache";
}
