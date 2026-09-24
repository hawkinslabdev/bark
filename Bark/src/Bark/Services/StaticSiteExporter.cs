using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Bark.Configuration;
using Bark.Serialization;
using Bark.Services.Rendering;

namespace Bark.Services;

public static class StaticSiteExporter
{
    public static async Task RunAsync(WebApplication app, string outputDir, string? baseUrl, CancellationToken cancellationToken)
    {
        app.Urls.Clear();
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync(cancellationToken);

        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.First();

        using var client = new HttpClient { BaseAddress = new Uri(address) };

        var docs = app.Services.GetRequiredService<DocumentationService>();
        var pages = await docs.GetAllPagesAsync(cancellationToken);

        Directory.CreateDirectory(outputDir);

        var originPrefix = address.TrimEnd('/');
        var publicPrefix = string.IsNullOrEmpty(baseUrl) ? null : baseUrl.TrimEnd('/');
        var basePath = app.Services.GetRequiredService<PageRequestSettings>().BasePath;

        foreach (var path in ExportPaths(pages.Select(p => p.Path), docs.LocaleCodes, docs.RootLocale))
        {
            var requestPath = path == "index" ? "/" : $"/{path}";
            using var response = await client.GetAsync(requestPath, cancellationToken);
            var html = WithCspMeta(
                await response.Content.ReadAsStringAsync(cancellationToken), response);
            if (publicPrefix is not null)
                html = html.Replace(originPrefix, publicPrefix);
            var depth = path == "index" ? 0 : path.Split('/').Length;
            var relativePrefix = string.Concat(Enumerable.Repeat("../", depth));
            html = html.Replace($"{basePath}/bark.css", $"{relativePrefix}bark.css")
                       .Replace($"{basePath}/bark.js", $"{relativePrefix}bark.js");
            var targetFile = path == "index"
                ? Path.Combine(outputDir, "index.html")
                : Path.Combine(outputDir, path, "index.html");
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
            await File.WriteAllTextAsync(targetFile, html, cancellationToken);
        }

        foreach (var extra in new[] { "robots.txt", "llms.txt", "sitemap.xml" })
        {
            var content = await client.GetStringAsync($"/{extra}", cancellationToken);
            if (publicPrefix is not null)
                content = content.Replace(originPrefix, publicPrefix);
            await File.WriteAllTextAsync(Path.Combine(outputDir, extra), content, cancellationToken);
        }

        foreach (var asset in new[] { "bark.css", "bark.js" })
        {
            using var assetResponse = await client.GetAsync($"/{asset}", cancellationToken);
            var bytes = await assetResponse.Content.ReadAsByteArrayAsync(cancellationToken);
            await File.WriteAllBytesAsync(Path.Combine(outputDir, asset), bytes, cancellationToken);
        }

        using var notFoundResponse = await client.GetAsync("/__bark_export_404__", cancellationToken);
        var notFoundHtml = WithCspMeta(
            await notFoundResponse.Content.ReadAsStringAsync(cancellationToken), notFoundResponse);
        await File.WriteAllTextAsync(Path.Combine(outputDir, "404.html"), notFoundHtml, cancellationToken);

        // Search has no server on a static host, so ship the prebuilt index the client queries directly.
        var searchIndex = docs.GetSearchIndexExport();
        var searchJson = JsonSerializer.Serialize(searchIndex, BarkJsonContext.Default.SearchIndexExport);
        await File.WriteAllTextAsync(Path.Combine(outputDir, "search-index.json"), searchJson, cancellationToken);

        foreach (var localeCode in docs.LocaleCodes)
        {
            var localeDir = Path.Combine(outputDir, localeCode);
            Directory.CreateDirectory(localeDir);
            var localeJson = JsonSerializer.Serialize(docs.GetSearchIndexExport(localeCode), BarkJsonContext.Default.SearchIndexExport);
            await File.WriteAllTextAsync(Path.Combine(localeDir, "search-index.json"), localeJson, cancellationToken);
        }
        app.Logger.LogInformation(
            "Static search index written: {Docs} docs, {Bytes:N0} bytes",
            searchIndex.Docs.Count, System.Text.Encoding.UTF8.GetByteCount(searchJson));

        CopyStaticAssets(app.Environment.WebRootPath, outputDir);

        // The /assets route is served from docs/assets at runtime, outside wwwroot; mirror it into the export.
        var assetsDir = Path.Combine(Path.GetFullPath(app.Services.GetRequiredService<DocsOptions>().RootPath), "assets");
        CopyStaticAssets(assetsDir, Path.Combine(outputDir, "assets"), AssetContentTypes.IsAllowed);

        await app.StopAsync(cancellationToken);
    }

    // A locale tree only holds the pages actually translated; the server falls back to the root locale for the rest.
    // A static host has no fallback, so every root page is also written under each locale prefix.
    internal static IReadOnlyList<string> ExportPaths(
        IEnumerable<string> pagePaths, IReadOnlyList<string> localeCodes, string rootLocale)
    {
        var paths = pagePaths.ToList();
        var seen = new HashSet<string>(paths, StringComparer.Ordinal);
        var rootPages = paths.Where(p => LocaleRouting.LocaleOf(p, localeCodes, rootLocale) == rootLocale).ToList();

        foreach (var code in localeCodes)
            foreach (var localized in rootPages.Select(p => LocaleRouting.Localize(code, p)))
                if (seen.Add(localized))
                    paths.Add(localized);

        return paths;
    }

    // CSP only ever existed as a response header, so a published export had no policy at all.
    // frame-ancestors is ignored in a meta policy and only logs a console warning, so it is dropped.
    private static string WithCspMeta(string html, HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Content-Security-Policy", out var values))
            return html;

        var policy = string.Join(";", values.First().Split(';')
            .Where(d => !d.TrimStart().StartsWith("frame-ancestors", StringComparison.OrdinalIgnoreCase)));

        var head = html.IndexOf("<head>", StringComparison.OrdinalIgnoreCase);
        if (head < 0) return html;

        var meta = $"\n    <meta http-equiv=\"Content-Security-Policy\" content=\"{WebUtility.HtmlEncode(policy)}\">";
        return html.Insert(head + "<head>".Length, meta);
    }

    // A static host serves what it is given, so the export applies the same media allowlist the runtime does.
    private static void CopyStaticAssets(string sourceRoot, string outputDir, Func<string, bool>? allow = null)
    {
        if (!Directory.Exists(sourceRoot)) return;

        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*", ContentLinks.NoLinks))
        {
            if (allow is not null && !allow(file)) continue;
            var relative = Path.GetRelativePath(sourceRoot, file);
            var dest = Path.Combine(outputDir, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest, overwrite: true);
        }
    }
}
