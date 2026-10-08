using System.Collections.Immutable;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using Bark.Configuration;
using Bark.Models;
using Bark.Services.Api;
using Bark.Services.Extensions;
using Bark.Services.Rendering;

namespace Bark.Services;

public sealed partial class DocumentationService : IHostedService, IDisposable, IExtensionSource
{
    private readonly DocsOptions _options;
    private readonly MarkdownService _markdown;
    private readonly ILogger<DocumentationService> _logger;
    private FileSystemWatcher? _watcher;
    private FileSystemWatcher? _configWatcher;
    private FileSystemWatcher? _extensionsWatcher;
    private FileSystemWatcher? _localeWatcher;
    private readonly CancellationTokenSource _shutdownCts = new();

    // All read state lives in one immutable snapshot swapped atomically after a full build; readers never see half-built state
    private sealed record ContentSnapshot(
        IReadOnlyDictionary<string, DocumentationPage> Pages,
        IReadOnlyDictionary<string, string> NavTitles,
        Config? Config,
        ExtensionSet Extensions,
        IReadOnlyDictionary<string, NavigationNode> NavigationByLocale,
        IReadOnlyDictionary<string, SearchIndex> SearchByLocale,
        IReadOnlyList<string> LocaleCodes,
        string RootLocale,
        LocaleTables Locales);

    private static readonly ContentSnapshot EmptySnapshot = new(
        ImmutableDictionary<string, DocumentationPage>.Empty,
        ImmutableDictionary<string, string>.Empty,
        null,
        ExtensionSet.Empty,
        ImmutableDictionary<string, NavigationNode>.Empty,
        ImmutableDictionary<string, SearchIndex>.Empty,
        [],
        "en",
        LocaleTables.Empty);

