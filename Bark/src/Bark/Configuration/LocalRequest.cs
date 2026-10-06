using System.Net;

namespace Bark.Configuration;

/// <summary>Decides whether a request came from the machine Bark runs on, for controls that expose server-side detail.</summary>
/// <remarks>
/// Loopback alone is not enough: a same-host reverse proxy makes every visitor look local, so the request must also carry no proxy hop markers and address Bark by a loopback host.
/// </remarks>
public static class LocalRequest
{
    private static readonly string[] ProxyHeaders =
        ["X-Forwarded-For", "X-Forwarded-Host", "X-Forwarded-Proto", "Forwarded", "X-Real-IP"];

    public static bool IsLocal(HttpContext context)
    {
        if (context.Connection.RemoteIpAddress is not { } remoteIp || !IPAddress.IsLoopback(remoteIp))
            return false;

        foreach (var header in ProxyHeaders)
            if (context.Request.Headers.ContainsKey(header))
                return false;

        return IsLoopbackHost(context.Request.Host.Host);
    }

    private static bool IsLoopbackHost(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || host.Equals("::1", StringComparison.Ordinal)
        || (IPAddress.TryParse(host, out var parsed) && IPAddress.IsLoopback(parsed));
}
