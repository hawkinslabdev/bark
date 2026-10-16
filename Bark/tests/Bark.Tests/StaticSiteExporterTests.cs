using Bark.Models;
using Bark.Services;

namespace Bark.Tests;

public sealed class StaticSiteExporterTests
{
    [Fact]
    public void ExportPaths_WritesRootPagesUnderEveryLocalePrefix()
    {
        var config = new Config { Lang = "en", Locales = new() { ["root"] = new(), ["nl"] = new(), ["de"] = new() } };
        var paths = StaticSiteExporter.ExportPaths(["index", "guide/what-is-bark", "nl"], config);

        Assert.Contains("nl/guide/what-is-bark", paths);
        Assert.Contains("de/guide/what-is-bark", paths);
        Assert.Contains("de", paths);
        // The translated nl index already exists; the fallback must not duplicate it.
        Assert.Single(paths, p => p == "nl");
        // A locale page is never re-prefixed with another locale.
        Assert.DoesNotContain("de/nl", paths);
    }
}
