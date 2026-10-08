using Microsoft.Extensions.Logging.Abstractions;
using Bark.Configuration;
using Bark.Models;
using Bark.Services;

namespace Bark.Tests;

public sealed class DocumentationServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly DocumentationService _service;
    private readonly DocsOptions _options;
    private readonly MarkdownService _markdown;

    public DocumentationServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "bark-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(Path.Combine(_tempDir, "getting-started"));

        _options = new DocsOptions
        {
            RootPath = _tempDir,
            DefaultPage = "index",
            EnableHotReload = false
        };
        _markdown = new MarkdownService();
        _service = new DocumentationService(_options, _markdown, NullLogger<DocumentationService>.Instance);
    }

    public void Dispose()
    {
        _service.Dispose();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private async Task CreateTestFiles()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "index.md"),
            "---\ntitle: Home\ndescription: Welcome page\n---\n\n# homepage\n\nWelcome to the docs.\n");
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "getting-started", "installation.md"),
            "---\ntitle: Installation\n---\n\n# Installation Guide\n\nHow to install.\n");
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "getting-started", "configuration.md"),
            "---\ntitle: Configuration\n---\n\n# Configuration Guide\n\nHow to configure.\n");
    }

    [Fact]
    public async Task StartAsync_BuildsPageCache()
    {
        await CreateTestFiles();
        await _service.StartAsync(CancellationToken.None);

        var home = await _service.GetPageAsync("index");
        Assert.NotNull(home);
        Assert.Equal("Home", home!.Title);

        var install = await _service.GetPageAsync("getting-started/installation");
        Assert.NotNull(install);
        Assert.Equal("Installation", install!.Title);
    }

    [Fact]
    public async Task GetPageAsync_ReturnsNull_ForMissingPage()
    {
        await _service.StartAsync(CancellationToken.None);
        var page = await _service.GetPageAsync("nonexistent");
        Assert.Null(page);
    }

    [Fact]
    public async Task GetPageAsync_NormalizesPath()
    {
        await CreateTestFiles();
        await _service.StartAsync(CancellationToken.None);

        var page = await _service.GetPageAsync("/Getting-Started/Installation/");
        Assert.NotNull(page);
        Assert.Equal("Installation", page!.Title);
    }

    [Fact]
    public async Task GetNavigationAsync_ReturnsTree()
    {
        await CreateTestFiles();
        await _service.StartAsync(CancellationToken.None);

        var nav = await _service.GetNavigationAsync();
        Assert.NotNull(nav);

        var gettingStarted = nav.Children.FirstOrDefault(c => c.Title == "getting-started");
        Assert.NotNull(gettingStarted);
        Assert.Equal(2, gettingStarted!.Children.Count);
    }

    [Fact]
    public async Task GetAllPagesAsync_ReturnsAllPages()
    {
        await CreateTestFiles();
        await _service.StartAsync(CancellationToken.None);

        var pages = await _service.GetAllPagesAsync();
        Assert.Equal(3, pages.Count);
    }

    [Fact]
    public async Task GetBreadcrumbs_ReturnsHomeAndSegments()
    {
        await CreateTestFiles();
        await _service.StartAsync(CancellationToken.None);

        var crumbs = await _service.GetBreadcrumbsAsync("getting-started/installation");
        Assert.Equal(3, crumbs.Count);
        Assert.Equal("Home", crumbs[0].Title);
        Assert.Equal("Getting started", crumbs[1].Title);
        Assert.Equal("Installation", crumbs[2].Title);
    }

    [Fact]
    public async Task GetBreadcrumbs_SegmentWithoutPage_HasNullPath()
    {
        await CreateTestFiles();
        await _service.StartAsync(CancellationToken.None);

        var crumbs = await _service.GetBreadcrumbsAsync("getting-started/installation");
        Assert.Null(crumbs[1].Path); // "getting-started" has no index page
        Assert.Equal("/getting-started/installation", crumbs[2].Path);
    }

    [Fact]
    public async Task GetBreadcrumbs_RootPath_ReturnsOnlyHome()
    {
        await CreateTestFiles();
        await _service.StartAsync(CancellationToken.None);

        var crumbs = await _service.GetBreadcrumbsAsync("index");
        Assert.Single(crumbs);
        Assert.Equal("Home", crumbs[0].Title);
    }

    [Fact]
    public async Task GetBreadcrumbsAsync_TopNavPath_UsesNavLabel()
    {
        await CreateTestFiles();
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "config.json"),
            """{"topNav":[{"text":"Reference","link":"/reference/"},{"text":"Guides","items":[{"text":"Getting Started","link":"/guide/"}]}]}""");
        await _service.StartAsync(CancellationToken.None);

        var crumbs = await _service.GetBreadcrumbsAsync("reference/api");

        Assert.Equal(3, crumbs.Count);
        Assert.Equal("Reference", crumbs[1].Title);
        Assert.Null(crumbs[1].Path); // top-nav segment has no page — no link
    }

    [Fact]
    public async Task GetBreadcrumbsAsync_TopNavDropdownChild_UsesNavLabel()
    {
        await CreateTestFiles();
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "config.json"),
            """{"topNav":[{"text":"Guides","items":[{"text":"Getting Started","link":"/guide/"}]}]}""");
        await _service.StartAsync(CancellationToken.None);

        var crumbs = await _service.GetBreadcrumbsAsync("guide/intro");

        Assert.Equal(3, crumbs.Count);
        Assert.Equal("Getting Started", crumbs[1].Title);
    }

    [Fact]
    public async Task Search_ReturnsResults_AfterBuild()
    {
        await CreateTestFiles();
        await _service.StartAsync(CancellationToken.None);

        var results = _service.Search("installation");
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Path == "getting-started/installation");
    }

    [Fact]
    public async Task Search_EmptyResult_ForNoMatch()
    {
        await CreateTestFiles();
        await _service.StartAsync(CancellationToken.None);

        var results = _service.Search("zzzzz_no_match_zzzzz");
        Assert.Empty(results);
    }

    [Fact]
    public async Task Search_ReturnsEmpty_BeforeBuild()
    {
        var results = _service.Search("anything");
        Assert.Empty(results);
    }

    [Fact]
    public async Task StartAsync_PageWithoutFrontMatterTitle_FallsBackToConfigNavTitle()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "index.md"), "# Home\n");
        Directory.CreateDirectory(Path.Combine(_tempDir, "getting-started"));
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "getting-started", "routing.md"),
            "# Some heading unrelated to the nav title\n");
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "config.json"), """
            {
              "nav": [
                {
                  "section": "Guide",
                  "items": [
                    { "title": "routing", "path": "getting-started/routing" }
                  ]
                }
              ]
            }
            """);

        await _service.StartAsync(CancellationToken.None);

        var page = await _service.GetPageAsync("getting-started/routing");
        Assert.NotNull(page);
        Assert.Equal("routing", page!.Title);
    }

    [Fact]
    public async Task StartAsync_FrontMatterTitle_TakesPrecedenceOverConfigNavTitle()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "index.md"), "# Home\n");
        Directory.CreateDirectory(Path.Combine(_tempDir, "getting-started"));
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "getting-started", "routing.md"),
            "---\ntitle: Custom Routing Title\n---\n\n# Routing\n");
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "config.json"), """
            {
              "nav": [
                {
                  "section": "Guide",
                  "items": [
                    { "title": "routing", "path": "getting-started/routing" }
                  ]
                }
              ]
            }
            """);

        await _service.StartAsync(CancellationToken.None);

        var page = await _service.GetPageAsync("getting-started/routing");
        Assert.NotNull(page);
        Assert.Equal("Custom Routing Title", page!.Title);
    }

    [Fact]
    public async Task StartAsync_PageWithTocFalse_ShowTocFalse()
    {
        await CreateTestFiles();
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "getting-started", "installation.md"),
            "---\ntitle: Installation\ntoc: false\n---\n\n# Installation Guide\n\nHow to install.\n");
        await _service.StartAsync(CancellationToken.None);

        var page = await _service.GetPageAsync("getting-started/installation");
        Assert.NotNull(page);
        Assert.False(page!.ShowToc);
    }

    [Fact]
    public async Task StartAsync_PageWithSidebarFalse_ShowSidebarFalse()
    {
        await CreateTestFiles();
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "getting-started", "installation.md"),
            "---\ntitle: Installation\nsidebar: false\nwidth: 80%\n---\n\n# Installation Guide\n");
        await _service.StartAsync(CancellationToken.None);

        var page = await _service.GetPageAsync("getting-started/installation");
        Assert.NotNull(page);
        Assert.False(page!.ShowSidebar);
        Assert.Equal(80, page.ContentWidth);
    }

    [Fact]
    public async Task StartAsync_PageWithoutToc_ShowTocTrue()
    {
        await CreateTestFiles();
        await _service.StartAsync(CancellationToken.None);

        var page = await _service.GetPageAsync("getting-started/installation");
        Assert.NotNull(page);
        Assert.True(page!.ShowToc);
    }

    [Fact]
    public async Task StartAsync_PageWithPaginationFalse_ShowPaginationFalse()
    {
        await CreateTestFiles();
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "getting-started", "installation.md"),
            "---\ntitle: Installation\npagination: false\n---\n\n# Installation Guide\n\nHow to install.\n");
        await _service.StartAsync(CancellationToken.None);

        var page = await _service.GetPageAsync("getting-started/installation");
        Assert.NotNull(page);
        Assert.False(page!.ShowPagination);
    }

    [Fact]
    public async Task StartAsync_PageWithoutPagination_ShowPaginationTrue()
    {
        await CreateTestFiles();
        await _service.StartAsync(CancellationToken.None);

        var page = await _service.GetPageAsync("getting-started/installation");
        Assert.NotNull(page);
        Assert.True(page!.ShowPagination);
    }

    [Fact]
    public async Task StartAsync_PageWithRedirect_ReturnsRedirect()
    {
        await CreateTestFiles();
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "getting-started", "installation.md"),
            "---\nredirect: /guide/setup\n---\n");
        await _service.StartAsync(CancellationToken.None);

        var page = await _service.GetPageAsync("getting-started/installation");
        Assert.NotNull(page);
        Assert.Equal("/guide/setup", page!.Redirect);
    }

    [Fact]
    public async Task StartAsync_PageWithFrontmatterDate_UsesItForLastModified()
    {
        await CreateTestFiles();
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "getting-started", "installation.md"),
            "---\ntitle: Installation\ndate: 2025-03-01\n---\n\n# Installation Guide\n");
        await _service.StartAsync(CancellationToken.None);

        var page = await _service.GetPageAsync("getting-started/installation");
        Assert.NotNull(page);
        Assert.Equal(new DateTime(2025, 3, 1), page!.LastModified);
    }

    [Fact]
    public async Task StartAsync_PageWithFrontmatterUpdated_OverridesFileMtime()
    {
        await CreateTestFiles();
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "getting-started", "installation.md"),
            "---\ntitle: Installation\ndate: 2025-01-01\nupdated: 2025-06-28\n---\n\n# Installation Guide\n");
        await _service.StartAsync(CancellationToken.None);

        var page = await _service.GetPageAsync("getting-started/installation");
        Assert.NotNull(page);
        Assert.Equal(new DateTime(2025, 6, 28), page!.LastModified);
    }
}
