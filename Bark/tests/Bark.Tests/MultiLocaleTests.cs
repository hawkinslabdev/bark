using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Bark.Tests;

public sealed class MultiLocaleWebApplicationFactory : WebApplicationFactory<Program>
{
    public string DocsDir { get; } =
        Path.Combine(Path.GetTempPath(), "bark-locale-integration-" + Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(DocsDir);
        Directory.CreateDirectory(Path.Combine(DocsDir, "guide"));
        Directory.CreateDirectory(Path.Combine(DocsDir, "nl", "guide"));
        Directory.CreateDirectory(Path.Combine(DocsDir, "locale"));

        File.WriteAllText(Path.Combine(DocsDir, "index.md"), "---\ntitle: Home\n---\n\n# Welcome\n\nEnglish home.\n");
        File.WriteAllText(Path.Combine(DocsDir, "guide", "install.md"), "---\ntitle: Install\n---\n\n# Install\n\nEnglish install page.\n");
        File.WriteAllText(Path.Combine(DocsDir, "guide", "deploy.md"), "---\ntitle: Deploy\n---\n\n# Deploy\n\nEnglish deploy page.\n");
        File.WriteAllText(Path.Combine(DocsDir, "nl", "index.md"), "---\ntitle: Start\n---\n\n# Welkom\n\nNederlandse startpagina.\n");
        File.WriteAllText(Path.Combine(DocsDir, "home.md"), "---\ntitle: Landing\nlayout: home\nhero:\n  name: Landing\n  actions:\n    - text: Guide\n      link: /guide/install\n---\n");
        File.WriteAllText(Path.Combine(DocsDir, "nl", "home.md"), "---\ntitle: Startpunt\nlayout: home\nhero:\n  name: Startpunt\n  actions:\n    - text: Handleiding\n      link: guide/install\n    - text: GitHub\n      link: https://example.com/repo\n---\n");
        File.WriteAllText(Path.Combine(DocsDir, "old-page.md"), "---\ntitle: Old\nredirect: /guide/install/\n---\n");
        File.WriteAllText(Path.Combine(DocsDir, "landing.md"), "---\ntitle: Landing\nlayout: home\nhero:\n  name: Landing\n---\n");
        File.WriteAllText(Path.Combine(DocsDir, "nl", "guide", "install.md"), "---\ntitle: Installeren\n---\n\n# Installeren\n\nNederlandse installatiepagina.\n");
        File.WriteAllText(Path.Combine(DocsDir, "locale", "nl.json"), """
            {
              "tocTitle": "Op Deze Pagina",
              "searchNoResults": "Geen resultaten gevonden.",
              "config": {
                "Guide": "Handleiding",
                "Install": "Installeren",
                "Deploy": "Uitrollen",
                "Guide": "Handleiding",
                "Docs": "Documentatie",
                "Written in English.": "Geschreven in het Nederlands.",
                "**New:** an English announcement.": "**Nieuw:** een Nederlandse aankondiging."
              }
            }
            """);
        File.WriteAllText(Path.Combine(DocsDir, "config.json"), """
            {
              "locales": {
                "root": { "label": "English", "lang": "en" },
                "nl": { "label": "Nederlands", "lang": "nl" }
              },
              "topNav": [
                { "text": "Home", "link": "/" },
                { "text": "Guide", "link": "/guide/install" },
                { "text": "GitHub", "link": "https://example.com/repo" }
              ],
              "sidebar": {
                "/guide/": [
                  {
                    "title": "Guide",
                    "items": [
                      { "title": "Install", "path": "guide/install" },
                      { "title": "Deploy", "path": "guide/deploy" }
                    ]
                  }
                ]
              },
              "brand": "Docs",
              "footer": "Written in English.",
              "bottomNav": [
                { "text": "Deploy", "link": "/guide/deploy" },
                { "text": "Source", "link": "https://example.com/repo" },
                { "text": "", "link": "/ignored" }
              ],
              "promo": "**New:** an English announcement."
            }
            """);

        File.SetLastWriteTimeUtc(Path.Combine(DocsDir, "nl", "guide", "install.md"), new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        File.SetLastWriteTimeUtc(Path.Combine(DocsDir, "guide", "install.md"), new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));

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

public sealed class MultiLocaleTests : IClassFixture<MultiLocaleWebApplicationFactory>
{
    private readonly MultiLocaleWebApplicationFactory _factory;

    public MultiLocaleTests(MultiLocaleWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task LocaleTreeIndex_ServesTheTranslatedPage()
    {
        var html = await _factory.CreateClient().GetStringAsync("/nl");

        Assert.Contains("Nederlandse startpagina", html);
        Assert.Contains("lang=\"nl\"", html);
        Assert.DoesNotContain("translation-notice", html);
    }

    [Fact]
    public async Task BottomNav_SplitsTheFooterAndLocalizesInternalLinks()
    {
        var client = _factory.CreateClient();
        var en = await client.GetStringAsync("/guide/install");
        var nl = await client.GetStringAsync("/nl/guide/install");

        Assert.Contains("class=\"footer-bar footer-bar--split\"", en);
        Assert.Contains("<a href=\"/guide/deploy/\">Deploy</a>", en);
        Assert.Contains("<a href=\"https://example.com/repo\" target=\"_blank\" rel=\"noopener noreferrer\">Source", en);
        Assert.DoesNotContain("/ignored", en);
        Assert.Contains("<a href=\"/nl/guide/deploy/\">Uitrollen</a>", nl);
    }

    [Fact]
    public async Task TranslatedPage_UsesTheTranslatedStringTable()
    {
        var html = await _factory.CreateClient().GetStringAsync("/nl/guide/install");

        Assert.Contains("Nederlandse installatiepagina", html);
        Assert.Contains("Op Deze Pagina", html);
    }

    [Fact]
    public async Task TranslationOlderThanItsOriginal_ShowsTheStaleNotice()
    {
        var html = await _factory.CreateClient().GetStringAsync("/nl/guide/install");

        Assert.Contains("translation-notice--stale", html);
        Assert.Contains("/guide/install/", html);
    }

    [Fact]
    public async Task TranslationNewerThanItsOriginal_ShowsNoNotice()
    {
        var html = await _factory.CreateClient().GetStringAsync("/nl");

        Assert.DoesNotContain("translation-notice", html);
    }

    [Fact]
    public async Task UntranslatedPage_ServesTheOriginalWithANotice()
    {
        var html = await _factory.CreateClient().GetStringAsync("/nl/guide/deploy");

        Assert.Contains("English deploy page", html);
        Assert.Contains("translation-notice", html);
        Assert.Contains("/guide/deploy/", html);
    }

    [Fact]
    public async Task UntranslatedPage_PointsItsCanonicalAtTheOriginal()
    {
        var html = await _factory.CreateClient().GetStringAsync("/nl/guide/deploy");

        Assert.Contains("rel=\"canonical\"", html);
        Assert.DoesNotContain("<link rel=\"canonical\" href=\"http://localhost/nl/guide/deploy/\">", html);
    }

    [Fact]
    public async Task RootTree_HasNoNoticeAndKeepsEnglish()
    {
        var html = await _factory.CreateClient().GetStringAsync("/guide/install");

        Assert.Contains("English install page", html);
        Assert.Contains("lang=\"en\"", html);
        Assert.DoesNotContain("translation-notice", html);
    }

    [Fact]
    public async Task EveryPage_OffersTheLocaleSwitcher()
    {
        var html = await _factory.CreateClient().GetStringAsync("/guide/install");

        Assert.Contains("locale-switcher", html);
        Assert.Contains(">English</a>", html);
        Assert.Contains(">Nederlands</a>", html);
        Assert.Contains("href=\"/nl/guide/install/\"", html);
    }

    [Fact]
    public async Task LocaleSwitcher_KeepsTheCurrentPageWhenItIsUntranslated()
    {
        var html = await _factory.CreateClient().GetStringAsync("/guide/deploy");

        Assert.Contains("href=\"/nl/guide/deploy/\"", html);
    }

    [Fact]
    public async Task RootNavigation_DoesNotListTheLocaleTree()
    {
        var html = await _factory.CreateClient().GetStringAsync("/guide/install");
        var start = html.IndexOf("class=\"sidebar-left\"", StringComparison.Ordinal);
        var sidebar = html[start..html.IndexOf("</aside>", start, StringComparison.Ordinal)];

        Assert.DoesNotContain("/nl/", sidebar);
    }

    [Fact]
    public async Task Search_IsScopedToTheRequestedLocale()
    {
        var client = _factory.CreateClient();

        var dutch = await client.GetStringAsync("/api/search?q=installeren&locale=nl");
        var english = await client.GetStringAsync("/api/search?q=installeren&locale=en");

        Assert.Contains("nl/guide/install", dutch);
        Assert.DoesNotContain("nl/guide/install", english);
    }

    [Fact]
    public async Task Script_IsServedPerLocale()
    {
        var client = _factory.CreateClient();

        var dutch = await client.GetStringAsync("/bark.js?locale=nl");
        var english = await client.GetStringAsync("/bark.js?locale=en");

        Assert.Contains("Geen resultaten gevonden.", dutch);
        Assert.Contains("No results found.", english);
    }

    [Fact]
    public async Task Sitemap_LinksTranslationsAsAlternates()
    {
        var xml = await _factory.CreateClient().GetStringAsync("/sitemap.xml");

        Assert.Contains("xmlns:xhtml", xml);
        Assert.Contains("hreflang=\"nl\"", xml);
        Assert.Contains("hreflang=\"en\"", xml);
    }

    [Fact]
    public async Task SidebarInsideALocaleTree_KeepsEveryLinkInThatLocale()
    {
        var html = await _factory.CreateClient().GetStringAsync("/nl/guide/install");
        var start = html.IndexOf("class=\"sidebar-left\"", StringComparison.Ordinal);
        var sidebar = html[start..html.IndexOf("</aside>", start, StringComparison.Ordinal)];

        Assert.Contains("href=\"/nl/guide/install/\"", sidebar);
        Assert.Contains("href=\"/nl/guide/deploy/\"", sidebar);
        Assert.DoesNotContain("href=\"/guide/", sidebar);
    }

    [Fact]
    public async Task TopNavInsideALocaleTree_KeepsInternalLinksInThatLocale()
    {
        var html = await _factory.CreateClient().GetStringAsync("/nl/guide/install");
        var start = html.IndexOf("class=\"top-nav\"", StringComparison.Ordinal);
        var nav = html[start..html.IndexOf("</nav>", start, StringComparison.Ordinal)];

        Assert.Contains("href=\"/nl/guide/install/\"", nav);
        Assert.Contains("href=\"/nl/\"", nav);
        Assert.DoesNotContain("href=\"/guide/install/\"", nav);
        Assert.Contains("https://example.com/repo", nav);
    }

    [Fact]
    public async Task PaginationInsideALocaleTree_LinksTheNextPageInThatLocale()
    {
        var html = await _factory.CreateClient().GetStringAsync("/nl/guide/install");
        var start = html.IndexOf("class=\"pagination", StringComparison.Ordinal);
        var pager = start < 0 ? string.Empty : html[start..html.IndexOf("</nav>", start, StringComparison.Ordinal)];

        Assert.Contains("/nl/guide/deploy/", pager);
        Assert.Contains("Deploy", pager);
    }

    [Fact]
    public async Task SidebarOnTheRootTree_StaysOnTheRootTree()
    {
        var html = await _factory.CreateClient().GetStringAsync("/guide/install");
        var start = html.IndexOf("class=\"sidebar-left\"", StringComparison.Ordinal);
        var sidebar = html[start..html.IndexOf("</aside>", start, StringComparison.Ordinal)];

        Assert.Contains("href=\"/guide/deploy/\"", sidebar);
        Assert.DoesNotContain("href=\"/nl/", sidebar);
    }

    [Fact]
    public async Task MenuLabelsComeFromTheLocaleEntry()
    {
        var html = await _factory.CreateClient().GetStringAsync("/nl/guide/install");
        var start = html.IndexOf("class=\"sidebar-left\"", StringComparison.Ordinal);
        var sidebar = html[start..html.IndexOf("</aside>", start, StringComparison.Ordinal)];

        Assert.Contains("Handleiding", sidebar);
        Assert.Contains("Installeren", sidebar);
        Assert.Contains("Uitrollen", sidebar);
        Assert.DoesNotContain(">Deploy<", sidebar);
        Assert.DoesNotContain(">Install<", sidebar);
    }

    [Fact]
    public async Task BrandAndFooterComeFromTheLocaleEntry()
    {
        var html = await _factory.CreateClient().GetStringAsync("/nl/guide/install");

        Assert.Contains("Documentatie", html);
        Assert.Contains("Geschreven in het Nederlands.", html);
    }

    [Fact]
    public async Task PromoBarComesFromTheLocaleEntry()
    {
        var dutch = await _factory.CreateClient().GetStringAsync("/nl/guide/install");
        var english = await _factory.CreateClient().GetStringAsync("/guide/install");

        Assert.Contains("een Nederlandse aankondiging", dutch);
        Assert.DoesNotContain("an English announcement", dutch);
        Assert.Contains("an English announcement", english);
    }

    [Fact]
    public async Task RootTreeKeepsItsOwnMenuLabels()
    {
        var html = await _factory.CreateClient().GetStringAsync("/guide/install");
        var start = html.IndexOf("class=\"sidebar-left\"", StringComparison.Ordinal);
        var sidebar = html[start..html.IndexOf("</aside>", start, StringComparison.Ordinal)];

        Assert.Contains(">Deploy<", sidebar);
        Assert.Contains("Written in English.", html);
        Assert.DoesNotContain("Uitrollen", html);
    }

    [Fact]
    public async Task LocaleWithoutOverrides_InheritsTheRootMenus()
    {
        var html = await _factory.CreateClient().GetStringAsync("/guide/install");

        Assert.Contains("href=\"/guide/deploy/\"", html);
    }

    [Fact]
    public async Task HomeLayoutNeverShowsATranslationNotice()
    {
        var translated = await _factory.CreateClient().GetStringAsync("/nl/home");
        var fallback = await _factory.CreateClient().GetStringAsync("/nl/landing");

        Assert.DoesNotContain("translation-notice", translated);
        Assert.Contains("Landing", fallback);
        Assert.DoesNotContain("translation-notice", fallback);
    }

    [Fact]
    public async Task HomeHeroLinksStayInsideTheLocaleTree()
    {
        var html = await _factory.CreateClient().GetStringAsync("/nl/home");

        Assert.Contains("Startpunt", html);
        Assert.Contains("href=\"/nl/guide/install/\"", html);
        Assert.DoesNotContain("class=\"bark-hero-action brand\" href=\"/guide/install/\"", html);
        Assert.Contains("https://example.com/repo", html);
    }

    [Fact]
    public async Task RedirectInsideALocaleTree_StaysInThatTree()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var dutch = await client.GetAsync("/nl/old-page");
        var english = await client.GetAsync("/old-page");

        Assert.Equal("/nl/guide/install/", dutch.Headers.Location?.OriginalString);
        Assert.Equal("/guide/install/", english.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task BreadcrumbSegmentsUseTheDictionary()
    {
        var html = await _factory.CreateClient().GetStringAsync("/nl/guide/install");
        var start = html.IndexOf("class=\"breadcrumb\"", StringComparison.Ordinal);
        var crumbs = html[start..html.IndexOf("</nav>", start, StringComparison.Ordinal)];

        Assert.Contains("Handleiding", crumbs);
        Assert.DoesNotContain(">Guide<", crumbs);
    }

    [Fact]
    public async Task UnknownPageInALocaleTree_Returns404()
    {
        var response = await _factory.CreateClient().GetAsync("/nl/guide/nothing-here");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task NotFoundPageInsideALocaleTree_SendsTheReaderBackToThatTree()
    {
        var response = await _factory.CreateClient().GetAsync("/nl/guide/nothing-here");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("lang=\"nl\"", html);
        Assert.Contains("href=\"/nl/\"", html);
    }

    [Fact]
    public async Task BrandLinkStaysInsideTheLocaleTree()
    {
        var html = await _factory.CreateClient().GetStringAsync("/nl/guide/install");
        var start = html.IndexOf("class=\"brand\"", StringComparison.Ordinal);
        var brand = html[start..(start + 200)];

        Assert.Contains("href=\"/nl/\"", brand);
    }

    [Fact]
    public async Task SocialMetaDeclaresTheLocaleLanguage()
    {
        var html = await _factory.CreateClient().GetStringAsync("/nl/guide/install");

        Assert.Contains("\"nl\"", html);
        Assert.DoesNotContain("og:locale\" content=\"en\"", html);
    }

    [Fact]
    public async Task LlmsTxtListsTheRootTreeOnly()
    {
        var text = await _factory.CreateClient().GetStringAsync("/llms.txt");

        Assert.Contains("/guide/install/", text);
        Assert.DoesNotContain("/nl/", text);
    }
}

public sealed class BottomNavRendererTests
{
    [Fact]
    public void EmptyBottomNav_RendersNoNavLandmark() =>
        Assert.Equal(string.Empty, Bark.Services.Rendering.NavigationHtmlRenderer.BuildBottomNavHtml([], basePath: ""));
}
