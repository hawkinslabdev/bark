using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace Bark.Services;

public sealed record RepoStats(string? Version);

public sealed class RepoStatsProvider
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);

    private readonly HttpClient _http = BuildClient();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private RepoStats? _cached;
    private DateTime _fetchedAt = DateTime.MinValue;

    public async Task<RepoStats?> GetStatsAsync(string repoUrl, CancellationToken cancellationToken)
    {
        if (DateTime.UtcNow - _fetchedAt < CacheDuration)
            return _cached;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (DateTime.UtcNow - _fetchedAt < CacheDuration)
                return _cached;

            _cached = await FetchAsync(repoUrl, cancellationToken) ?? _cached;
            _fetchedAt = DateTime.UtcNow;
            return _cached;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<RepoStats?> FetchAsync(string repoUrl, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(repoUrl, UriKind.Absolute, out var uri))
            return null;

        var path = uri.AbsolutePath.Trim('/');
        if (path.Length == 0)
            return null;

        var isGitHub = uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase);
        var apiBase = isGitHub ? $"https://api.github.com/repos/{path}" : $"{uri.Scheme}://{uri.Host}/api/v1/repos/{path}";

        try
        {
            string? version = null;

            using (var response = await _http.GetAsync($"{apiBase}/releases/latest", cancellationToken))
            {
                if (response.IsSuccessStatusCode)
                {
                    using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                    if (json.RootElement.TryGetProperty("tag_name", out var tagEl))
                        version = tagEl.GetString();
                }
            }

            return version is null ? null : new RepoStats(version);
        }
        catch
        {
            return null;
        }
    }

    // config.json is contributor-writable, so the repo host must not let content reach the server's internal network.
    // The check runs on the connected address (not the hostname) so DNS rebinding can't slip past it, and redirects are
    // off because a public host could otherwise bounce the request inward.
    private static HttpClient BuildClient()
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            ConnectCallback = async (context, cancellationToken) =>
            {
                var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
                var target = addresses.FirstOrDefault(IsPublicAddress)
                    ?? throw new HttpRequestException($"Refusing non-public repo host {context.DnsEndPoint.Host}");

                var socket = new Socket(target.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                try
                {
                    await socket.ConnectAsync(target, context.DnsEndPoint.Port, cancellationToken);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            },
        };
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Bark-DocSite");
        return client;
    }

    internal static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
            return false;

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
            return !(address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6UniqueLocal || address.IsIPv6Multicast);

        var b = address.GetAddressBytes();
        return b[0] switch
        {
            0 or 10 or 127 => false,
            100 => b[1] is < 64 or > 127,  // 100.64.0.0/10 carrier-grade NAT
            169 => b[1] != 254,            // link-local, incl. cloud metadata 169.254.169.254
            172 => b[1] is < 16 or > 31,
            192 => b[1] != 168,
            >= 224 => false,               // multicast and reserved
            _ => true,
        };
    }
}
