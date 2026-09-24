namespace Bark.Models;

using System.Text.Json.Serialization;

public class Config
{
    public string? Title { get; set; }
    public string? TitleTemplate { get; set; }
    public string? Description { get; set; }
    public string? Lang { get; set; }
    public List<HeadTag>? Head { get; set; }

    [JsonConverter(typeof(LocaleOptionsConverter))]
    public LocaleOptions? Locale { get; set; }

    public Dictionary<string, LocaleEntry>? Locales { get; set; }

    public static LocaleOptions? ResolveLocale(Config? config)
    {
        if (config is null || (config.Locale is null && config.Lang is null))
            return null;

        return new LocaleOptions
        {
            Code = config.Locale?.Code ?? config.Lang
        };
    }

    /// <summary>Theme name, e.g. <c>"forest"</c>. Unknown names fall back to the default theme.</summary>
    public string? Theme { get; set; }

    public string? Brand { get; set; }
    public string? BrandImage { get; set; }
    public string? Image { get; set; }
    public string? Footer { get; set; }
    public string? Favicon { get; set; }
    public string? Repo { get; set; }

    /// <summary>Markdown for a dismissible announcement bar above the header; null disables it</summary>
    public string? Promo { get; set; }

    /// <summary>Flat sidebar. Superseded by <see cref="Sidebar"/> when present.</summary>
    public List<NavEntry>? Nav { get; set; }

    /// <summary>Path-prefix-keyed sidebars, e.g. <c>{"/guide/": [...]}</c>. Longest matching prefix wins, falls back to <see cref="Nav"/>.</summary>
    public Dictionary<string, List<NavEntry>>? Sidebar { get; set; }

    /// <summary>Header nav bar. Each item is a direct link or a dropdown with <see cref="TopNavItem.Items"/>.</summary>
    public List<TopNavItem>? TopNav { get; set; }

    public List<SocialLink>? SocialLinks { get; set; }

    public List<TopNavItem>? BottomNav { get; set; }

    public PageControlsConfig? PageControls { get; set; }

    /// <summary>Site-wide "Last updated" stamp toggle (uses file mtime). Off by default; a page can opt out via <c>lastUpdated: false</c>.</summary>
    public bool LastUpdated { get; set; }

    /// <summary>"Edit this page" link near the pagination footer. Null disables it.</summary>
    public EditLinkConfig? EditLink { get; set; }

    /// <summary>Hosts a front-matter <c>redirect:</c> may send visitors to off-site. Same-host redirects always work.</summary>
    public List<string>? RedirectHosts { get; set; }
}
