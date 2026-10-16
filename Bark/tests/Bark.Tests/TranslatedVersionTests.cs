using System.Net;
using Bark.Models;
using Bark.Services;
using Bark.Services.Rendering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Bark.Tests;

public sealed class TranslatedVersionWebApplicationFactory : WebApplicationFactory<Program>
{
    public string DocsDir { get; } =
        Path.Combine(Path.GetTempPath(), "bark-translated-version-" + Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(Path.Combine(DocsDir, "guide"));
        Directory.CreateDirectory(Path.Combine(DocsDir, "v1.0", "guide"));
        Directory.CreateDirectory(Path.Combine(DocsDir, "nl", "guide"));
        Directory.CreateDirectory(Path.Combine(DocsDir, "nl", "v1.0", "guide"));
        Directory.CreateDirectory(Path.Combine(DocsDir, "locale"));

        File.WriteAllText(Path.Combine(DocsDir, "index.md"), "# Latest home\n");
        File.WriteAllText(Path.Combine(DocsDir, "guide", "install.md"), "# Install\n\nLatest install.\n");
        File.WriteAllText(Path.Combine(DocsDir, "guide", "deploy.md"), "# Deploy\n\nLatest deploy.\n");
        File.WriteAllText(Path.Combine(DocsDir, "v1.0", "index.md"), "# Old home\n");
        File.WriteAllText(Path.Combine(DocsDir, "v1.0", "guide", "install.md"), "# Install\n\nOld install giraffe.\n");
        File.WriteAllText(Path.Combine(DocsDir, "v1.0", "guide", "setup.md"), "# Setup\n\nOld setup okapi.\n");
        File.WriteAllText(Path.Combine(DocsDir, "nl", "index.md"), "# Nieuwste start\n");
        File.WriteAllText(Path.Combine(DocsDir, "nl", "guide", "install.md"), "# Installeren\n\nNieuwste installatie.\n");
        File.WriteAllText(Path.Combine(DocsDir, "nl", "v1.0", "index.md"), "# Oude start\n");
        File.WriteAllText(Path.Combine(DocsDir, "nl", "v1.0", "guide", "install.md"),
            "# Installeren\n\nOude installatie giraf.\n\nZie [setup](/guide/setup), [nl setup](/nl/guide/setup) en [blad](/assets/sheet.pdf).\n");
        File.WriteAllText(Path.Combine(DocsDir, "locale", "nl.json"), """{ "versionOutdated": "Deze pagina beschrijft een oudere versie." }""");
        File.WriteAllText(Path.Combine(DocsDir, "config.json"), """
            {
              "lang": "en",
              "locales": {
                "root": { "label": "English" },
                "nl": { "label": "Nederlands", "lang": "nl-NL" }
              },
              "versions": {
                "items": [ { "text": "v2.0", "link": "/" }, { "text": "v1.0", "link": "/v1.0/" } ]
              }
            }
            """);

        builder.UseSetting("urls", "http://127.0.0.1:0");
        builder.UseSetting("Docs:RootPath", DocsDir);
        builder.UseSetting("Docs:EnableHotReload", "false");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (Directory.Exists(DocsDir))
            Directory.Delete(DocsDir, true);
    }
}

public sealed class TranslatedVersionTests : IClassFixture<TranslatedVersionWebApplicationFactory>
{
    private readonly TranslatedVersionWebApplicationFactory _factory;

