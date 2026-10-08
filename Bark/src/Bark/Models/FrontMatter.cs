namespace Bark.Models;

public sealed record FrontMatter
{
    public string? Title { get; init; }
    public string? Description { get; init; }

    /// <summary>Social preview image (og:image / twitter:image). Absolute URL, or root-relative path resolved against the request origin.</summary>
    public string? Image { get; init; }

    /// <summary>Set to <c>"home"</c> to render <see cref="Hero"/>/<see cref="Features"/> instead of standard docs chrome.</summary>
    public string? Layout { get; init; }

    public HeroFrontMatter? Hero { get; init; }
    public List<FeatureFrontMatter>? Features { get; init; }

    public List<string>? Keywords { get; init; }

    /// <summary>Per-page override for <c>Config.LastUpdated</c>. <c>false</c> hides the
    /// "Last updated" stamp on this page even when the site-wide setting is on.</summary>
    public bool? LastUpdated { get; init; }

    /// <summary>Set to <c>false</c> to hide prev/next pagination links on this page.</summary>
    public bool? Pagination { get; init; }

    /// <summary>Set to <c>false</c> to hide the table of contents on this page.</summary>
    public bool? Toc { get; init; }

    /// <summary>Set to <c>false</c> to hide the left sidebar on this page.</summary>
    public bool? Sidebar { get; init; }

    /// <summary>Content width as a percentage of the available width, for example <c>80%</c>. Accepts 10 to 100.</summary>
    public string? Width { get; init; }

    public int? WidthPercent =>
        int.TryParse(Width?.Trim().TrimEnd('%'), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var n) && n is >= 10 and <= 100
            ? n
            : null;

    /// <summary>When set, the page issues a 307 redirect to this URL instead of rendering.
    /// Root-relative paths (starting with <c>/</c>) are prefixed with the configured base path.
    /// Absolute URLs are used as-is.</summary>
    public string? Redirect { get; init; }

    /// <summary>Content creation date (ISO 8601). Used as the "Last updated" display value
    /// when <see cref="Updated"/> is absent. Overrides file system mtime.</summary>
    public DateTime? Date { get; init; }

    /// <summary>Last-modified date (ISO 8601). Takes priority over <see cref="Date"/> and
    /// file system mtime for the "Last updated" display.</summary>
    public DateTime? Updated { get; init; }

    public bool? MachineTranslated { get; init; }

    /// <summary>API reference source: <c>&lt;spec file&gt; &lt;selector&gt; &lt;name&gt;</c>, for example <c>openapi.yaml POST /tasks</c>.</summary>
    public string? Api { get; init; }

    /// <summary>API section source: a spec file. Generates one page per operation and object in this folder, and the folder sidebar.</summary>
    public string? ApiSpec { get; init; }

    /// <summary>API base URL. Overrides the spec servers; required for GraphQL and OData specs without one.</summary>
    public string? Server { get; init; }

    /// <summary>API authentication: <c>bearer</c>, <c>basic</c> or <c>apiKey &lt;header|query&gt; &lt;name&gt;</c>. Overrides the spec security.</summary>
    public string? Auth { get; init; }
}
