using System.Net;
using Bark.Models;
using Bark.Services.Rendering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Bark.Tests;

public sealed class VersionSwitcherTests
{
    private static readonly List<TopNavItem> Versions =
    [
        new() { Text = "v2", Link = "/" },
        new() { Text = "v1", Link = "/v1/" },
        new() { Text = "v0", Link = "https://old.example.com/" },
    ];

    [Theory]
    [InlineData("", 0)]
    [InlineData("/v1", 1)]
    [InlineData("/V1", 1)]
    [InlineData("/bark", 0)]
    [InlineData("/bark/v1", 1)]
    [InlineData("/unknown", 0)]
    public void CurrentIndex_MatchesTheDeployByBasePathSuffix(string basePath, int expected) =>
        Assert.Equal(expected, VersionSwitcherRenderer.CurrentIndex(Versions, basePath));

    [Theory]
    [InlineData("", "/", "/v1/")]
    [InlineData("/v1", "/", "/v1/")]
    [InlineData("/bark", "/bark/", "/bark/v1/")]
    [InlineData("/bark/v1", "/bark/", "/bark/v1/")]
    public void Href_ResolvesAgainstTheSiteRoot(string basePath, string latest, string older)
    {
        var current = VersionSwitcherRenderer.CurrentIndex(Versions, basePath);

        Assert.Equal(latest, VersionSwitcherRenderer.Href(Versions, current, basePath, "/"));
        Assert.Equal(older, VersionSwitcherRenderer.Href(Versions, current, basePath, "/v1/"));
        Assert.Equal("https://old.example.com/", VersionSwitcherRenderer.Href(Versions, current, basePath, "https://old.example.com/"));
    }

    [Fact]
    public void Active_HonoursTheSwitchAndNeedsTwoVersions()
    {
        Assert.NotNull(VersionSwitcherRenderer.Active(new Config { Versions = new VersionsConfig { Items = Versions } }));
        Assert.Null(VersionSwitcherRenderer.Active(new Config { Versions = new VersionsConfig { Enabled = false, Items = Versions } }));
        Assert.Null(VersionSwitcherRenderer.Active(new Config { Versions = new VersionsConfig { Items = [Versions[0]] } }));
        Assert.Null(VersionSwitcherRenderer.Active(null));
    }

    [Fact]
    public void Switcher_MarksTheCurrentVersion()
    {
        var html = VersionSwitcherRenderer.Build(Versions, 1, "/bark/v1", Localization.Default);

        Assert.Contains("aria-label=\"Switch version (v1)\"", html);
        Assert.Contains("href=\"/bark/\"", html);
        Assert.Contains("locale-option--current\" aria-current=\"true\" role=\"menuitem\" href=\"/bark/v1/\"", html);
    }

    [Theory]
    [InlineData("", "v2", "/")]
    [InlineData("", "V2/guide/install", "/guide/install")]
    [InlineData("", "v2//evil.example", "/evil.example")]
    [InlineData("", "v2/\\evil.example", "/evil.example")]
    [InlineData("", "v1/guide", null)]
    [InlineData("", "guide/v2", null)]
    [InlineData("/v1", "v2/guide", null)]
    public void LatestAlias_OnlyStripsTheLatestPrefixOnTheLatestDeploy(string basePath, string path, string? expected) =>
        Assert.Equal(expected, VersionSwitcherRenderer.LatestAlias(Versions, basePath, path));

    [Fact]
    public void Notice_OnlyOnOlderVersions_LinkingToTheLatest()
    {
        Assert.Empty(VersionSwitcherRenderer.Notice(Versions, 0, "", Localization.Default));
        Assert.Contains("href=\"/bark/\"", VersionSwitcherRenderer.Notice(Versions, 1, "/bark/v1", Localization.Default));
    }

    [Fact]
    public void Validate_FlagsMistakes_AndStaysQuietWhenDisabled()
    {
        var bad = new VersionsConfig
        {
            Items =
            [
                new() { Text = "v2", Link = "/v2/" },
                new() { Text = "v2", Link = "v1" },
                new() { Text = "", Link = "/" },
            ],
        };

        var problems = VersionSwitcherRenderer.Validate(bad).ToList();

        Assert.Contains(problems, p => p.Contains("must link to \"/\""));
        Assert.Contains(problems, p => p.Contains("more than once"));
        Assert.Contains(problems, p => p.Contains("must start with \"/\""));
        Assert.Contains(problems, p => p.Contains("needs both"));
        Assert.Empty(VersionSwitcherRenderer.Validate(new VersionsConfig { Items = Versions }));
        bad.Enabled = false;
        Assert.Empty(VersionSwitcherRenderer.Validate(bad));
    }
}

/// <summary>Boots an older version deployed under a base path, the way versioned deploys run.</summary>
public sealed class VersionedWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _docsDir =
        Path.Combine(Path.GetTempPath(), "bark-version-" + Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(Path.Combine(_docsDir, "guide"));
        File.WriteAllText(Path.Combine(_docsDir, "index.md"), "# Home\n");
        File.WriteAllText(Path.Combine(_docsDir, "guide", "install.md"), "# Install\n");
        File.WriteAllText(Path.Combine(_docsDir, "config.json"),
            """{"versions": {"items": [{"text": "v2", "link": "/"}, {"text": "v1", "link": "/v1/"}]}}""");

        builder.UseSetting("urls", "http://127.0.0.1:0");
        builder.UseSetting("Docs:RootPath", _docsDir);
        builder.UseSetting("Docs:BasePath", "/v1");
        builder.UseSetting("Docs:EnableHotReload", "false");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (Directory.Exists(_docsDir))
            Directory.Delete(_docsDir, true);
    }
}

public sealed class VersionedDeployTests : IClassFixture<VersionedWebApplicationFactory>
{
    private readonly VersionedWebApplicationFactory _factory;

    public VersionedDeployTests(VersionedWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task OlderVersion_ShowsNotice_AndIsNotIndexed()
    {
        var html = await _factory.CreateClient().GetStringAsync("/v1/guide/install");

        Assert.Contains("id=\"version-toggle\"", html);
        Assert.Contains("class=\"translation-notice version-notice\"", html);
        Assert.Contains("<meta name=\"robots\" content=\"noindex\">", html);
    }

    [Fact]
    public async Task StaticFiles_AreServedUnderTheBasePath()
    {
        var response = await _factory.CreateClient().GetAsync("/v1/brand-image.svg");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