    public TranslatedVersionTests(TranslatedVersionWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task TranslatedVersionPage_UsesTheLocaleLanguageAndTheVersionTree()
    {
        var html = await _factory.CreateClient().GetStringAsync("/nl/v1.0/guide/install");

        Assert.Contains("Oude installatie giraf", html);
        Assert.Contains("lang=\"nl-NL\"", html);
        Assert.Contains("Deze pagina beschrijft een oudere versie.", html);
        Assert.Contains("href=\"/nl/v1.0/guide/setup\"", html);
        Assert.DoesNotContain("href=\"/nl/v1.0/nl/guide/setup\"", html);
        Assert.Contains("href=\"/assets/sheet.pdf\"", html);
        Assert.DoesNotContain("translation-notice--missing", html);
    }

    [Fact]
    public async Task TranslatedVersionPage_Untranslated_FallsBackToTheSameVersion()
    {
        var response = await _factory.CreateClient().GetAsync("/nl/v1.0/guide/setup");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Old setup okapi", html);
        Assert.Contains("translation-notice--missing", html);
        Assert.Contains("href=\"/v1.0/guide/setup/\"", html);
    }

    [Fact]
    public async Task TranslatedVersionPage_MissingInTheVersion_Returns404()
    {
        var response = await _factory.CreateClient().GetAsync("/nl/v1.0/guide/deploy");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task VersionSwitcher_KeepsTheLocale()
    {
        var html = await _factory.CreateClient().GetStringAsync("/nl/guide/install");

        Assert.Contains("id=\"version-toggle\"", html);
        Assert.Contains("href=\"/nl/v1.0/guide/install/\">v1.0</a>", html);
        Assert.Contains("aria-current=\"true\" role=\"menuitem\" href=\"/nl/guide/install/\">v2.0</a>", html);
    }

    [Fact]
    public async Task VersionSwitcher_PageMissingInTheVersion_LinksToTheTranslatedVersionHome()
    {
        var html = await _factory.CreateClient().GetStringAsync("/nl/guide/deploy");

        Assert.Contains("href=\"/nl/v1.0/\">v1.0</a>", html);
    }

    [Fact]
    public async Task LocaleSwitcher_KeepsTheVersion()
    {
        var html = await _factory.CreateClient().GetStringAsync("/v1.0/guide/install");

        Assert.Contains("id=\"locale-toggle\"", html);
        Assert.Contains("href=\"/nl/v1.0/guide/install/\"", html);
        Assert.Contains("href=\"/v1.0/guide/install/\" aria-current=\"true\"", html);
    }

    [Fact]
    public async Task VersionNotice_OnATranslatedVersion_LinksToTheLatestTranslation()
    {
        var html = await _factory.CreateClient().GetStringAsync("/nl/v1.0/guide/install");

        Assert.Contains("version-notice", html);
        Assert.Contains("<a href=\"/nl/guide/install/\">", html);
    }

    [Fact]
    public async Task Search_IsScopedPerTranslatedVersion()
    {
        var client = _factory.CreateClient();
        var translated = await client.GetStringAsync("/api/search?q=giraf&locale=" + Uri.EscapeDataString("nl/v1.0"));
        var latest = await client.GetStringAsync("/api/search?q=giraf&locale=nl");

        Assert.Contains("nl/v1.0/guide/install", translated);
        Assert.Equal("[]", latest);
    }

    [Fact]
    public async Task Sitemap_ListsTheLatestVersionOnly()
    {
        var xml = await _factory.CreateClient().GetStringAsync("/sitemap.xml");

        Assert.Contains("hreflang=\"nl-NL\" href=\"/nl/guide/install/\"", xml);
        Assert.DoesNotContain("/v1.0/", xml);
    }

    [Fact]
    public async Task OlderFolderVersions_AreNotIndexed()
    {
        var client = _factory.CreateClient();
        var old = await client.GetStringAsync("/nl/v1.0/guide/install");
        var latest = await client.GetStringAsync("/nl/guide/install");

        Assert.Contains("<meta name=\"robots\" content=\"noindex\">", old);
        Assert.DoesNotContain("noindex", latest);
    }

    [Fact]
    public async Task LocaleNavigation_ExcludesTheTranslatedVersionTree()
    {
        _ = _factory.CreateClient();
        var docs = _factory.Services.GetRequiredService<DocumentationService>();

        var nl = await docs.GetNavigationAsync("nl");
        var translatedVersion = await docs.GetNavigationAsync("nl/v1.0");

        Assert.DoesNotContain(Flatten(nl), path => path.StartsWith("nl/v1.0", StringComparison.Ordinal));
        Assert.Contains("nl/v1.0/guide/install", Flatten(translatedVersion));
    }

    [Fact]
    public void ExportPaths_PadsTranslatedVersionsFromTheSameVersion()
    {
        var config = new Config
        {
            Lang = "en",
            Locales = new() { ["root"] = new(), ["nl"] = new() },
            Versions = VersionConfigs.Folders("v1.0"),
        };

        var paths = StaticSiteExporter.ExportPaths(["index", "guide/deploy", "v1.0", "v1.0/guide/setup"], config);

        Assert.Contains("nl/v1.0", paths);
        Assert.Contains("nl/v1.0/guide/setup", paths);
        Assert.DoesNotContain("nl/v1.0/guide/deploy", paths);
    }

    [Theory]
    [InlineData("nl/v1.0/guide/setup", "nl/v1.0", "v1.0/guide/setup")]
    [InlineData("nl/v1.0", "nl/v1.0", "v1.0")]
    [InlineData("nl/guide/setup", "nl", "guide/setup")]
    [InlineData("v1.0/guide/setup", "v1.0", null)]
    [InlineData("guide/setup", "en", null)]
    public void Resolve_MatchesTheLongestTreePrefix(string path, string code, string? fallback)
    {
        var config = new Config
        {
            Lang = "en",
            Locales = new() { ["root"] = new(), ["nl"] = new() },
            Versions = VersionConfigs.Folders("v1.0"),
        };

        var route = LocaleRouting.Resolve(config, path);

        Assert.Equal(code, route.Code);
        Assert.Equal(fallback, route.FallbackPath);
    }

    private static IEnumerable<string> Flatten(NavigationNode node) =>
        (node.Path is { } path ? [path] : Array.Empty<string>()).Concat(node.Children.SelectMany(Flatten));
}
