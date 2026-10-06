using System.Text.RegularExpressions;
using Bark.Models;
using Markdig.Extensions.CustomContainers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Bark.Services.MarkdownExtensions;

/// <summary>Renders <c>::: name</c> ... <c>:::</c> blocks: tip/info/warning/danger/details, code-group and cards.</summary>
public sealed partial class ContainerRenderer : HtmlObjectRenderer<CustomContainer>
{
    private static readonly Dictionary<string, string> DefaultTitles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tip"] = "TIP",
        ["info"] = "INFO",
        ["warning"] = "WARNING",
        ["danger"] = "DANGER",
        ["details"] = "Details",
    };

    private readonly FencedCodeBlockRenderer _codeBlockRenderer;
    private readonly CodeGroupIconOptions _icons;
    private readonly string _basePath;

    public ContainerRenderer(FencedCodeBlockRenderer codeBlockRenderer, CodeGroupIconOptions? icons = null, string basePath = "")
    {
        _codeBlockRenderer = codeBlockRenderer;
        _icons = icons ?? new CodeGroupIconOptions();
        _basePath = basePath;
    }

    protected override void Write(HtmlRenderer renderer, CustomContainer obj)
    {
        var name = (obj.Info ?? string.Empty).Trim().ToLowerInvariant();

        if (name == "code-group")
        {
            WriteCodeGroup(renderer, obj);
            return;
        }

        if (name == "cards")
        {
            WriteCards(renderer, obj);
            return;
        }

        if (string.IsNullOrEmpty(name))
        {
            WriteCustomBlock(renderer, obj, "custom-block", "");
            return;
        }

        var defaultTitle = DefaultTitles.TryGetValue(name, out var known)
            ? known
            : char.ToUpperInvariant(name[0]) + name[1..];

        WriteCustomBlock(renderer, obj, name, defaultTitle);
    }

    private static void WriteCustomBlock(HtmlRenderer renderer, CustomContainer obj, string klass, string defaultTitle)
    {
        renderer.EnsureLine();

        var info = (obj.Arguments ?? string.Empty).Trim();
        var hasCustomTitle = !string.IsNullOrEmpty(info);
        var title = hasCustomTitle ? info : defaultTitle;
        var titleClass = hasCustomTitle ? "custom-block-title" : "custom-block-title custom-block-title-default";

        if (klass == "details")
        {
            renderer.Write("<details class=\"details custom-block\"><summary>")
                .WriteEscape(title)
                .Write("</summary>\n");
            renderer.WriteChildren(obj);
            renderer.WriteLine("</details>");
        }
        else
        {
            renderer.Write("<div class=\"").Write(klass).Write(" custom-block\"><p class=\"")
                .Write(titleClass).Write("\">").WriteEscape(title).Write("</p>\n");
            renderer.WriteChildren(obj);
            renderer.WriteLine("</div>");
        }

        renderer.EnsureLine();
    }

    // Leading link or bold text: card title. Remaining content: card body.
    private static void WriteCards(HtmlRenderer renderer, CustomContainer obj)
    {
        renderer.EnsureLine();
        renderer.Write("<div class=\"bark-features bark-cards\"");
        if (int.TryParse(obj.Arguments, out var cols) && cols is >= 1 and <= 6)
            renderer.Write(" style=\"--cols:").Write(cols.ToString()).Write('"');
        renderer.Write('>');

        foreach (var item in obj.OfType<ListBlock>().SelectMany(list => list.OfType<ListItemBlock>()))
        {
            var lead = (item.FirstOrDefault() as ParagraphBlock)?.Inline;
            Inline? title = lead?.FirstChild is LinkInline { IsImage: false } or EmphasisInline { DelimiterCount: 2 } ? lead.FirstChild : null;
            var link = title as LinkInline;

            if (link is not null)
                renderer.Write("<a class=\"bark-feature\" href=\"").WriteEscapeUrl(link.GetDynamicUrl?.Invoke() ?? link.Url).Write("\">");
            else
                renderer.Write("<div class=\"bark-feature\">");

            if (title is ContainerInline titleInline)
            {
                renderer.Write("<p class=\"bark-feature-title\">");
                renderer.WriteChildren(titleInline);
                renderer.Write("</p>");
            }

            var body = title?.NextSibling ?? lead?.FirstChild;
            while (body is LineBreakInline)
                body = body.NextSibling;
            if (body is not null)
            {
                renderer.Write("<p class=\"bark-feature-details\">");
                for (; body is not null; body = body.NextSibling)
                    renderer.Write(body);
                renderer.Write("</p>");
            }

            foreach (var block in item.Skip(1))
                renderer.Render(block);

            renderer.Write(link is not null ? "</a>" : "</div>");
        }

        renderer.Write("</div>");
        renderer.EnsureLine();
    }

    private void WriteCodeGroup(HtmlRenderer renderer, CustomContainer obj)
    {
        renderer.EnsureLine();
        renderer.Write("<div class=\"bark-code-group\"><div class=\"tabs\">");

        var groupId = obj.Line;
        var tabIndex = 0;
        foreach (var child in obj)
        {
            if (child is not FencedCodeBlock fence)
                continue;

            var meta = CodeBlockMeta.Parse(fence.Info, fence.Arguments);
            var title = meta.Title ?? (string.IsNullOrEmpty(meta.Lang) ? "txt" : meta.Lang);
            var tabId = $"tab-{groupId}-{tabIndex}";

            renderer.Write("<input type=\"radio\" name=\"group-").Write(groupId.ToString()).Write("\" id=\"").Write(tabId).Write('"');
            if (tabIndex == 0)
                renderer.Write(" checked");
            renderer.Write("><label data-title=\"").WriteEscape(title).Write("\" for=\"").Write(tabId).Write("\">");

            if (_icons.Enabled)
                renderer.Write(BuildIconTag(title, meta.IconSlug));

            renderer.WriteEscape(title).Write("</label>");

            tabIndex++;
        }

        renderer.Write("</div><div class=\"blocks\">");

        var isFirst = true;
        foreach (var child in obj)
        {
            if (child is FencedCodeBlock fence)
            {
                _codeBlockRenderer.WriteFenced(renderer, fence, forceActive: isFirst, isCodeGroupChild: true);
                isFirst = false;
            }
            else
            {
                renderer.Render(child);
            }
        }

        renderer.Write("</div></div>");
        renderer.EnsureLine();
    }

    private string BuildIconTag(string title, string? iconSlug)
    {
        var slug = iconSlug
            ?? (_icons.Overrides is { } overrides && overrides.TryGetValue(title, out var mapped)
                ? mapped
                : SlugRegex().Replace(title.ToLowerInvariant(), "-").Trim('-'));

        var iconBase = IsRootRelative(_icons.BaseUrl) ? PrefixBasePath(_icons.BaseUrl) : _icons.BaseUrl;
        var src = System.Net.WebUtility.HtmlEncode($"{iconBase}/{slug}.{_icons.Format}");
        // A masked background instead of a filtered <img>: filter's raster pass blurs these small icons to a solid blob in dark mode.
        return $"<span class=\"tab-icon\" style=\"--icon:url('{src}')\" aria-hidden=\"true\"></span>";
    }

    private static bool IsRootRelative(string url) =>
        url.StartsWith('/') && !url.StartsWith("//");

    private string PrefixBasePath(string url)
    {
        if (string.IsNullOrEmpty(_basePath))
            return url;
        if (url.StartsWith(_basePath, StringComparison.Ordinal)
            && (url.Length == _basePath.Length || url[_basePath.Length] == '/'))
            return url;
        return $"{_basePath}{url}";
    }

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex SlugRegex();
}
