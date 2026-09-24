using System.IO.Compression;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Serilog;
using System.Threading.RateLimiting;
using Bark.Configuration;
using Bark.Endpoints;
using Bark.Models;
using Bark.Serialization;
using Bark.Services;
using Bark.Services.Extensions;
using Bark.Services.Rendering;
using Bark.Services.Translation;
using Bark.Services.MarkdownExtensions;

Directory.CreateDirectory("log");

var cliArgs = CliArguments.Parse(args);
var exportDir = cliArgs.ExportDir;
var exportBaseUrl = cliArgs.ExportBaseUrl;

try
{
    var builder = WebApplication.CreateBuilder(args);

    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .CreateLogger();

    builder.Host.UseSerilog();

    var docsOptions = builder.Configuration.GetSection("Docs").Get<DocsOptions>() ?? new DocsOptions();
    docsOptions = docsOptions with { RootPath = Environment.GetEnvironmentVariable("DOCS_ROOT_PATH") ?? docsOptions.RootPath };
    if (exportDir != null)
        // No server survives the export, so the hot-reload poll and /api/search have nothing to talk to.
        docsOptions = docsOptions with { EnableHotReload = false, IsStaticExport = true };

    builder.Services.AddSingleton(docsOptions);

    var basePath = NormalizeBasePath(cliArgs.BasePath ?? docsOptions.BasePath);
    var docsRootAbsolute = Path.GetFullPath(docsOptions.RootPath).Replace(Path.DirectorySeparatorChar, '/');

    // WebRootPath is null when wwwroot/ is missing (e.g. under test hosts); fall back to the conventional path
    var webRootPath = builder.Environment.WebRootPath
        ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot");

    var gitSyncOptions = new GitSyncOptions
    {
        Enabled = Environment.GetEnvironmentVariable("GIT_ENABLED") is "true" or "1",
        Url = Environment.GetEnvironmentVariable("GIT_URL"),
        Username = Environment.GetEnvironmentVariable("GIT_USERNAME"),
        Password = Environment.GetEnvironmentVariable("GIT_PASSWORD"),
        Root = Environment.GetEnvironmentVariable("GIT_ROOT"),
        Cron = Environment.GetEnvironmentVariable("GIT_CRON") ?? "*/5 * * * *",
    };
    var gitRoot = string.IsNullOrWhiteSpace(gitSyncOptions.Root)
        ? docsRootAbsolute
        : Path.GetFullPath(gitSyncOptions.Root).Replace(Path.DirectorySeparatorChar, '/');

    // theme/ inside the git-synced repo wins over wwwroot/theme when Git:Root is set, even before the first
    // clone lands (the clone is async and may still be running when this runs)
    var usingGitTheme = !string.IsNullOrWhiteSpace(gitSyncOptions.Root);
    var themeDir = usingGitTheme ? Path.Combine(gitRoot, "theme") : Path.Combine(webRootPath, "theme");
    try { Directory.CreateDirectory(themeDir); } catch (IOException) { }

    // appsettings.json's Docs:Themes wins if present; theme.json is the file-only alternative.
    var themeOptions = builder.Configuration.GetSection("Docs:Themes").Get<ThemeOptions>()
        ?? ThemeJsonLoader.Load(themeDir)
        ?? new ThemeOptions();
    builder.Services.AddSingleton(themeOptions);

    var codeGroupIconOptions = builder.Configuration.GetSection("Docs:CodeGroupIcons").Get<CodeGroupIconOptions>()
        ?? new CodeGroupIconOptions();
    builder.Services.AddSingleton(codeGroupIconOptions);

    builder.Services.AddSingleton<ISyntaxHighlighter, TextMateSyntaxHighlighter>();
    builder.Services.AddSingleton<MathRenderer>();
    builder.Services.AddSingleton(sp => new MarkdownService(
        sp.GetRequiredService<ISyntaxHighlighter>(), basePath,
        sp.GetRequiredService<CodeGroupIconOptions>(),
        sp.GetRequiredService<MathRenderer>(),
        sp.GetRequiredService<ILogger<MarkdownService>>()));
    builder.Services.AddSingleton<DocumentationService>();
    builder.Services.AddHostedService(sp => sp.GetRequiredService<DocumentationService>());
    builder.Services.AddSingleton<IExtensionSource>(sp => sp.GetRequiredService<DocumentationService>());

    if (exportDir is null) // no background pulls during a one-shot static export
        builder.Services.AddHostedService(sp => new GitContentSyncService(
            gitSyncOptions, gitRoot, sp.GetRequiredService<ILogger<GitContentSyncService>>()));

    var customCspRaw = builder.Configuration["Docs:ContentSecurityPolicy"];
    var customCsp = string.IsNullOrWhiteSpace(customCspRaw) ? null : customCspRaw;

    builder.Services.AddSingleton(new PageRequestSettings(
        BasePath: basePath,
        CustomCsp: customCsp,
        ThemeDir: themeDir,
        WebRootPath: webRootPath,
        DocsRootAbsolute: docsRootAbsolute,
        PublicBaseUrl: PageRequestSettings.ResolvePublicBaseUrl(
            exportBaseUrl, docsOptions.PublicBaseUrl, builder.Configuration["PublicBaseUrl"]),
        CliTheme: cliArgs.Theme));
    builder.Services.AddSingleton<RepoStatsProvider>();
    builder.Services.AddSingleton<PageRequestHandler>();

    builder.Services.ConfigureHttpJsonOptions(opts =>
        opts.SerializerOptions.TypeInfoResolverChain.Insert(0, BarkJsonContext.Default));

    builder.Services.AddResponseCompression(opts =>
    {
        opts.EnableForHttps = true;
        opts.Providers.Add<BrotliCompressionProvider>();
        opts.Providers.Add<GzipCompressionProvider>();
    });

    builder.Services.Configure<BrotliCompressionProviderOptions>(opts =>
    {
        opts.Level = CompressionLevel.Fastest;
    });

    builder.Services.Configure<GzipCompressionProviderOptions>(opts =>
    {
        opts.Level = CompressionLevel.Fastest;
    });

    builder.WebHost.ConfigureKestrel(KestrelHardening.Configure);

    builder.Logging.ClearProviders();
    builder.Logging.AddConsole(opts => opts.FormatterName = "simple");
    builder.Logging.AddSimpleConsole(opts => opts.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ");

    builder.Services.AddSingleton<Serilog.ILogger>(sp => Log.Logger);

    builder.Services.AddRateLimiter(options =>
    {
        options.AddPolicy(RateLimitPolicies.Search, httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 30,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    LogApplicationBanner();

    var app = builder.Build();

    if (!app.Environment.IsDevelopment() && app.Services.GetRequiredService<PageRequestSettings>().PublicBaseUrl is null)
        Log.Warning("Docs:PublicBaseUrl is not set; canonical URLs, feeds and robots.txt are built from the caller's Host header. Set it in production.");

    // Must finish before DocumentationService's async renders the pages
    await app.Services.GetRequiredService<ISyntaxHighlighter>().InitializeAsync(CancellationToken.None);

    if (basePath.Length > 0)
        app.UsePathBase(basePath);

    app.UseSecurityHeaders(customCsp);
    app.UseResponseCompression();

    var defaultWebRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot-default");
    if (Directory.Exists(defaultWebRoot) && Directory.Exists(webRootPath))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new CompositeFileProvider(
                new PhysicalFileProvider(webRootPath),
                new PhysicalFileProvider(defaultWebRoot)
            )
        });
    }
    else
    {
        app.UseStaticFiles();
    }

    // when theme/ lives in the git-synced repo rather than wwwroot, wwwroot's static file provider can't see it
    if (usingGitTheme)
        app.UseStaticFiles(new StaticFileOptions { FileProvider = new ContentLinks.NoLinkFileProvider(themeDir), RequestPath = "/theme" });

    // Serve user-hosted media/files from /assets/
    var assetsDir = Path.Combine(Path.GetFullPath(docsOptions.RootPath), "assets");
    AssetVersioning.Current = new AssetVersioning(assetsDir);
    if (Directory.Exists(assetsDir))
    {
        var assetContentTypes = AssetContentTypes.Provider();

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new ContentLinks.NoLinkFileProvider(assetsDir),
            RequestPath = "/assets",
            ContentTypeProvider = assetContentTypes,
            ServeUnknownFileTypes = false,
            OnPrepareResponse = ctx =>
            {
                ctx.Context.Response.Headers.CacheControl =
                    !app.Environment.IsDevelopment() && ctx.Context.Request.Query.ContainsKey("v")
                        ? "public,max-age=31536000,immutable"
                        : "no-cache";

                // An .svg navigated to directly runs its own inline script in this origin; the page route's nonce swap doesn't reach static responses.
                if (ctx.File.Name.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                    ctx.Context.Response.Headers.ContentSecurityPolicy = "default-src 'none'; style-src 'unsafe-inline'; sandbox";
            }
        });
    }

    app.UseRouting();
    app.UseRateLimiter();

    app.MapApiEndpoints();
    app.MapSeoEndpoints();
    app.MapAssetEndpoints();
    app.MapContentEndpoints();

    if (cliArgs.TranslateTo is { Length: > 0 } translateTo)
    {
        var translationDocs = app.Services.GetRequiredService<DocumentationService>();
        await translationDocs.StartAsync(CancellationToken.None);

        var sourceCode = cliArgs.TranslateFrom ?? LocaleRouting.RootCode(translationDocs.SiteConfig);
        using var translateHttp = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        var translateClient = new LibreTranslateClient(
            translateHttp,
            cliArgs.TranslateEndpoint ?? "http://localhost:5000",
            sourceCode,
            translateTo,
            cliArgs.TranslateApiKey);

        var outcome = await TranslationRunner.RunAsync(
            new TranslationRequest(docsOptions.RootPath, translateTo, cliArgs.TranslateOverwrite),
            translationDocs.SiteConfig,
            translateClient.TranslateAsync,
            app.Logger,
            CancellationToken.None);

        Log.Information("Translated {Written} pages into {Code}, skipped {Skipped} that already existed",
            outcome.Written, translateTo, outcome.Skipped);
        return;
    }

    if (exportDir != null)
    {
        await StaticSiteExporter.RunAsync(app, exportDir, exportBaseUrl, CancellationToken.None);
        Log.Information("Static export written to {Dir}", exportDir);
        return;
    }

    var urls = app.Urls.Count > 0
        ? app.Urls.ToArray()
        : (Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
            ?? builder.Configuration["urls"]
            ?? "http://localhost:5000").Split(';');

    if (!PortAvailabilityChecker.TryEnsureUrlsAvailable(urls, out var conflictingPort))
    {
        Log.Fatal("Port {Port} is already in use. Stop the existing process and try again.", conflictingPort);
        return;
    }

    Log.Information("Application is hosted on the following URLs:");
    foreach (var url in urls)
    {
        Log.Information("   {Url}", url.Trim());
        Log.Information("");
    }

    app.Lifetime.ApplicationStopping.Register(() =>
    {
        Log.Information("");
        Log.Information("Application shutting down...");
        Log.CloseAndFlush();
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal("");
    Log.Fatal(ex, "Application failed to start.");
    Environment.ExitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}

static string NormalizeBasePath(string? raw)
{
    if (string.IsNullOrWhiteSpace(raw)) return "";
    var trimmed = "/" + raw.Trim().Trim('/');
    return trimmed == "/" ? "" : trimmed;
}

void LogApplicationBanner()
{
    Log.Information("");
    Log.Information("Bark - Your fast documentation server built on .NET");
    Log.Information("");
}
