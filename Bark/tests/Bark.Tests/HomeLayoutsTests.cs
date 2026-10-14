using Bark.Configuration;
using Bark.Models;
using Bark.Services;
using Bark.Services.Rendering;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bark.Tests;

public sealed class HomeLayoutsTests : IDisposable
{
    private readonly string _docs = Path.Combine(Path.GetTempPath(), "bark-layouts-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_docs))
            Directory.Delete(_docs, true);
    }

    private static string Render(string frontMatter) => new MarkdownService().Parse($"---\n{frontMatter}---\n").Html;

    [Fact]
    public void HeroWithImageFile_IsSplit_EmojiHeroStaysCentered()
    {
        Assert.Contains("class=\"bark-hero bark-hero--split\"", Render("layout: home\nhero:\n  text: Docs\n  image: /logo.svg\n"));
        Assert.Contains("class=\"bark-hero\"", Render("layout: home\nhero:\n  text: Docs\n  image: \"🌳\"\n"));
    }

    [Fact]
    public void BannerVariant_MarksTheHome()
    {
        Assert.Contains("class=\"bark-home bark-home--banner\"", Render("layout: home\nhero:\n  variant: banner\n  text: Docs\n"));
        Assert.Contains("<div class=\"bark-home\">", Render("layout: home\nhero:\n  text: Docs\n"));
    }

    [Fact]
    public void Actions_BrandGetsArrow_MinimalExternalGetsExternalIcon_UnknownFallsBackToBrand()
    {
        var html = Render("""
            layout: home
            hero:
              text: Docs
              actions:
                - theme: brand
                  text: Start
                  link: /start
                - theme: minimal
                  text: GitHub
                  link: https://github.com/x/y
                - theme: loud
                  text: Other
                  link: /other

            """);

        Assert.Contains("class=\"bark-hero-action brand\" href=\"/start/\">Start<svg", html);
        Assert.Contains("class=\"bark-hero-action minimal\" href=\"https://github.com/x/y\">GitHub<svg", html);
        Assert.Contains("class=\"bark-hero-action brand\" href=\"/other/\">Other<svg", html);
    }

    [Fact]
    public void HelpLayout_RendersSearchTopicsPopularAndContact()
    {
        var html = Render("""
            layout: help
            hero:
              text: How can we help?
              tagline: Answers.
            features:
              - title: Payroll
                details: Pay runs.
                link: payroll
            popular:
              - text: First run
                link: payroll/first-run
            contact: "Need more? [Contact us](mailto:help@example.com)"

            """);

        Assert.Contains("<h1 class=\"bark-help-title\">How can we help?</h1>", html);
        Assert.Contains("data-bark-search", html);
        Assert.Contains("href=\"/payroll/\"", html);
        Assert.Contains("data-section=\"payroll\"", html);
        Assert.Contains("href=\"/payroll/first-run/\">First run</a>", html);
        Assert.Contains("<a href=\"mailto:help@example.com\">Contact us</a>", html);
    }

    [Fact]
    public void HelpLayout_OmitsOptionalBlocks()
    {
        var html = Render("layout: help\nhero:\n  text: Help\n");

        Assert.DoesNotContain("bark-help-topics", html);
        Assert.DoesNotContain("bark-help-popular", html);
        Assert.DoesNotContain("bark-help-contact", html);
    }

    [Fact]
    public async Task HelpLayout_FillsArticleCountsFromThePageSet()
    {
        Directory.CreateDirectory(Path.Combine(_docs, "payroll"));
        Directory.CreateDirectory(Path.Combine(_docs, "billing"));
        await File.WriteAllTextAsync(Path.Combine(_docs, "index.md"),
            "---\nlayout: help\nhero:\n  text: Help\nfeatures:\n  - title: Payroll\n    link: payroll\n  - title: Billing\n    link: billing\n  - title: Empty\n    link: empty\n---\n");
        await File.WriteAllTextAsync(Path.Combine(_docs, "payroll", "index.md"), "# Payroll\n");
        await File.WriteAllTextAsync(Path.Combine(_docs, "payroll", "first-run.md"), "# First run\n");
        await File.WriteAllTextAsync(Path.Combine(_docs, "payroll", "corrections.md"), "# Corrections\n");
        await File.WriteAllTextAsync(Path.Combine(_docs, "billing", "invoices.md"), "# Invoices\n");

        using var service = new DocumentationService(new DocsOptions { RootPath = _docs, EnableHotReload = false }, new MarkdownService(), NullLogger<DocumentationService>.Instance);
        await service.StartAsync(CancellationToken.None);
        var html = (await service.GetPageAsync("index"))!.HtmlContent;

        Assert.Contains("<span class=\"bark-help-count\">2 articles</span>", html);
        Assert.Contains("<span class=\"bark-help-count\">1 article</span>", html);
        Assert.DoesNotContain("data-section", html);
    }

    [Fact]
    public void BottomNav_GroupsRenderAsColumns_FlatLinksStayInline()
    {
        var html = NavigationHtmlRenderer.BuildBottomNavHtml(
        [
            new TopNavItem { Text = "Learn", Items = [new TopNavItem { Text = "Start", Link = "/start" }] },
            new TopNavItem { Text = "Privacy", Link = "/privacy" },
        ], "");

        Assert.Contains("class=\"footer-nav footer-nav--columns\"", html);
        Assert.Contains("<h2 class=\"footer-nav-title\">Learn</h2><ul><li><a href=\"/start/\">Start</a></li></ul>", html);
        Assert.Contains("<a href=\"/privacy/\">Privacy</a>", html);
        Assert.DoesNotContain("footer-nav--columns", NavigationHtmlRenderer.BuildBottomNavHtml([new TopNavItem { Text = "Privacy", Link = "/privacy" }], ""));
    }
}
