using Bark.Services;
using Bark.Services.Layout;

namespace Bark.Tests;

public sealed class LayoutProviderTests
{
    [Fact]
    public void GetLayout_ContainsTitle()
    {
        var html = LayoutProvider.GetLayout(
            "Test Title", "<p>content</p>",
            "<nav>nav</nav>", "<li>toc</li>",
            "<a href='/'>Home</a>", "<nav>pagination</nav>",
            null);

        Assert.Contains("Test Title", html);
    }

    [Theory]
    [InlineData("", null, true)]
    [InlineData("", "<li>toc</li>", false)]
    [InlineData("<nav>nav</nav>", null, false)]
    public void GetLayout_ProseLayout_RequiresNoSidebarAndNoToc(string nav, string? toc, bool expected)
    {
        var html = LayoutProvider.GetLayout("Title", "<p>x</p>", nav, toc, "", "", null);

        Assert.Equal(expected, html.Contains("bark-prose-layout"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("<nav>nav</nav>")]
    public void GetLayout_Wide_ReplacesProseLayout(string nav)
    {
        var html = LayoutProvider.GetLayout("Title", "<p>x</p>", nav, null, "", "", null, isWide: true);

        Assert.Contains("bark-wide-layout", html);
        Assert.DoesNotContain("bark-prose-layout", html);
    }

    [Fact]
    public void GetLayout_ContentWidth_SetsCustomProperty()
    {
        var html = LayoutProvider.GetLayout("Title", "<p>x</p>", "", null, "", "", null, contentWidth: 80);

        Assert.Contains("bark-custom-width", html);
        Assert.Contains("style=\"--bark-content-width: 80%\"", html);
    }

    [Fact]
    public void GetLayout_ContentWidth_IgnoredOnHome()
    {
        var html = LayoutProvider.GetLayout("Title", "<p>x</p>", "", null, "", "", null, isHomePage: true, contentWidth: 80);

        Assert.DoesNotContain("bark-content-width", html);
    }

    [Fact]
    public void GetLayout_ContainsContent()
    {
        var html = LayoutProvider.GetLayout(
            "Title", "<p>Hello World</p>",
            "<nav>nav</nav>", "<li>toc</li>",
            "<a href='/'>Home</a>", "<nav>pagination</nav>",
            null);

        Assert.Contains("Hello World", html);
    }

    [Fact]
    public void GetLayout_ContainsNavigation()
    {
        var html = LayoutProvider.GetLayout(
            "Title", "<p>content</p>",
            "<ul><li>Nav Item</li></ul>", "<li>toc</li>",
            "<a href='/'>Home</a>", "<nav>pagination</nav>",
            null);

        Assert.Contains("Nav Item", html);
    }

    [Fact]
    public void GetLayout_ContainsToc()
    {
        var html = LayoutProvider.GetLayout(
            "Title", "<p>content</p>",
            "<nav>nav</nav>", "<li>Section One</li>",
            "<a href='/'>Home</a>", "<nav>pagination</nav>",
            null);

        Assert.Contains("Section One", html);
    }

    [Fact]
    public void GetLayout_ContainsBreadcrumbs()
    {
        var html = LayoutProvider.GetLayout(
            "Title", "<p>content</p>",
            "<nav>nav</nav>", "<li>toc</li>",
            "<a href='/'>Home</a> / <a href='/page'>Page</a>",
            "<nav>pagination</nav>",
            null);

        Assert.Contains("Home", html);
        Assert.Contains("/page", html);
    }

    [Fact]
    public void GetLayout_ContainsPagination()
    {
        var html = LayoutProvider.GetLayout(
            "Title", "<p>content</p>",
            "<nav>nav</nav>", "<li>toc</li>",
            "<a href='/'>Home</a>",
            "<a href='/next' class=\"pagination-link next\">Next</a>",
            null);

        Assert.Contains("Next", html);
        Assert.Contains("/next", html);
    }

    [Fact]
    public void GetLayout_ContainsThemeCss_WhenProvided()
    {
        var themeCss = "<style>:root { --primary-color: red; }</style>";
        var html = LayoutProvider.GetLayout(
            "Title", "<p>content</p>",
            "<nav>nav</nav>", "<li>toc</li>",
            "<a href='/'>Home</a>", "<nav>pagination</nav>",
            themeCss);

        Assert.Contains("--primary-color: red;", html);
    }

    [Fact]
    public void GetLayout_UserThemeCss_ComesAfterTheBaseStylesheet()
    {
        // Same specificity, so document order decides; before this, theme.json and custom.css did nothing.
        var themeCss = "<style>:root, :root[data-theme=\"dark\"] { --accent: red; }</style>";
        var html = LayoutProvider.GetLayout(
            "Title", "<p>content</p>",
            "<nav>nav</nav>", "<li>toc</li>",
            "<a href='/'>Home</a>", "<nav>pagination</nav>",
            themeCss);

        var baseStylesheet = html.IndexOf("/bark.css?v=", StringComparison.Ordinal);
        var userOverride = html.IndexOf("--accent: red;", StringComparison.Ordinal);

        Assert.True(baseStylesheet >= 0, "layout emitted no base stylesheet link");
        Assert.True(userOverride > baseStylesheet, "user theme CSS must come after the base stylesheet");
    }

    [Fact]
    public void GetLayout_HtmlEncodesTitle()
    {
        var html = LayoutProvider.GetLayout(
            "Test <script>alert('xss')</script>", "<p>content</p>",
            "<nav>nav</nav>", "<li>toc</li>",
            "<a href='/'>Home</a>", "<nav>pagination</nav>",
            null);

        Assert.Contains("Test &lt;script&gt;", html);
        Assert.Contains("&lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;", html);
    }

    [Fact]
    public void GetLayout_HtmlEncodesDescription()
    {
        var html = LayoutProvider.GetLayout(
            "Title", "<p>content</p>",
            "<nav>nav</nav>", "<li>toc</li>",
            "<a href='/'>Home</a>", "<nav>pagination</nav>",
            null,
            description: "desc <script>alert('xss')</script>");

        Assert.Contains("desc &lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;", html);
        Assert.DoesNotContain("<script>alert('xss')</script>", html);
    }

    [Fact]
    public void GetLayout_DefaultLang_IsEn()
    {
        var html = LayoutProvider.GetLayout(
            "Title", "<p>content</p>",
            "<nav>nav</nav>", "<li>toc</li>",
            "<a href='/'>Home</a>", "<nav>pagination</nav>",
            null);

        Assert.Contains("lang=\"en\"", html);
    }

    [Fact]
    public void GetLayout_CustomLang_UsedInHtmlTag()
    {
        var html = LayoutProvider.GetLayout(
            "Title", "<p>content</p>",
            "<nav>nav</nav>", "<li>toc</li>",
            "<a href='/'>Home</a>", "<nav>pagination</nav>",
            null,
            lang: "fr");

        Assert.Contains("lang=\"fr\"", html);
        Assert.DoesNotContain("lang=\"en\"", html);
    }

    [Fact]
    public void GetLayout_HeadTagsHtml_InjectedInHead()
    {
        var html = LayoutProvider.GetLayout(
            "Title", "<p>content</p>",
            "<nav>nav</nav>", "<li>toc</li>",
            "<a href='/'>Home</a>", "<nav>pagination</nav>",
            null,
            headTagsHtml: "<meta name=\"robots\" content=\"noindex\">");

        Assert.Contains("<meta name=\"robots\" content=\"noindex\">", html);
        var headEnd = html.IndexOf("</head>", StringComparison.Ordinal);
        var metaPos = html.IndexOf("<meta name=\"robots\"", StringComparison.Ordinal);
        Assert.True(metaPos < headEnd, "head tag should appear before </head>");
    }

    [Fact]
    public void Get404Layout_DefaultLang_IsEn()
    {
        var html = LayoutProvider.Get404Layout(LayoutProvider.HtmlEncode);
        Assert.Contains("lang=\"en\"", html);
    }

    [Fact]
    public void Get404Layout_CustomLang_UsedInHtmlTag()
    {
        var html = LayoutProvider.Get404Layout(LayoutProvider.HtmlEncode, lang: "de");
        Assert.Contains("lang=\"de\"", html);
        Assert.DoesNotContain("lang=\"en\"", html);
    }

    [Fact]
    public void Get404Layout_Contains404()
    {
        var html = LayoutProvider.Get404Layout(LayoutProvider.HtmlEncode);
        Assert.Contains("404", html);
    }

    [Fact]
    public void Get404Layout_ContainsReturnHomeLink()
    {
        var html = LayoutProvider.Get404Layout(LayoutProvider.HtmlEncode);
        Assert.Contains("Return home", html);
        Assert.Contains("href=\"/\"", html);
    }

    [Fact]
    public void Get404Layout_DoesNotContainUserContent()
    {
        var html = LayoutProvider.Get404Layout(LayoutProvider.HtmlEncode);
        Assert.DoesNotContain("<p>content</p>", html);
    }

    [Fact]
    public void HtmlEncode_EncodesHtml()
    {
        var result = LayoutProvider.HtmlEncode("<script>alert('xss')</script>");
        Assert.Equal("&lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;", result);
    }

    [Fact]
    public void HtmlEncode_NullReturnsEmpty()
    {
        Assert.Equal("", LayoutProvider.HtmlEncode(null));
    }

    [Fact]
    public void HtmlEncode_EmptyReturnsEmpty()
    {
        Assert.Equal("", LayoutProvider.HtmlEncode(""));
    }

    [Fact]
    public void ResolveAssetUrl_AbsoluteUrl_ReturnsUnchanged()
    {
        var result = LayoutProvider.ResolveAssetUrl("https://example.com/asset.svg", "/docs");
        Assert.Equal("https://example.com/asset.svg", result);
    }

    [Fact]
    public void ResolveAssetUrl_RootRelativeWithBasePath_PrependsBasePath()
    {
        var result = LayoutProvider.ResolveAssetUrl("/asset.svg", "/docs");
        Assert.Equal("/docs/asset.svg", result);
    }

    [Fact]
    public void ResolveAssetUrl_RootRelativeNoBasePath_ReturnsRootRelative()
    {
        var result = LayoutProvider.ResolveAssetUrl("/asset.svg", "");
        Assert.Equal("/asset.svg", result);
    }

    [Fact]
    public void ResolveAssetUrl_RelativeUrl_ReturnsUnchanged()
    {
        var result = LayoutProvider.ResolveAssetUrl("asset.svg", "/docs");
        Assert.Equal("asset.svg", result);
    }

    [Fact]
    public void ResolveAssetUrl_Null_ReturnsNull()
    {
        var result = LayoutProvider.ResolveAssetUrl(null, "/docs");
        Assert.Null(result);
    }

    [Fact]
    public void ResolveAssetUrl_Empty_ReturnsNull()
    {
        var result = LayoutProvider.ResolveAssetUrl("", "/docs");
        Assert.Null(result);
    }

    [Fact]
    public void GetLayout_BrandImageRootRelative_WithBasePath()
    {
        var html = LayoutProvider.GetLayout(
            "Title", "<p>content</p>",
            "<nav>nav</nav>", "<li>toc</li>",
            "<a href='/'>Home</a>", "<nav>pagination</nav>",
            null,
            brandImage: "/brand.svg",
            basePath: "/docs");

        Assert.Contains("<img src=\"/docs/brand.svg\"", html);
    }

    [Fact]
    public void GetLayout_BrandImageRootRelativeNoBasePath_StaysRootRelative()
    {
        var html = LayoutProvider.GetLayout(
            "Title", "<p>content</p>",
            "<nav>nav</nav>", "<li>toc</li>",
            "<a href='/'>Home</a>", "<nav>pagination</nav>",
            null,
            brandImage: "/brand.svg");

        Assert.Contains("<img src=\"/brand.svg\"", html);
    }

    [Fact]
    public void GetLayout_BrandImageAbsoluteUrl_Unchanged()
    {
        var html = LayoutProvider.GetLayout(
            "Title", "<p>content</p>",
            "<nav>nav</nav>", "<li>toc</li>",
            "<a href='/'>Home</a>", "<nav>pagination</nav>",
            null,
            brandImage: "https://cdn.example.com/logo.svg",
            basePath: "/docs");

        Assert.Contains("<img src=\"https://cdn.example.com/logo.svg\"", html);
    }

    [Fact]
    public void GetLayout_BlankBrandImage_RendersNoImg()
    {
        var html = LayoutProvider.GetLayout(
            "Title", "<p>content</p>",
            "<nav>nav</nav>", "<li>toc</li>",
            "<a href='/'>Home</a>", "<nav>pagination</nav>",
            null,
            brandImage: "",
            basePath: "/docs");

        Assert.DoesNotContain("<img src=\"\"", html);
    }

    [Fact]
    public void GetLayout_FaviconRootRelative_WithBasePath()
    {
        var html = LayoutProvider.GetLayout(
            "Title", "<p>content</p>",
            "<nav>nav</nav>", "<li>toc</li>",
            "<a href='/'>Home</a>", "<nav>pagination</nav>",
            null,
            favicon: "/icon.ico",
            basePath: "/docs");

        Assert.Contains("<link rel=\"icon\" href=\"/docs/icon.ico\"", html);
    }

    [Fact]
    public void GetLayout_FaviconAbsoluteUrl_Unchanged()
    {
        var html = LayoutProvider.GetLayout(
            "Title", "<p>content</p>",
            "<nav>nav</nav>", "<li>toc</li>",
            "<a href='/'>Home</a>", "<nav>pagination</nav>",
            null,
            favicon: "https://example.com/favicon.ico",
            basePath: "/docs");

        Assert.Contains("<link rel=\"icon\" href=\"https://example.com/favicon.ico\"", html);
    }

    [Fact]
    public void GetLayout_FaviconEmoji_FallbackToDataUri()
    {
        var html = LayoutProvider.GetLayout(
            "Title", "<p>content</p>",
            "<nav>nav</nav>", "<li>toc</li>",
            "<a href='/'>Home</a>", "<nav>pagination</nav>",
            null,
            favicon: "🔥");

        Assert.Contains("<link rel=\"icon\" href=\"data:image/svg+xml;base64,", html);
        Assert.DoesNotContain("/🔥", html);
    }

    [Fact]
    public void GetLayout_HomePage_MobileDrawerHasTopNavButNoSidebar()
    {
        var html = LayoutProvider.GetLayout(
            "Home", "<p>content</p>",
            "<nav>SIDEBAR_TREE</nav>", null,
            "<a href='/'>Home</a>", "",
            null,
            topNavHtml: "<nav>DESKTOP_TOP_NAV</nav>",
            mobileTopNavHtml: "<nav>MOBILE_TOP_NAV</nav>",
            isHomePage: true);

        Assert.DoesNotContain("SIDEBAR_TREE", html);
        Assert.Contains("MOBILE_TOP_NAV", html);
        Assert.Contains("id=\"menu-toggle\"", html);
    }

    [Fact]
    public void GetLayout_ContentPage_MobileDrawerKeepsSidebar()
    {
        var html = LayoutProvider.GetLayout(
            "Guide", "<p>content</p>",
            "<nav>SIDEBAR_TREE</nav>", null,
            "<a href='/'>Home</a>", "",
            null,
            mobileTopNavHtml: "<nav>MOBILE_TOP_NAV</nav>");

        Assert.Contains("SIDEBAR_TREE", html);
        Assert.Contains("MOBILE_TOP_NAV", html);
    }
}
