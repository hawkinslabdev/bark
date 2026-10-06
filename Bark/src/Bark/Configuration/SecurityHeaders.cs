using System.Security.Cryptography;
using System.Text;

namespace Bark.Configuration;

/// <summary>Derives an unpredictable CSP nonce from the ETag, so it stays unique per response and consistent across 304s.</summary>
public static class CspNonce
{
    private static readonly byte[] Key = RandomNumberGenerator.GetBytes(32);

    public static readonly string ProcessSalt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

    public static string Derive(string etag) =>
        Convert.ToBase64String(HMACSHA256.HashData(Key, Encoding.UTF8.GetBytes(etag)), 0, 16);
}

public static class SecurityHeaders
{
    public const string DefaultCsp =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "style-src-attr 'unsafe-inline'; " +
        "img-src 'self' data:; " +
        "font-src 'self' data:; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'";

    public static Task Apply(HttpContext context, Func<Task> next) =>
        Apply(context, next, DefaultCsp);

    public static Task Apply(HttpContext context, Func<Task> next, string contentSecurityPolicy)
    {
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers.XFrameOptions = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers.ContentSecurityPolicy = contentSecurityPolicy;
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";
        context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
        // same-site, not same-origin: sibling hosts routinely embed docs images and fonts.
        context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-site";
        context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";

        if (context.Request.IsHttps)
            context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        return next();
    }

    private static readonly string[] WidenedDirectives =
        ["script-src ", "connect-src ", "img-src ", "frame-src "];

    public static string WithExtraSources(string csp, IReadOnlyList<string> origins)
    {
        if (origins.Count == 0)
            return csp;

        var extra = " " + string.Join(' ', origins);
        var directives = csp.Split(';');
        var seen = new bool[WidenedDirectives.Length];

        for (var i = 0; i < directives.Length; i++)
        {
            var trimmed = directives[i].TrimStart();
            for (var d = 0; d < WidenedDirectives.Length; d++)
            {
                if (trimmed.StartsWith(WidenedDirectives[d], StringComparison.Ordinal))
                {
                    directives[i] = directives[i].TrimEnd() + extra;
                    seen[d] = true;
                    break;
                }
            }
        }

        var result = new List<string>(directives);
        for (var d = 0; d < WidenedDirectives.Length; d++)
            if (!seen[d])
                result.Add(" " + WidenedDirectives[d].TrimEnd() + extra);

        return string.Join(";", result);
    }

    /// <summary>Adds origins to connect-src only, for API pages whose playground calls the API from the browser.</summary>
    public static string WithConnectSources(string csp, IReadOnlyList<string>? origins)
    {
        if (origins is not { Count: > 0 })
            return csp;
        var extra = " " + string.Join(' ', origins);
        var directives = csp.Split(';');
        for (var i = 0; i < directives.Length; i++)
        {
            if (directives[i].TrimStart().StartsWith("connect-src ", StringComparison.Ordinal))
            {
                directives[i] = directives[i].TrimEnd() + extra;
                return string.Join(";", directives);
            }
        }
        return csp.TrimEnd().TrimEnd(';') + "; connect-src 'self'" + extra;
    }

    /// <summary>Allows unnonced <c>&lt;style&gt;</c> elements, for the diagram pages where Mermaid injects its own; scripts stay nonce-only.</summary>
    public static string WithInlineStyleElements(string csp)
    {
        var directives = csp.Split(';');
        for (var i = 0; i < directives.Length; i++)
        {
            if (!directives[i].TrimStart().StartsWith("style-src-elem ", StringComparison.Ordinal))
                continue;

            return directives[i].Contains("'unsafe-inline'", StringComparison.Ordinal)
                ? csp
                : string.Join(";", directives.Select((d, j) => j == i ? d.TrimEnd() + " 'unsafe-inline'" : d));
        }

        return csp + "; style-src-elem 'self' 'unsafe-inline'";
    }

    /// <summary>Appends or swaps per-response nonces into script-src/style-src CSP directives to avoid breaking inline scripts.</summary>
    public static string BuildNonceCsp(string baseCsp, string nonce)
    {
        var noncePart = $"'nonce-{nonce}'";
        var directives = baseCsp.Split(';');
        for (var i = 0; i < directives.Length; i++)
        {
            var trimmed = directives[i].TrimStart();
            if (!trimmed.StartsWith("script-src ") && !trimmed.StartsWith("style-src "))
                continue;

            directives[i] = trimmed.Contains("'unsafe-inline'")
                ? directives[i].Replace("'unsafe-inline'", noncePart)
                : directives[i].TrimEnd() + " " + noncePart;
        }
        return string.Join(";", directives);
    }
}

public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, string? contentSecurityPolicy = null)
    {
        var csp = contentSecurityPolicy ?? SecurityHeaders.DefaultCsp;
        return app.Use((context, next) => SecurityHeaders.Apply(context, next, csp));
    }
}
