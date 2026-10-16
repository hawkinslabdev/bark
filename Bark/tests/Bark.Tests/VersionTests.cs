using System.Net;
using Bark.Models;
using Bark.Services;
using Bark.Services.Rendering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Bark.Tests;

internal static class VersionConfigs
{
    public static VersionsConfig Folders(params string[] folders) => new()
    {
        Items = [new() { Text = "v2.0", Link = "/" }, .. folders.Select(folder => new VersionItem { Text = folder, Link = $"/{folder}/" })],
        Folders = new HashSet<string>(folders, StringComparer.OrdinalIgnoreCase),
    };
}

public sealed class VersionWebApplicationFactory : WebApplicationFactory<Program>
{
    public string DocsDir { get; } =
        Path.Combine(Path.GetTempPath(), "bark-version-integration-" + Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(Path.Combine(DocsDir, "guide"));
        Directory.CreateDirectory(Path.Combine(DocsDir, "v1.0", "guide"));

        File.WriteAllText(Path.Combine(DocsDir, "index.md"), "# Latest home\n");
        File.WriteAllText(Path.Combine(DocsDir, "guide", "install.md"), "# Install\n\nLatest install zebra.\n\nSee [deploy](/guide/deploy).\n");
        File.WriteAllText(Path.Combine(DocsDir, "guide", "deploy.md"), "# Deploy\n\nLatest deploy page.\n");
        File.WriteAllText(Path.Combine(DocsDir, "v1.0", "index.md"), "# Old home\n");
        File.WriteAllText(Path.Combine(DocsDir, "v1.0", "guide", "install.md"), "# Install\n\nOld install giraffe.\n\nSee [steps](/guide/install#steps) and [sheet](/assets/sheet.pdf).\n");
        File.WriteAllText(Path.Combine(DocsDir, "v1.0", "guide", "setup.md"), "# Setup\n\nOld setup page.\n");
        File.WriteAllText(Path.Combine(DocsDir, "config.json"), """
            {
              "lang": "en",
              "versions": {
                "items": [
                  { "text": "v2.0", "link": "/" },
                  {
                    "text": "v1.0",
                    "link": "/v1.0/",
                    "sidebar": {
                      "/guide/": [ { "title": "Old guide", "items": [ { "title": "Setup", "path": "guide/setup" } ] } ]
                    }
                  }
                ]
              },
              "sidebar": {
                "/guide/": [ { "title": "Guide", "items": [ { "title": "Deploy", "path": "guide/deploy" } ] } ]
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

public sealed class VersionTests : IClassFixture<VersionWebApplicationFactory>
{
    private readonly VersionWebApplicationFactory _factory;

    public VersionTests(VersionWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task VersionPage_KeepsSiteLangAndLinksWithinTheVersion()
    {
        var html = await _factory.CreateClient().GetStringAsync("/v1.0/guide/install");

        Assert.Contains("Old install giraffe", html);
        Assert.Contains("lang=\"en\"", html);
        Assert.Contains("href=\"/v1.0/guide/install#steps\"", html);
        Assert.Contains("href=\"/assets/sheet.pdf\"", html);
        Assert.DoesNotContain("translation-notice--missing", html);
    }

    [Fact]
    public async Task VersionPage_MissingInTheVersion_Returns404()
    {
        var response = await _factory.CreateClient().GetAsync("/v1.0/guide/deploy");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task VersionSwitcher_LinksToTheSamePageOrTheVersionHome()
    {
        var client = _factory.CreateClient();
        var old = await client.GetStringAsync("/v1.0/guide/install");
        var latest = await client.GetStringAsync("/guide/deploy");

        Assert.Contains("id=\"version-toggle\"", old);
        Assert.Contains("href=\"/guide/install/\">v2.0</a>", old);
        Assert.Contains("aria-current=\"true\" role=\"menuitem\" href=\"/v1.0/guide/install/\">v1.0</a>", old);
        Assert.Contains("href=\"/v1.0/\">v1.0</a>", latest);
        Assert.DoesNotContain("id=\"locale-toggle\"", old);
    }

    [Fact]
    public async Task VersionNotice_OnOlderVersionsOnly()
    {
        var client = _factory.CreateClient();
        var old = await client.GetStringAsync("/v1.0/guide/install");
        var latest = await client.GetStringAsync("/guide/install");

        Assert.Contains("version-notice", old);
        Assert.Contains("<a href=\"/guide/install/\">", old);
        Assert.DoesNotContain("version-notice", latest);
    }

    [Fact]
    public async Task VersionSidebar_ReplacesTheRootSidebar()
    {
        var client = _factory.CreateClient();
        var old = await client.GetStringAsync("/v1.0/guide/install");
        var latest = await client.GetStringAsync("/guide/install");

        Assert.Contains("Old guide", old);
        Assert.DoesNotContain("/v1.0/guide/deploy/", old);
        Assert.Contains("href=\"/guide/deploy/\"", latest);
    }

    [Fact]
    public async Task Search_IsScopedPerVersion()
    {
        var client = _factory.CreateClient();
        var old = await client.GetStringAsync("/api/search?q=giraffe&locale=v1.0");
        var latest = await client.GetStringAsync("/api/search?q=giraffe");

        Assert.Contains("v1.0/guide/install", old);
        Assert.Equal("[]", latest);
    }

    [Fact]
    public void ExportPaths_DoesNotPadVersionTrees()
    {
        var config = new Config
        {
            Lang = "en",
            Locales = new() { ["root"] = new(), ["nl"] = new() },
            Versions = VersionConfigs.Folders("v1.0"),
        };
        var paths = StaticSiteExporter.ExportPaths(["index", "guide/deploy", "v1.0", "v1.0/guide/install", "nl/v1.0/guide/install"], config);

        Assert.Contains("nl/guide/deploy", paths);
        Assert.DoesNotContain("v1.0/guide/deploy", paths);
        Assert.Contains("nl/v1.0", paths);
        Assert.Single(paths, p => p == "nl/v1.0/guide/install");
        Assert.DoesNotContain("nl/v1.0/guide/deploy", paths);
        Assert.DoesNotContain("nl/v1.0/v1.0/guide/install", paths);
    }

    [Fact]
    public void VersionSwitcher_MixesFolderVersionsAndDeploys()
    {
        var config = new Config
        {
            Versions = new VersionsConfig
            {
                Items =
                [
                    new() { Text = "v2.0", Link = "/" },
                    new() { Text = "v1.0", Link = "/v1.0/" },
                    new() { Text = "v0.9", Link = "/v0.9/" },
                    new() { Text = "v0.1", Link = "https://old.example.com/" },
                ],
                Folders = new HashSet<string>(["v1.0"], StringComparer.OrdinalIgnoreCase),
            },
        };

        var html = VersionSwitcherRenderer.Build(config, "v1.0", "v1.0/guide/install", "", Localization.Default, _ => true);

        Assert.Contains("href=\"/guide/install/\">v2.0</a>", html);
        Assert.Contains("aria-current=\"true\" role=\"menuitem\" href=\"/v1.0/guide/install/\">v1.0</a>", html);
        Assert.Contains("href=\"/v0.9/\">v0.9</a>", html);
        Assert.Contains("href=\"https://old.example.com/\">v0.1</a>", html);
        Assert.Equal(["v1.0"], LocaleRouting.VersionCodes(config));
    }

    [Fact]
    public void Routing_VersionHasNoFallback()
    {
        var config = new Config { Versions = VersionConfigs.Folders("v1.0") };

        var route = LocaleRouting.Resolve(config, "v1.0/guide/deploy");

        Assert.Equal("v1.0", route.Prefix);
        Assert.Null(route.FallbackPath);
    }

    [Fact]
    public void VersionLocaleCollisions_ReturnsVersionKeysMatchingLocaleOrRootCodes()
    {
        var config = new Config
        {
            Lang = "en",
            Locales = new() { ["root"] = new(), ["nl"] = new() },
            Versions = VersionConfigs.Folders("NL", "en", "v1.0"),
        };

        Assert.Equal(["nl", "en"], LocaleRouting.VersionLocaleCollisions(config));
    }

    [Fact]
    public void VersionLocaleCollisions_EmptyWithoutOverlap()
    {
        var config = new Config
        {
            Locales = new() { ["root"] = new(), ["nl"] = new() },
            Versions = VersionConfigs.Folders("v1.0"),
        };

        Assert.Empty(LocaleRouting.VersionLocaleCollisions(config));
    }
}
