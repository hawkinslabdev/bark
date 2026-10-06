namespace Bark.Models;

public sealed record DocumentationPage(
    string Path,
    string Title,
    string HtmlContent,
    string? Description = null,
    DateTime? LastModified = null,
    IReadOnlyList<HeadingInfo> Headings = default!,
    string? Layout = null,
    bool ShowLastUpdated = true,
    string? OriginalRelativePath = null,
    IReadOnlyList<string>? Keywords = null,
    bool ShowPagination = true,
    string? Redirect = null,
    bool ShowToc = true,
    string? Image = null,
    bool MachineTranslated = false,
    IReadOnlyList<string>? ConnectSources = null,
    string? ApiMethod = null
)
{
    public DocumentationPage(
        string path,
        string title,
        string htmlContent,
        string? description = null,
        DateTime? lastModified = null,
        IEnumerable<HeadingInfo>? headings = null,
        string? layout = null,
        bool showLastUpdated = true,
        string? originalRelativePath = null,
        IReadOnlyList<string>? keywords = null,
        bool showPagination = true,
        string? redirect = null,
        bool showToc = true,
        string? image = null
    ) : this(
        path,
        title,
        htmlContent,
        description,
        lastModified,
        (headings ?? Array.Empty<HeadingInfo>()).ToList().AsReadOnly(),
        layout,
        showLastUpdated,
        originalRelativePath,
        keywords,
        showPagination,
        redirect,
        showToc,
        image
    )
    { }
}
