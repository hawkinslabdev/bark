using System.Text;
using System.Text.RegularExpressions;

namespace Bark.Services.Translation;

public delegate Task<string> TranslateText(string text, CancellationToken cancellationToken);

public static partial class MarkdownTranslator
{
    [GeneratedRegex(@"`[^`\n]*`|!?\[[^\]]*\]\([^)]*\)|<[^>\n]+>|\{#[^}\n]+\}|\$\$?[^$\n]*\$\$?", RegexOptions.Compiled)]
    private static partial Regex ProtectedSpans();

    [GeneratedRegex(@"^(\s*(?:[-*+]\s+|\d+[.)]\s+|>\s*|#{1,6}\s+|\|\s*)*)(.*)$", RegexOptions.Compiled)]
    private static partial Regex LinePrefix();

    [GeneratedRegex(@"^(#{1,6})\s+(.*)$", RegexOptions.Compiled)]
    private static partial Regex Heading();

    private const string FrontMatterFence = "---";

    public static async Task<string> TranslateAsync(string markdown, TranslateText translate, CancellationToken cancellationToken = default)
    {
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var output = new StringBuilder();
        var inFrontMatter = false;
        var frontMatterDone = false;
        string? codeFence = null;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.TrimStart();

            if (i == 0 && trimmed == FrontMatterFence)
            {
                inFrontMatter = true;
                output.Append(line).Append('\n');
                continue;
            }

            if (inFrontMatter)
            {
                if (trimmed == FrontMatterFence)
                {
                    inFrontMatter = false;
                    frontMatterDone = true;
                    output.Append("machineTranslated: true").Append('\n').Append(line).Append('\n');
                    continue;
                }

                output.Append(await TranslateFrontMatterLineAsync(line, translate, cancellationToken)).Append('\n');
                continue;
            }

            if (codeFence is null && (trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal)))
            {
                codeFence = trimmed[..3];
                output.Append(line).Append('\n');
                continue;
            }

            if (codeFence is not null)
            {
                if (trimmed.StartsWith(codeFence, StringComparison.Ordinal))
                    codeFence = null;

                output.Append(line).Append('\n');
                continue;
            }

            if (IsSkippable(line))
            {
                output.Append(line).Append('\n');
                continue;
            }

            output.Append(await TranslateLineAsync(line, translate, cancellationToken)).Append('\n');
        }

        var result = output.ToString();
        if (!frontMatterDone)
            result = $"---\nmachineTranslated: true\n---\n\n{result.TrimStart('\n')}";

        return markdown.EndsWith('\n') ? result : result.TrimEnd('\n');
    }

    private static bool IsSkippable(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0)
            return true;

        if (line.StartsWith("    ", StringComparison.Ordinal) || line.StartsWith('\t'))
            return true;

        if (trimmed.StartsWith(":::", StringComparison.Ordinal) || trimmed.StartsWith('<'))
            return true;

        if (trimmed.All(c => c is '-' or '=' or '|' or ':' or ' '))
            return true;

        return false;
    }

    private static async Task<string> TranslateFrontMatterLineAsync(string line, TranslateText translate, CancellationToken cancellationToken)
    {
        var separator = line.IndexOf(':');
        if (separator < 0)
            return line;

        var key = line[..separator].Trim();
        if (key is not ("title" or "description"))
            return line;

        var value = line[(separator + 1)..].Trim();
        if (value.Length == 0 || value.StartsWith('[') || value.StartsWith('{'))
            return line;

        var quote = value.Length > 1 && (value[0] == '"' || value[0] == '\'') && value[^1] == value[0] ? value[0] : '\0';
        var bare = quote == '\0' ? value : value[1..^1];
        var translated = await translate(bare, cancellationToken);

        return quote == '\0' ? $"{key}: {translated}" : $"{key}: {quote}{translated}{quote}";
    }

    private static async Task<string> TranslateLineAsync(string line, TranslateText translate, CancellationToken cancellationToken)
    {
        var match = LinePrefix().Match(line);
        var prefix = match.Groups[1].Value;
        var body = match.Groups[2].Value;
        if (body.Trim().Length == 0)
            return line;

        var heading = Heading().Match(line);
        var anchor = heading.Success ? $" {{#{MarkdownService.Slugify(ProtectedSpans().Replace(heading.Groups[2].Value, string.Empty).Trim())}}}" : string.Empty;
        if (heading.Success && heading.Groups[2].Value.Contains("{#", StringComparison.Ordinal))
            anchor = string.Empty;

        var placeholders = new List<string>();
        var masked = ProtectedSpans().Replace(body, m =>
        {
            placeholders.Add(m.Value);
            return $"[[{placeholders.Count - 1}]]";
        });

        if (masked.Trim().Length == 0 || masked.Trim().All(c => !char.IsLetter(c)))
            return line;

        var translated = await translate(masked, cancellationToken);

        for (var i = 0; i < placeholders.Count; i++)
            translated = translated.Replace($"[[{i}]]", placeholders[i], StringComparison.Ordinal);

        return prefix + translated + anchor;
    }
}
