namespace Bark.Models;

/// <summary>4-element <see cref="Deconstruct(out string, out string?, out string?, out List{HeadingInfo})"/> kept for older call sites that don't need <see cref="Layout"/>/<see cref="ShowLastUpdated"/></summary>
public sealed record MarkdownParseResult(
    string Html,
    string? Title,
    string? Description,
    List<HeadingInfo> Headings,
    string? Layout,
    bool ShowLastUpdated,
    IReadOnlyList<string>? Keywords = null,
    bool ShowPagination = true,
    string? Redirect = null,
    DateTime? FrontmatterDate = null,
    bool ShowToc = true,
    string? Image = null,
    bool MachineTranslated = false,
    FrontMatter? FrontMatter = null)
{
    public void Deconstruct(out string html, out string? title, out string? description, out List<HeadingInfo> headings)
    {
        html = Html;
        title = Title;
        description = Description;
        headings = Headings;
    }
}
