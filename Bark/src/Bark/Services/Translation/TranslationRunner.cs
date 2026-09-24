using Bark.Models;
using Bark.Services.Rendering;

namespace Bark.Services.Translation;

public sealed record TranslationRequest(string DocsPath, string TargetCode, bool Overwrite);

public sealed record TranslationOutcome(int Written, int Skipped);

public static class TranslationRunner
{
    public static async Task<TranslationOutcome> RunAsync(
        TranslationRequest request,
        Config? config,
        TranslateText translate,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var treeCodes = LocaleRouting.TreeCodes(config);
        var targetDir = Path.Combine(request.DocsPath, request.TargetCode);
        var written = 0;
        var skipped = 0;

        foreach (var file in Directory.GetFiles(request.DocsPath, "*.md", ContentLinks.NoLinks).Order())
        {
            var relative = Path.GetRelativePath(request.DocsPath, file).Replace('\\', '/');
            var head = relative.Split('/')[0];
            if (treeCodes.Contains(head, StringComparer.OrdinalIgnoreCase))
                continue;

            var destination = Path.Combine(targetDir, relative.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(destination) && !request.Overwrite)
            {
                skipped++;
                continue;
            }

            var source = await File.ReadAllTextAsync(file, cancellationToken);
            var translated = await MarkdownTranslator.TranslateAsync(source, translate, cancellationToken);

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            await File.WriteAllTextAsync(destination, translated, cancellationToken);
            written++;
            logger.LogInformation("Translated {Source} to {Destination}", relative, Path.GetRelativePath(request.DocsPath, destination));
        }

        return new TranslationOutcome(written, skipped);
    }
}
