using Bark.Configuration;
using Bark.Models;
using Bark.Services.Translation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bark.Tests;

public sealed class MarkdownTranslatorTests
{
    private static Task<string> Shout(string text, CancellationToken cancellationToken) =>
        Task.FromResult(text.ToUpperInvariant());

    [Fact]
    public async Task ProseIsTranslated()
    {
        var result = await MarkdownTranslator.TranslateAsync("Hello there.\n", Shout);

        Assert.Contains("HELLO THERE.", result);
    }

    [Fact]
    public async Task FencedCodeIsLeftAlone()
    {
        var markdown = "Intro line.\n\n```csharp\nvar greeting = \"hello\";\n```\n\nOutro line.\n";

        var result = await MarkdownTranslator.TranslateAsync(markdown, Shout);

        Assert.Contains("var greeting = \"hello\";", result);
        Assert.Contains("INTRO LINE.", result);
        Assert.Contains("OUTRO LINE.", result);
    }

    [Fact]
    public async Task InlineCodeAndLinkTargetsSurvive()
    {
        var markdown = "Run `dotnet build` and read the [guide](/guide/install/).\n";

        var result = await MarkdownTranslator.TranslateAsync(markdown, Shout);

        Assert.Contains("`dotnet build`", result);
        Assert.Contains("[guide](/guide/install/)", result);
        Assert.DoesNotContain("[[0]]", result);
    }

    [Fact]
    public async Task HeadingsKeepTheirOriginalAnchor()
    {
        var result = await MarkdownTranslator.TranslateAsync("## Getting started\n", Shout);

        Assert.Contains("## GETTING STARTED {#getting-started}", result);
    }

    [Fact]
    public async Task HeadingWithAnExplicitAnchorIsNotGivenASecondOne()
    {
        var result = await MarkdownTranslator.TranslateAsync("## Getting started {#start}\n", Shout);

        Assert.Contains("{#start}", result);
        Assert.DoesNotContain("{#getting-started}", result);
    }

    [Fact]
    public async Task FrontMatterTitleIsTranslatedAndTheFileIsFlagged()
    {
        var markdown = "---\ntitle: Install\nlayout: home\n---\n\nBody text.\n";

        var result = await MarkdownTranslator.TranslateAsync(markdown, Shout);

        Assert.Contains("title: INSTALL", result);
        Assert.Contains("layout: home", result);
        Assert.Contains("machineTranslated: true", result);
    }

    [Fact]
    public async Task FileWithoutFrontMatterGetsOne()
    {
        var result = await MarkdownTranslator.TranslateAsync("Just a line.\n", Shout);

        Assert.StartsWith("---\nmachineTranslated: true\n---", result);
    }

    [Fact]
    public async Task ContainerMarkersAndHtmlAreLeftAlone()
    {
        var markdown = "::: tip\nUseful hint.\n:::\n\n<div class=\"x\">raw</div>\n";

        var result = await MarkdownTranslator.TranslateAsync(markdown, Shout);

        Assert.Contains("::: tip", result);
        Assert.Contains("<div class=\"x\">raw</div>", result);
        Assert.Contains("USEFUL HINT.", result);
    }

    [Fact]
    public async Task ListMarkersAreKept()
    {
        var result = await MarkdownTranslator.TranslateAsync("- first item\n- second item\n", Shout);

        Assert.Contains("- FIRST ITEM", result);
        Assert.Contains("- SECOND ITEM", result);
    }
}

public sealed class TranslationRunnerTests : IDisposable
{
    private readonly string _docsDir = Path.Combine(Path.GetTempPath(), "bark-translate-" + Guid.NewGuid().ToString("N"));

    public TranslationRunnerTests()
    {
        Directory.CreateDirectory(Path.Combine(_docsDir, "guide"));
        File.WriteAllText(Path.Combine(_docsDir, "index.md"), "---\ntitle: Home\n---\n\nWelcome.\n");
        File.WriteAllText(Path.Combine(_docsDir, "guide", "install.md"), "---\ntitle: Install\n---\n\nInstall it.\n");
    }

    public void Dispose()
    {
        if (Directory.Exists(_docsDir))
            Directory.Delete(_docsDir, true);
    }

    private static Task<string> Shout(string text, CancellationToken cancellationToken) =>
        Task.FromResult(text.ToUpperInvariant());

    private Config LocaleConfig() => new()
    {
        Locales = new Dictionary<string, LocaleEntry>
        {
            ["root"] = new() { Label = "English", Lang = "en" },
            ["nl"] = new() { Label = "Nederlands", Lang = "nl" }
        }
    };

    [Fact]
    public async Task WritesATranslatedTreeMirroringTheSource()
    {
        var outcome = await TranslationRunner.RunAsync(
            new TranslationRequest(_docsDir, "nl", Overwrite: false),
            LocaleConfig(), Shout, NullLogger.Instance);

        Assert.Equal(2, outcome.Written);
        Assert.True(File.Exists(Path.Combine(_docsDir, "nl", "index.md")));
        Assert.Contains("INSTALL IT.", await File.ReadAllTextAsync(Path.Combine(_docsDir, "nl", "guide", "install.md")));
        Assert.Contains("WELCOME.", await File.ReadAllTextAsync(Path.Combine(_docsDir, "nl", "index.md")));
    }

    [Fact]
    public async Task TranslatesVersionTreesIntoTheLocaleTree()
    {
        Directory.CreateDirectory(Path.Combine(_docsDir, "v1.0"));
        await File.WriteAllTextAsync(Path.Combine(_docsDir, "v1.0", "index.md"), "Old welcome.\n");
        var config = LocaleConfig();
        config.Versions = new() { ["root"] = new(), ["v1.0"] = new() };

        var outcome = await TranslationRunner.RunAsync(new TranslationRequest(_docsDir, "nl", Overwrite: false), config, Shout, NullLogger.Instance);

        Assert.Equal(3, outcome.Written);
        Assert.Contains("OLD WELCOME.", await File.ReadAllTextAsync(Path.Combine(_docsDir, "nl", "v1.0", "index.md")));
    }

    [Fact]
    public async Task DoesNotTranslateAnExistingTranslationTree()
    {
        await TranslationRunner.RunAsync(new TranslationRequest(_docsDir, "nl", Overwrite: false), LocaleConfig(), Shout, NullLogger.Instance);
        var second = await TranslationRunner.RunAsync(new TranslationRequest(_docsDir, "nl", Overwrite: false), LocaleConfig(), Shout, NullLogger.Instance);

        Assert.Equal(0, second.Written);
        Assert.Equal(2, second.Skipped);
    }

    [Fact]
    public async Task OverwriteRewritesExistingFiles()
    {
        await TranslationRunner.RunAsync(new TranslationRequest(_docsDir, "nl", Overwrite: false), LocaleConfig(), Shout, NullLogger.Instance);
        var second = await TranslationRunner.RunAsync(new TranslationRequest(_docsDir, "nl", Overwrite: true), LocaleConfig(), Shout, NullLogger.Instance);

        Assert.Equal(2, second.Written);
    }

    [Fact]
    public void CliParsesTheTranslateFlags()
    {
        var parsed = CliArguments.Parse(["--translate", "nl", "--translate-endpoint", "http://lt:5000", "--translate-from", "en", "--translate-overwrite"]);

        Assert.Equal("nl", parsed.TranslateTo);
        Assert.Equal("http://lt:5000", parsed.TranslateEndpoint);
        Assert.Equal("en", parsed.TranslateFrom);
        Assert.True(parsed.TranslateOverwrite);
    }
}