    private volatile ContentSnapshot _snapshot = EmptySnapshot;
    private string? _lastContentHash;
    private readonly SemaphoreSlim _buildLock = new(1, 1);
    private readonly Channel<FileSystemEventArgs> _fileChannel =
        Channel.CreateBounded<FileSystemEventArgs>(new BoundedChannelOptions(256)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });
    private bool _disposed;

    public DocumentationService(
        DocsOptions options,
        MarkdownService markdown,
        ILogger<DocumentationService> logger)
    {
        _options = options;
        _markdown = markdown;
        _logger = logger;
    }

    public Config? SiteConfig => _snapshot.Config;
    public ExtensionSet Extensions => _snapshot.Extensions;

    /// <summary>HTTP method of the API page at <paramref name="path"/>, for sidebar badges; null for other pages.</summary>
    public string? ApiMethodOf(string path) =>
        _snapshot.Pages.TryGetValue(path, out var page) ? page.ApiMethod : null;
    public long BuildVersion { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await RebuildAsync(cancellationToken);

        if (_options.EnableHotReload)
        {
            var docsPath = Path.GetFullPath(_options.RootPath);
            if (!Directory.Exists(docsPath))
                Directory.CreateDirectory(docsPath);

            _watcher = new FileSystemWatcher(docsPath)
            {
                IncludeSubdirectories = true,
                Filter = "*.md",
            };
            foreach (var extension in ApiSpecLoader.OpenApiExtensions.Concat(ApiSpecLoader.GraphQlExtensions).Concat(ApiSpecLoader.ODataExtensions))
                _watcher.Filters.Add("*" + extension);
            _watcher.EnableRaisingEvents = true;
            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileChanged;
            _watcher.Deleted += OnFileChanged;
            _watcher.Renamed += OnFileRenamed;

            _configWatcher = new FileSystemWatcher(docsPath)
            {
                Filter = "config.json",
                EnableRaisingEvents = true
            };
            _configWatcher.Changed += OnFileChanged;
            _configWatcher.Created += OnFileChanged;
            _configWatcher.Deleted += OnFileChanged;
            _configWatcher.Renamed += OnFileRenamed;

            _extensionsWatcher = new FileSystemWatcher(docsPath)
            {
                Filter = ExtensionLoader.FileName,
                EnableRaisingEvents = true
            };
            _extensionsWatcher.Changed += OnFileChanged;
            _extensionsWatcher.Created += OnFileChanged;
            _extensionsWatcher.Deleted += OnFileChanged;
            _extensionsWatcher.Renamed += OnFileRenamed;

            // The main watcher filters *.md, so locale JSON needs its own watcher.
            var localeDir = Path.Combine(docsPath, "locale");
            if (Directory.Exists(localeDir))
            {
                _localeWatcher = new FileSystemWatcher(localeDir)
                {
                    Filter = "*.json",
                    EnableRaisingEvents = true
                };
                _localeWatcher.Changed += OnFileChanged;
                _localeWatcher.Created += OnFileChanged;
                _localeWatcher.Deleted += OnFileChanged;
                _localeWatcher.Renamed += OnFileRenamed;
            }

            _ = FileWatcherConsumerAsync(_shutdownCts.Token);

            _logger.LogInformation("Hot reload enabled, watching {DocsPath}", docsPath);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // DI can dispose this singleton before the host stops it; cancelling a disposed CTS would fail shutdown.
        if (_disposed)
            return Task.CompletedTask;

        _shutdownCts.Cancel();
        _watcher?.Dispose();
        _configWatcher?.Dispose();
        _extensionsWatcher?.Dispose();
        _localeWatcher?.Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _shutdownCts.Cancel();
        _shutdownCts.Dispose();
        _watcher?.Dispose();
        _configWatcher?.Dispose();
        _extensionsWatcher?.Dispose();
        _localeWatcher?.Dispose();
        _buildLock.Dispose();
        _disposed = true;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        _fileChannel.Writer.TryWrite(e);
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        _fileChannel.Writer.TryWrite(e);
    }

    private async Task FileWatcherConsumerAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var __ in _fileChannel.Reader.ReadAllAsync(ct))
            {
                await Task.Delay(300, ct);

                while (_fileChannel.Reader.TryRead(out _)) { }

                try
                {
                    await RebuildAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to rebuild documentation");
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File watcher consumer failed");
        }
    }

    private async Task RebuildAsync(CancellationToken cancellationToken)
    {
        await _buildLock.WaitAsync(cancellationToken);
        try
        {
            await BuildAsync(cancellationToken);
        }
        finally
        {
            _buildLock.Release();
        }
    }

    private sealed record ApiPageResult(string Html, string? Title, IReadOnlyList<string>? ConnectSources, bool Failed, string? Method = null);

    private ApiPageResult RenderApiPage(ApiSpecLoader specs, string api, MarkdownParseResult parsed, string relativePath, Localization localization)
    {
        var frontMatter = parsed.FrontMatter!;
        try
        {
            var model = specs.Load(api, relativePath);
            var html = ApiPageRenderer.Render(model, parsed.Html,
                new ApiPageRenderer.Options(localization, _markdown.ToHtml, frontMatter.Title, frontMatter.Server, frontMatter.Auth));
            var method = model.Operation is { } op ? (op.Webhook ? "webhook" : op.Method) : null;
            return new ApiPageResult(html, frontMatter.Title ?? ApiPageRenderer.DefaultTitle(model), ApiPageRenderer.Origins(model, frontMatter.Server), false, method);
        }
        catch (ApiSpecException ex)
        {
            _logger.LogWarning("API page {Path}: {Message}", relativePath, ex.Message);
            var error = $"<div class=\"danger custom-block\"><p class=\"custom-block-title\">API</p><p>{WebUtility.HtmlEncode(ex.Message)}</p></div>";
            return new ApiPageResult(error + parsed.Html, null, null, true);
        }
    }

    [GeneratedRegex("(?<=[a-z0-9])(?=[A-Z])")]
    private static partial Regex CamelBoundary();

    private sealed record ApiSection(string RelativePath, string PagePath, string Spec, string LocalePrefix, string Title, DateTime LastModified, Localization Localization, string? Server, string? Auth);

    // One page per catalog entry unless a Markdown file already has that path, plus the folder sidebar unless config.json defines one.
    private void AddApiSection(ApiSection section, ApiSpecLoader specs, Dictionary<string, DocumentationPage> pageMap, List<DocumentationPage> pages, Config config)
    {
        (string SpecKey, IReadOnlyList<ApiCatalogEntry> Entries) catalog;
        try
        {
            catalog = specs.Catalog(section.Spec, section.RelativePath);
        }
        catch (ApiSpecException ex)
        {
            _logger.LogWarning("API section {Path}: {Message}", section.RelativePath, ex.Message);
            return;
        }

        var l = section.Localization;
        var dir = Path.GetDirectoryName(section.RelativePath)?.Replace('\\', '/') ?? "";
        var slugs = new HashSet<string>(StringComparer.Ordinal);
        var groups = new List<NavEntry>();
        foreach (var entry in catalog.Entries)
        {
            var baseSlug = MarkdownService.Slugify(CamelBoundary().Replace(entry.Slug, "-")) is { Length: > 0 } s ? s : "page";
            var slug = baseSlug;
            for (var n = 2; !slugs.Add(slug); n++)
                slug = $"{baseSlug}-{n}";

            var relativePath = dir.Length == 0 ? $"{slug}.md" : $"{dir}/{slug}.md";
            var pagePath = PagePath.FromFile(relativePath);
            if (!pageMap.TryGetValue(pagePath, out var page))
            {
                var api = $"{section.Spec} {entry.Selector}";
                // The index page's server and auth apply to every generated page; GraphQL and OData specs carry no server.
                var frontMatter = $"api: {api}\n"
                    + (section.Server is { Length: > 0 } server ? $"server: {server}\n" : "")
                    + (section.Auth is { Length: > 0 } auth ? $"auth: {auth}\n" : "");
                var parsed = _markdown.Parse($"---\n{frontMatter}---\n", entry.Title, filePath: relativePath, localePrefix: section.LocalePrefix);
                var rendered = RenderApiPage(specs, api, parsed, relativePath, l);
                page = new DocumentationPage(
                    Path: pagePath,
                    Title: rendered.Title ?? entry.Title,
                    HtmlContent: VersionAssets(WrapTables(rendered.Html)),
                    LastModified: section.LastModified,
                    Headings: parsed.Headings,
                    Layout: rendered.Failed ? null : "api",
                    OriginalRelativePath: catalog.SpecKey,
                    ShowToc: false,
                    ConnectSources: rendered.ConnectSources,
                    ApiMethod: rendered.Method);
                pageMap[pagePath] = page;
                pages.Add(page);
            }

            var groupTitle = entry.Group.StartsWith('@')
                ? l.Text("apiGroup" + char.ToUpperInvariant(entry.Group[1]) + entry.Group[2..])
                : entry.Group;
            var group = groups.FirstOrDefault(g => g.Title == groupTitle);
            if (group is null)
                groups.Add(group = new NavEntry { Title = groupTitle, Items = [] });
            group.Items!.Add(new NavEntry { Title = page.Title, Path = LocaleRouting.Delocalize(section.LocalePrefix, pagePath) });
        }

        var prefix = LocaleRouting.Delocalize(section.LocalePrefix, dir).Trim('/');
        config.Sidebar ??= [];
        if (prefix.Length == 0 || config.Sidebar.Keys.Any(k => k.Trim('/').Equals(prefix, StringComparison.OrdinalIgnoreCase)))
            return;
        var overview = new NavEntry { Items = [new NavEntry { Title = section.Title, Path = LocaleRouting.Delocalize(section.LocalePrefix, section.PagePath) }] };
        config.Sidebar[$"/{prefix}/"] = [overview, .. groups];
    }

    // Caller must hold _buildLock; builds a complete snapshot off to the side, then swaps it in
    private async Task BuildAsync(CancellationToken cancellationToken)
    {
        IconProvider.ClearCache();
        var docsPath = Path.GetFullPath(_options.RootPath);
        if (!Directory.Exists(docsPath))
        {
            _logger.LogWarning("Docs directory does not exist: {Path}", docsPath);
            return;
        }

        // Loaded up front so title fallback can consult config.json before the filename.
        var config = LoadConfig(docsPath);
        var locales = Localization.BuildAll(docsPath, config, _logger);
        var navTitlesByPath = BuildNavTitleLookup(config);
        var configuredLocales = LocaleRouting.TreeCodes(config);
        var configuredRootLocale = LocaleRouting.RootCode(config);

        // Sorted for deterministic hashing, regardless of FS enumeration order.
        var allFiles = Directory.GetFiles(docsPath, "*.md", ContentLinks.NoLinks).Order().ToArray();
        var pages = new List<DocumentationPage>();
        var pageMap = new Dictionary<string, DocumentationPage>();
        var hashInput = new StringBuilder();
        var apiSpecs = new ApiSpecLoader(docsPath);
        var apiSections = new List<ApiSection>();

        foreach (var file in allFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = Path.GetRelativePath(docsPath, file);
            var pagePath = PagePath.FromFile(relativePath);

            var content = await File.ReadAllTextAsync(file, cancellationToken);
            hashInput.Append(relativePath).Append('\0').Append(content).Append('\0');

            var defaultTitle = Path.GetFileNameWithoutExtension(relativePath);
            if (defaultTitle.Equals("index", StringComparison.OrdinalIgnoreCase))
            {
                var dir = Path.GetDirectoryName(relativePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    var dirName = Path.GetFileName(dir)!;
                    var spaced = dirName.Replace('-', ' ').Replace('_', ' ');
                    defaultTitle = spaced.Length > 0 ? char.ToUpperInvariant(spaced[0]) + spaced[1..] : dirName;
                }
                else
                {
                    defaultTitle = locales.Root.BreadcrumbHome;
                }
            }

            if (navTitlesByPath.TryGetValue(pagePath, out var navTitle))
                defaultTitle = navTitle;

            var normalizedRelativePath = relativePath.Replace('\\', '/');
            var pageLocale = LocaleRouting.LocaleOf(pagePath, configuredLocales, configuredRootLocale);
            var localePrefix = pageLocale == configuredRootLocale ? string.Empty : pageLocale;
            var parsed = _markdown.Parse(content, defaultTitle, filePath: normalizedRelativePath, localePrefix: localePrefix);
            var api = parsed.FrontMatter?.Api is { Length: > 0 } apiValue
                ? RenderApiPage(apiSpecs, apiValue, parsed, normalizedRelativePath, locales.For(pageLocale))
                : null;

            var html = WrapTables(api?.Html ?? parsed.Html);
            html = VersionAssets(html);
            var lastModified = parsed.FrontmatterDate ?? File.GetLastWriteTimeUtc(file);

            var page = new DocumentationPage(
                Path: pagePath,
                Title: api?.Title ?? parsed.Title ?? defaultTitle,
                HtmlContent: html,
                Description: parsed.Description,
                LastModified: lastModified,
                Headings: parsed.Headings,
                Layout: api is { Failed: false } ? "api" : parsed.Layout,
                ShowLastUpdated: parsed.ShowLastUpdated,
                OriginalRelativePath: normalizedRelativePath,
                Keywords: parsed.Keywords,
                ShowPagination: parsed.ShowPagination,
                Redirect: parsed.Redirect,
                ShowToc: api is { Failed: false } ? false : parsed.ShowToc,
                Image: parsed.Image,
                MachineTranslated: parsed.MachineTranslated,
                ConnectSources: api?.ConnectSources,
                ApiMethod: api?.Method,
                ShowSidebar: parsed.FrontMatter?.Sidebar ?? true,
                ContentWidth: parsed.FrontMatter?.WidthPercent
            );

            pageMap[pagePath] = page;
            pages.Add(page);

            if (parsed.FrontMatter?.ApiSpec is { Length: > 0 } apiSpec)
                apiSections.Add(new ApiSection(normalizedRelativePath, pagePath, apiSpec, localePrefix, page.Title, lastModified, locales.For(pageLocale), parsed.FrontMatter.Server, parsed.FrontMatter.Auth));
        }

        if (apiSections.Count > 0)
        {
            config ??= new Config();
            foreach (var section in apiSections)
                AddApiSection(section, apiSpecs, pageMap, pages, config);
        }

        foreach (var (specPath, specText) in apiSpecs.Contents.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            hashInput.Append(specPath).Append('\0').Append(specText).Append('\0');

        var configPath = Path.Combine(docsPath, "config.json");
        if (File.Exists(configPath))
            hashInput.Append(await File.ReadAllTextAsync(configPath, cancellationToken));

        var extensions = ExtensionLoader.Load(docsPath, _logger);
        var extensionsPath = Path.Combine(docsPath, ExtensionLoader.FileName);
        if (File.Exists(extensionsPath))
            hashInput.Append(await File.ReadAllTextAsync(extensionsPath, cancellationToken));

        // Fold locale files into the hash so an edit bumps BuildVersion and drives live reload.
        var localeDir = Path.Combine(docsPath, "locale");
        if (Directory.Exists(localeDir))
            foreach (var f in Directory.GetFiles(localeDir, "*.json").Order())
                hashInput.Append(Path.GetFileName(f)).Append('\0')
                         .Append(await File.ReadAllTextAsync(f, cancellationToken)).Append('\0');

        var contentHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(hashInput.ToString())));

        var localeCodes = configuredLocales;
        var rootLocale = configuredRootLocale;

        var rootPages = pages.Where(p => LocaleRouting.LocaleOf(p.Path, localeCodes, rootLocale) == rootLocale).ToList();
        var searchIndex = new SearchIndex();
        searchIndex.Build(rootPages);

        var navigationByLocale = new Dictionary<string, NavigationNode>(StringComparer.OrdinalIgnoreCase);
        var searchByLocale = new Dictionary<string, SearchIndex>(StringComparer.OrdinalIgnoreCase)
        {
            [rootLocale] = searchIndex
        };

        foreach (var code in localeCodes)
        {
            var treeDir = Path.Combine(docsPath, code);
            navigationByLocale[code] = Directory.Exists(treeDir)
                ? BuildNodeFromDirectory(docsPath, treeDir, pages.ToDictionary(p => p.Path), localeCodes, locales.For(code))
                : new NavigationNode(locales.For(code).BreadcrumbHome, null, Array.Empty<NavigationNode>());

            var treeIndex = new SearchIndex();
            treeIndex.Build(pages.Where(p => LocaleRouting.LocaleOf(p.Path, localeCodes, rootLocale) == code).ToList());
            searchByLocale[code] = treeIndex;
        }

        navigationByLocale[rootLocale] = BuildNavigation(docsPath, pages, localeCodes, locales.Root);

        var snapshot = new ContentSnapshot(
            pageMap,
            navTitlesByPath,
            config,
            extensions,
            navigationByLocale,
            searchByLocale,
            localeCodes,
            rootLocale,
            locales);

        _snapshot = snapshot;

        // Prevent unnecessary client reloads from spurious file events by verifying content changes!
        if (contentHash == _lastContentHash)
        {
            _logger.LogDebug("Rebuilt documentation but content is unchanged, skipping version bump");
            return;
        }

        _lastContentHash = contentHash;
        BuildVersion++;
        _logger.LogInformation("Built documentation with {PageCount} pages", pages.Count);

        LogDeadLinks(pages, pageMap);
    }

    private void LogDeadLinks(List<DocumentationPage> pages, Dictionary<string, DocumentationPage> pageMap)
    {
        var deadSources = new HashSet<string>();
        foreach (var page in pages)
        {
            foreach (Match match in HrefRegex().Matches(page.HtmlContent))
            {
                var href = match.Groups[1].Value;
                if (ShouldSkipHref(href))
                    continue;

                var resolved = ResolveHref(page.Path, href);
                if (resolved.Length == 0 || PathResolves(resolved))
                    continue;

                deadSources.Add(page.Path);
            }
        }

        if (deadSources.Count > 0)
        {
            var list = string.Join(", ", deadSources.Order());
            _logger.LogWarning("Dead internal links found in: {Sources}", list);
        }
    }

    private static string ResolveHref(string pagePath, string href)
    {
        var fragIdx = href.IndexOf('#');
        var pathOnly = fragIdx >= 0 ? href[..fragIdx] : href;

        if (pathOnly.StartsWith('/'))
            return pathOnly.Trim('/').ToLowerInvariant();

        var basePath = pagePath == "index" ? "" : pagePath;
        var combined = $"{basePath}/{pathOnly}";
        var segments = new List<string>();
        foreach (var seg in combined.Split('/'))
        {
            if (seg == "..")
            {
                if (segments.Count > 0)
                    segments.RemoveAt(segments.Count - 1);
            }
            else if (seg != "." && seg != "")
                segments.Add(seg);
        }
        return string.Join("/", segments).ToLowerInvariant();
    }

    private static bool ShouldSkipHref(string href)
    {
        return href.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || href.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || href.StartsWith("//")
            || href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
            || href.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)
            || href.StartsWith("#")
            || href == "/";
    }

    [GeneratedRegex(@"<a\s[^>]*href=""([^""]+)""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HrefRegex();

    private NavigationNode BuildNavigation(string docsPath, IEnumerable<DocumentationPage> pages, IReadOnlyList<string> localeCodes, Localization localization)
    {
        var pageMap = pages.ToDictionary(p => p.Path);
        return BuildNodeFromDirectory(docsPath, docsPath, pageMap, localeCodes, localization, skipLocaleDirectories: true);
    }

    private NavigationNode BuildNodeFromDirectory(
        string basePath,
        string currentDir,
        Dictionary<string, DocumentationPage> pageMap,
        IReadOnlyList<string> localeCodes,
        Localization localization,
        bool skipLocaleDirectories = false)
    {
        var relativePath = Path.GetRelativePath(basePath, currentDir).Replace('\\', '/');
        var title = Path.GetFileName(currentDir);
        if (relativePath == ".")
            title = localization.BreadcrumbHome;

        var children = new List<NavigationNode>();

        foreach (var subDir in Directory.GetDirectories(currentDir))
        {
            if (skipLocaleDirectories && localeCodes.Contains(Path.GetFileName(subDir), StringComparer.OrdinalIgnoreCase))
                continue;

            var node = BuildNodeFromDirectory(basePath, subDir, pageMap, localeCodes, localization);
            if (node.Children.Count > 0 || pageMap.Values.Any(p =>
                p.Path.StartsWith(Path.GetRelativePath(basePath, subDir).Replace('\\', '/').ToLowerInvariant())))
            {
                children.Add(node);
            }
        }

        foreach (var file in Directory.GetFiles(currentDir, "*.md"))
        {
            var pagePath = PagePath.FromFile(Path.GetRelativePath(basePath, file));

            if (pageMap.TryGetValue(pagePath, out var page))
            {
                children.Add(new NavigationNode(page.Title, page.Path, Array.Empty<NavigationNode>()));
            }
        }

        children = children.OrderBy(c => c.Path == null ? 0 : 1)
                           .ThenBy(c => c.Title)
                           .ToList();

        return new NavigationNode(title, null, children);
    }

    public Task<NavigationNode> GetNavigationAsync(string? localeCode = null, CancellationToken cancellationToken = default)
    {
        var snapshot = _snapshot;
        var code = localeCode is { Length: > 0 } ? localeCode : snapshot.RootLocale;
        return Task.FromResult(snapshot.NavigationByLocale.TryGetValue(code, out var navigation)
            ? navigation
            : new NavigationNode(snapshot.Locales.Root.BreadcrumbHome, null, Array.Empty<NavigationNode>()));
    }

    public LocaleTables Locales => _snapshot.Locales;

    public IReadOnlyList<string> LocaleCodes => _snapshot.LocaleCodes;

    public string RootLocale => _snapshot.RootLocale;

    public ValueTask<DocumentationPage?> GetPageAsync(string path, CancellationToken cancellationToken = default)
    {
        path = path.Trim('/').ToLowerInvariant();
        if (string.IsNullOrEmpty(path))
            path = _options.DefaultPage ?? "index";

        _snapshot.Pages.TryGetValue(path, out var page);
        return ValueTask.FromResult(page);
    }

    public bool PathResolves(string path)
    {
        var snapshot = _snapshot;
        var normalized = path.Trim('/').ToLowerInvariant();
        if (snapshot.Pages.ContainsKey(normalized))
            return true;

        var code = LocaleRouting.LocaleOf(normalized, snapshot.LocaleCodes, snapshot.RootLocale);
        return code != snapshot.RootLocale
            && snapshot.Pages.ContainsKey(LocaleRouting.Delocalize(code, normalized));
    }

    public Task<IReadOnlyList<DocumentationPage>> GetAllPagesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DocumentationPage> pages = _snapshot.Pages.Values.ToImmutableList();
        return Task.FromResult(pages);
    }

    public IReadOnlyList<SearchResult> Search(string query, string? localeCode = null) =>
        IndexFor(localeCode).Search(query);

    public SearchIndexExport GetSearchIndexExport(string? localeCode = null) =>
        IndexFor(localeCode).ExportSnapshot();

    private SearchIndex IndexFor(string? localeCode)
    {
        var snapshot = _snapshot;
        var code = localeCode is { Length: > 0 } ? localeCode : snapshot.RootLocale;
        return snapshot.SearchByLocale.TryGetValue(code, out var index) ? index : new SearchIndex();
    }

    public Task<IReadOnlyList<BreadcrumbItem>> GetBreadcrumbsAsync(string path, Localization? localization = null, CancellationToken cancellationToken = default)
    {
        path = path.Trim('/').ToLowerInvariant();
        var snapshotForHome = _snapshot;
        var localeCode = LocaleRouting.LocaleOf(path, snapshotForHome.LocaleCodes, snapshotForHome.RootLocale);
        var homeHref = localeCode == snapshotForHome.RootLocale ? "/" : $"/{localeCode}/";
        var allSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var localePrefix = localeCode == snapshotForHome.RootLocale ? string.Empty : localeCode;
        var segments = allSegments
            .Skip(localePrefix.Length == 0 ? 0 : 1)
            .Where(s => !s.Equals("index", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var table = localization ?? snapshotForHome.Locales.Root;
        var crumbs = new List<BreadcrumbItem> { new(table.BreadcrumbHome, homeHref) };

        var snapshot = _snapshot;
        var accumulated = localePrefix;
        foreach (var segment in segments)
        {
            accumulated = string.IsNullOrEmpty(accumulated) ? segment : $"{accumulated}/{segment}";

            if (snapshot.Pages.TryGetValue(accumulated, out var page))
                crumbs.Add(new BreadcrumbItem(page.Title, $"/{accumulated}"));
            else if (snapshot.NavTitles.TryGetValue(accumulated, out var navTitle))
                crumbs.Add(new BreadcrumbItem(table.Label(navTitle), null));
            else
            {
                var title = segment.Replace('-', ' ').Replace('_', ' ');
                if (title.Length > 0)
                    title = char.ToUpperInvariant(title[0]) + title[1..];
                crumbs.Add(new BreadcrumbItem(table.Label(title), null));
            }
        }

        return Task.FromResult<IReadOnlyList<BreadcrumbItem>>(crumbs);
    }

    private static Dictionary<string, string> BuildNavTitleLookup(Config? config)
    {
        var lookup = new Dictionary<string, string>();

        if (config?.Nav is { } nav)
            CollectNavTitles(nav, lookup);

        if (config?.Sidebar is { } sidebar)
            foreach (var entries in sidebar.Values)
                CollectNavTitles(entries, lookup);

        if (config?.TopNav is { } topNav)
            foreach (var item in topNav)
                CollectTopNavTitles(item, lookup);

        return lookup;
    }

    private static void CollectTopNavTitles(TopNavItem item, Dictionary<string, string> lookup)
    {
        if (!string.IsNullOrEmpty(item.Link) && !string.IsNullOrEmpty(item.Text))
            lookup[item.Link.Trim('/').ToLowerInvariant()] = item.Text;

        if (item.Items is { Count: > 0 } children)
            foreach (var child in children)
                CollectTopNavTitles(child, lookup);
    }

    private static void CollectNavTitles(List<NavEntry> entries, Dictionary<string, string> lookup)
    {
        foreach (var entry in entries)
        {
            // External targets have no page path, so they must not shadow a real page's breadcrumb title.
            if (!string.IsNullOrEmpty(entry.Path) && !UrlPaths.IsExternal(entry.Path))
                lookup[entry.Path.Trim('/').ToLowerInvariant()] = entry.Title;

            if (entry.Items is { Count: > 0 } children)
                CollectNavTitles(children, lookup);
        }
    }

    [GeneratedRegex(@"<table[^>]*>[\s\S]*?</table>", RegexOptions.IgnoreCase)]
    private static partial Regex TableRegex();

    private static string WrapTables(string html) =>
        TableRegex().Replace(html, m => $"<div class=\"table-wrapper\">{m.Value}</div>");

    private static string VersionAssets(string html) =>
        AssetSrcHrefRegex().Replace(html, m =>
        {
            var url = m.Groups[2].Value;
            var versioned = AssetVersioning.Current.Apply(url);
            return versioned == url ? m.Value : $"{m.Groups[1].Value}=\"{versioned}\"";
        });

    [GeneratedRegex(@"(src|href)=""([^""]+)""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AssetSrcHrefRegex();

    private static Config? LoadConfig(string docsPath)
    {
        var configPath = Path.Combine(docsPath, "config.json");
        if (!File.Exists(configPath))
            return null;

        try
        {
            var json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<Config>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }
}
