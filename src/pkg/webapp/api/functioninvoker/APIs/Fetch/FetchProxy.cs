using System.Net;
using System.Net.Sockets;

namespace WebApp.FunctionInvoker.Apis.Fetch;

public class FetchProxy : IDisposable
{
    private readonly FetchOptions _options;
    private readonly HttpClient _httpClient;
    private int _count;
    private long _totalBytesReceived;

    public FetchProxy(FetchOptions options)
    {
        _options = options;

        var handler = new SocketsHttpHandler
        {
            ConnectCallback = SsrfSafeConnectAsync,
            MaxAutomaticRedirections = options.MaxRedirects,
            AllowAutoRedirect = true,
            PooledConnectionLifetime = TimeSpan.FromMinutes(1)
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(options.TimeoutPerRequestSeconds),
            DefaultRequestHeaders =
            {
                { "User-Agent", "MycroCloud-FunctionInvoker/1.0" }
            }
        };
    }

    public async Task<HttpResponseMessage> Fetch(HttpRequestMessage request)
    {
        PreFetch(request);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request);
        }
        catch (TaskCanceledException)
        {
            throw new FetchTimeoutException(
                $"Request to {request.RequestUri?.Host} timed out after {_options.TimeoutPerRequestSeconds}s");
        }

        PostFetch(response);
        return response;
    }

    private void PreFetch(HttpRequestMessage request)
    {
        _count++;
        if (_count > _options.MaxRequestCount)
            throw new CountLimitException();

        // Scheme allowlist
        var scheme = request.RequestUri?.Scheme;
        if (scheme is not ("http" or "https"))
            throw new FetchSecurityException($"Blocked URL scheme: {scheme}. Only http and https are allowed.");

        // Request body size limit
        if (request.Content is not null)
        {
            var contentLength = request.Content.Headers.ContentLength;
            if (contentLength > _options.MaxRequestBodyBytes)
                throw new FetchSizeLimitException(
                    $"Request body size {contentLength} exceeds limit of {_options.MaxRequestBodyBytes} bytes");
        }
    }

    private void PostFetch(HttpResponseMessage response)
    {
        var contentLength = response.Content.Headers.ContentLength ?? 0;

        // Per-response size limit
        if (contentLength > _options.MaxResponseBodyBytes)
            throw new FetchSizeLimitException(
                $"Response body size {contentLength} exceeds limit of {_options.MaxResponseBodyBytes} bytes");

        // Total bandwidth cap
        _totalBytesReceived += contentLength;
        if (_totalBytesReceived > _options.MaxTotalBytes)
            throw new FetchSizeLimitException(
                $"Total fetch bandwidth {_totalBytesReceived} exceeds limit of {_options.MaxTotalBytes} bytes");
    }

    private static async ValueTask<Stream> SsrfSafeConnectAsync(
        SocketsHttpConnectionContext context, CancellationToken ct)
    {
        var host = context.DnsEndPoint.Host;
        var port = context.DnsEndPoint.Port;

        var addresses = await Dns.GetHostAddressesAsync(host, ct);

        if (addresses.Length == 0)
            throw new FetchSecurityException($"DNS resolution failed for {host}");

        foreach (var addr in addresses)
        {
            if (IsPrivateOrReserved(addr))
                throw new FetchSecurityException($"Blocked request to private/reserved IP: {addr}");
        }

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        try
        {
            await socket.ConnectAsync(addresses[0], port, ct);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private static bool IsPrivateOrReserved(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
            return true;

        // Map IPv6-mapped IPv4 to IPv4 for consistent checking
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // ::1 (loopback, already caught above), fe80::/10 (link-local)
            return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal;
        }

        var bytes = address.GetAddressBytes();
        return bytes switch
        {
            [10, ..]                                    => true,  // 10.0.0.0/8
            [172, >= 16 and <= 31, ..]                  => true,  // 172.16.0.0/12
            [192, 168, ..]                              => true,  // 192.168.0.0/16
            [169, 254, ..]                              => true,  // 169.254.0.0/16 (link-local, cloud metadata)
            [0, ..]                                     => true,  // 0.0.0.0/8
            [100, >= 64 and <= 127, ..]                 => true,  // 100.64.0.0/10 (CGNAT)
            [127, ..]                                   => true,  // 127.0.0.0/8 (extra safety)
            _                                           => false
        };
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
