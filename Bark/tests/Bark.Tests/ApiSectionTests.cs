using Microsoft.Extensions.Logging.Abstractions;
using Bark.Configuration;
using Bark.Services;

namespace Bark.Tests;

public sealed class ApiSectionTests : IDisposable
{
    private const string Spec = """
        openapi: 3.1.0
        info: { title: T, version: '1' }
        servers: [ { url: https://api.example.com } ]
        tags: [ { name: Tasks } ]
        paths:
          /tasks:
            get: { tags: [Tasks], operationId: listTasks, summary: List tasks, responses: { '200': { description: OK } } }
            post: { tags: [Tasks], summary: Create task, responses: { '201': { description: Created } } }
          /health:
            get: { summary: Health, responses: { '200': { description: OK } } }
        components:
          schemas:
            Task: { type: object, properties: { id: { type: string } } }
        """;

    private readonly string _docs = Directory.CreateTempSubdirectory("bark-apisection-").FullName;

    public void Dispose() => Directory.Delete(_docs, true);

    private async Task<DocumentationService> Build(string? config = null)
    {
        Directory.CreateDirectory(Path.Combine(_docs, "api"));
        await File.WriteAllTextAsync(Path.Combine(_docs, "index.md"), "# Home\n");
        await File.WriteAllTextAsync(Path.Combine(_docs, "api", "openapi.yaml"), Spec);
        await File.WriteAllTextAsync(Path.Combine(_docs, "api", "index.md"), "---\ntitle: Tasks API\napiSpec: openapi.yaml\n---\n");
        await File.WriteAllTextAsync(Path.Combine(_docs, "api", "create-task.md"), "---\napi: openapi.yaml POST /tasks\n---\n\nHand-written note.\n");
        if (config is not null)
            await File.WriteAllTextAsync(Path.Combine(_docs, "config.json"), config);

        var service = new DocumentationService(new DocsOptions { RootPath = _docs, EnableHotReload = false }, new MarkdownService(), NullLogger<DocumentationService>.Instance);
        await service.StartAsync(CancellationToken.None);
        return service;
    }

    [Fact]
    public async Task ApiSpec_GeneratesPagesAndTagGroupedSidebar_KeepsHandWrittenPage()
    {
        using var service = await Build();

        var list = await service.GetPageAsync("api/list-tasks");
        Assert.Equal("List tasks", list!.Title);
        Assert.Equal("GET", service.ApiMethodOf("api/list-tasks"));
        Assert.Equal("api/openapi.yaml", list.OriginalRelativePath);
        Assert.NotNull(await service.GetPageAsync("api/health"));
        Assert.NotNull(await service.GetPageAsync("api/task"));
        Assert.Contains("Hand-written note.", (await service.GetPageAsync("api/create-task"))!.HtmlContent);

        var sidebar = service.SiteConfig!.Sidebar!["/api/"];
        Assert.Equal("api", sidebar[0].Items![0].Path);
        Assert.Equal(["Tasks", "Endpoints", "Objects"], sidebar.Skip(1).Select(g => g.Title).ToArray());
        Assert.Equal(new[] { "api/list-tasks", "api/create-task" }, sidebar[1].Items!.Select(i => i.Path ?? "").ToArray());
    }

    [Fact]
    public async Task ApiSpec_ConfigSidebarForTheFolderWins()
    {
        using var service = await Build("""{ "sidebar": { "/api/": [ { "title": "Mine", "items": [ { "title": "Health", "path": "api/health" } ] } ] } }""");

        Assert.Equal("Mine", service.SiteConfig!.Sidebar!["/api/"][0].Title);
        Assert.NotNull(await service.GetPageAsync("api/list-tasks"));
    }
}
