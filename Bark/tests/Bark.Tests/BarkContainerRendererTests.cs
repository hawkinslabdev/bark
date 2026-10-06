using Bark.Models;
using Bark.Services;

namespace Bark.Tests;

public sealed class ContainerRendererTests
{
    private const string CodeGroupMd = "::: code-group\n```sh [npm]\nnpm install\n```\n```sh [pnpm]\npnpm install\n```\n:::\n";

    [Fact]
    public void CodeGroup_IconsEnabledByDefault_RendersImgWithSlugifiedTitle()
    {
        var options = new CodeGroupIconOptions();
        var service = new MarkdownService(codeGroupIcons: options);
        var (html, _, _, _) = service.Parse(CodeGroupMd);

        Assert.Contains($"{options.BaseUrl}/npm.{options.Format}", html);
        Assert.Contains($"{options.BaseUrl}/pnpm.{options.Format}", html);
    }

    [Fact]
    public void CodeGroup_IconsDisabled_NoImgTag()
    {
        var options = new CodeGroupIconOptions { Enabled = false };
        var service = new MarkdownService(codeGroupIcons: options);
        var (html, _, _, _) = service.Parse(CodeGroupMd);
        Assert.DoesNotContain("<img", html);
    }

    [Fact]
    public void CodeGroup_OverrideWinsOverSlugifiedTitle()
    {
        var options = new CodeGroupIconOptions
        {
            Overrides = new Dictionary<string, string> { ["npm"] = "nodedotjs" }
        };
        var service = new MarkdownService(codeGroupIcons: options);
        var (html, _, _, _) = service.Parse(CodeGroupMd);

        Assert.Contains($"{options.BaseUrl}/nodedotjs.{options.Format}", html);
    }

    [Fact]
    public void CodeGroup_InlineIconDirective_WinsOverSlugifiedTitleAndGlobalOverride()
    {
        var options = new CodeGroupIconOptions
        {
            Overrides = new Dictionary<string, string> { ["csharp"] = "sharp" }
        };
        var service = new MarkdownService(codeGroupIcons: options);
        var md = "::: code-group\n```sh [csharp icon:dotnet]\ndotnet run\n```\n:::\n";
        var (html, _, _, _) = service.Parse(md);

        Assert.Contains($"{options.BaseUrl}/dotnet.{options.Format}", html);
        Assert.DoesNotContain($"{options.BaseUrl}/sharp.{options.Format}", html);
        Assert.Contains("data-title=\"csharp\"", html);
    }

    [Theory]
    [InlineData("3", " style=\"--cols:3\"")]
    [InlineData("", "")]
    [InlineData("9", "")]
    [InlineData("x", "")]
    public void Cards_ColumnArgument_SetsColsOnlyForOneToSix(string arg, string expectedAttr)
    {
        var (html, _, _, _) = new MarkdownService().Parse($"::: cards {arg}\n- [A](/a)\n:::\n");
        Assert.Contains($"<div class=\"bark-features bark-cards\"{expectedAttr}>", html);
    }
}
