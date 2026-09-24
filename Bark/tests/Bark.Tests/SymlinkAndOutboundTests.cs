using System.Net;
using Bark.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Bark.Tests;

/// <summary>Contributor-writable content must not reach server files through symlinks, leave the origin, or steer requests inward</summary>
public sealed class SymlinkAndOutboundTests : IDisposable
{
    private const string Secret = "dummy-secret-7731";
    private readonly string _outside = TempDir();
    private readonly string _docs = TempDir();

    private static string TempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "bark-links-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private sealed class Factory(string docs) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("urls", "http://127.0.0.1:0");
            builder.UseSetting("Docs:RootPath", docs);
            builder.UseSetting("Docs:EnableHotReload", "false");
        }
    }

    [Fact]
    public async Task SymlinkedContent_IsNeverServed()
    {
        var secretFile = Path.Combine(_outside, "secret.txt");
        File.WriteAllText(secretFile, Secret);
        Directory.CreateDirectory(Path.Combine(_docs, "assets"));
        File.WriteAllText(Path.Combine(_docs, "index.md"), "# Home\n");
        File.CreateSymbolicLink(Path.Combine(_docs, "leak.md"), secretFile);
        File.CreateSymbolicLink(Path.Combine(_docs, "assets", "leak.txt"), secretFile);
        Directory.CreateSymbolicLink(Path.Combine(_docs, "assets", "linkdir"), _outside);
        Directory.CreateSymbolicLink(Path.Combine(_docs, "linkdir"), _outside);
        File.WriteAllText(Path.Combine(_outside, "page.md"), Secret);

        using var factory = new Factory(_docs);
        var client = factory.CreateClient();

        foreach (var url in new[] { "/leak/", "/raw/leak.md", "/raw/leak.md?view=true", "/assets/leak.txt", "/assets/linkdir/secret.txt", "/linkdir/page/" })
        {
            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.DoesNotContain(Secret, await response.Content.ReadAsStringAsync());
        }

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/")).StatusCode);
    }

    [Theory]
    [InlineData("/\\\\evil.example")]
    [InlineData("\\\\evil.example")]
    [InlineData("/\\t/evil.example")]
    public async Task RelativeRedirect_ThatCouldLeaveOrigin_IsIgnored(string target)
    {
        File.WriteAllText(Path.Combine(_docs, "index.md"), "# Home\n");
        File.WriteAllText(Path.Combine(_docs, "go.md"), $"---\nredirect: \"{target}\"\n---\n# Go\n");
        File.WriteAllText(Path.Combine(_docs, "ok.md"), "---\nredirect: \"/guide/\"\n---\n# Ok\n");

        using var factory = new Factory(_docs);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/go/")).StatusCode);
        var ok = await client.GetAsync("/ok/");
        Assert.Equal(HttpStatusCode.Redirect, ok.StatusCode);
        Assert.Equal("/guide/", ok.Headers.Location?.OriginalString);
    }

    [Theory]
    [InlineData("127.0.0.1", false)]
    [InlineData("10.1.2.3", false)]
    [InlineData("172.16.0.1", false)]
    [InlineData("192.168.1.1", false)]
    [InlineData("169.254.169.254", false)]
    [InlineData("100.64.0.1", false)]
    [InlineData("0.0.0.0", false)]
    [InlineData("::1", false)]
    [InlineData("fe80::1", false)]
    [InlineData("fd00::1", false)]
    [InlineData("::ffff:127.0.0.1", false)]
    [InlineData("140.82.112.6", true)]
    [InlineData("2606:4700::1111", true)]
    public void RepoStats_OnlyConnectsToPublicAddresses(string ip, bool expected) =>
        Assert.Equal(expected, RepoStatsProvider.IsPublicAddress(IPAddress.Parse(ip)));

    [Fact]
    public async Task RepoStats_LoopbackRepo_ReturnsNothing() =>
        Assert.Null(await new RepoStatsProvider().GetStatsAsync("http://127.0.0.1/o/r", CancellationToken.None));

    public void Dispose()
    {
        try { Directory.Delete(_docs, true); } catch (IOException) { }
        try { Directory.Delete(_outside, true); } catch (IOException) { }
    }
}
