using System.Net;
using System.Net.Sockets;

namespace Upkilo.API.Middleware;

/// <summary>
/// SSRF prevention middleware — provides a static validation helper for controllers
/// that accept user-supplied URLs (webhooks, OAuth callbacks, integrations).
///
/// VULN-013 NOTE: This middleware does NOT automatically intercept and scan every request.
/// InvokeAsync is a pass-through. Protection requires calling ValidateUrlAsync() explicitly
/// before making any outbound HTTP request to a user-provided URL.
///
/// Controllers that MUST call ValidateUrlAsync before making outbound requests:
///   - WebhookService (webhook delivery)
///   - IntegrationsController (OAuth redirect URIs, external API endpoints)
///   - DeveloperController (webhook test endpoints)
///   - MarketingIntegrationService (Mailchimp/ActiveCampaign base URLs)
///   - Any endpoint accepting a `callbackUrl`, `webhookUrl`, or `redirectUri` parameter
/// </summary>
public class SsrfPreventionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SsrfPreventionMiddleware> _logger;

    public SsrfPreventionMiddleware(RequestDelegate next, ILogger<SsrfPreventionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);
    }

    /// <summary>
    /// Validate that a URL does not point to a private/internal IP address.
    /// Use this when accepting user-provided URLs (webhooks, callbacks, etc.).
    /// </summary>
    public static async Task<SsrfValidationResult> ValidateUrlAsync(string url, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            return SsrfValidationResult.Fail("URL cannot be empty.");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return SsrfValidationResult.Fail("Invalid URL format.");

        // Only allow HTTP/HTTPS schemes
        if (uri.Scheme != "http" && uri.Scheme != "https")
            return SsrfValidationResult.Fail($"Scheme '{uri.Scheme}' is not allowed. Only HTTP/HTTPS.");

        // Block localhost/loopback
        if (IsLoopback(uri.Host))
            return SsrfValidationResult.Fail("Localhost/loopback addresses are not allowed.");

        // Block metadata endpoints (cloud providers)
        if (IsCloudMetadataEndpoint(uri.Host))
            return SsrfValidationResult.Fail("Cloud metadata endpoints are not allowed.");

        try
        {
            // Resolve DNS to check actual IP
            var addresses = await Dns.GetHostAddressesAsync(uri.Host);

            foreach (var address in addresses)
            {
                if (IsPrivateOrReservedIp(address))
                {
                    logger?.LogWarning(
                        "SSRF blocked: URL {Url} resolves to private IP {IP}",
                        url, address);
                    return SsrfValidationResult.Fail(
                        $"URL resolves to a private/reserved IP address ({address}).");
                }
            }

            if (addresses.Length == 0)
                return SsrfValidationResult.Fail("URL could not be resolved.");

            return SsrfValidationResult.Success();
        }
        catch (SocketException ex)
        {
            logger?.LogWarning("SSRF validation DNS failure for {Url}: {Error}", url, ex.Message);
            return SsrfValidationResult.Fail($"DNS resolution failed: {ex.Message}");
        }
    }

    /// <summary>Check if an IP address is private, reserved, or internal</summary>
    public static bool IsPrivateOrReservedIp(IPAddress address)
    {
        if (IPAddress.IsLoopback(address)) return true;

        byte[] bytes = address.GetAddressBytes();

        // IPv4
        if (address.AddressFamily == AddressFamily.InterNetwork && bytes.Length == 4)
        {
            // 10.0.0.0/8
            if (bytes[0] == 10) return true;
            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168) return true;
            // 127.0.0.0/8 (loopback)
            if (bytes[0] == 127) return true;
            // 169.254.0.0/16 (link-local / APIPA)
            if (bytes[0] == 169 && bytes[1] == 254) return true;
            // 0.0.0.0/8
            if (bytes[0] == 0) return true;
            // 100.64.0.0/10 (Carrier-grade NAT)
            if (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127) return true;
            // 192.0.0.0/24 (IETF protocol assignments)
            if (bytes[0] == 192 && bytes[1] == 0 && bytes[2] == 0) return true;
            // 198.18.0.0/15 (benchmark testing)
            if (bytes[0] == 198 && (bytes[1] == 18 || bytes[1] == 19)) return true;
            // 224.0.0.0/4 (multicast)
            if (bytes[0] >= 224 && bytes[0] <= 239) return true;
            // 240.0.0.0/4 (reserved)
            if (bytes[0] >= 240) return true;
        }

        // IPv6
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // ::1 loopback
            if (address.Equals(IPAddress.IPv6Loopback)) return true;
            // :: unspecified
            if (address.Equals(IPAddress.IPv6None)) return true;
            // fe80::/10 link-local
            if (bytes[0] == 0xFE && (bytes[1] & 0xC0) == 0x80) return true;
            // fc00::/7 unique local
            if ((bytes[0] & 0xFE) == 0xFC) return true;
            // ::ffff:0:0/96 IPv4-mapped (check the mapped IPv4 portion)
            if (address.IsIPv4MappedToIPv6)
            {
                var ipv4 = address.MapToIPv4();
                return IsPrivateOrReservedIp(ipv4);
            }
        }

        return false;
    }

    private static bool IsLoopback(string host)
    {
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(host, "127.0.0.1", StringComparison.Ordinal) ||
               string.Equals(host, "[::1]", StringComparison.Ordinal) ||
               string.Equals(host, "::1", StringComparison.Ordinal) ||
               string.Equals(host, "0.0.0.0", StringComparison.Ordinal);
    }

    private static bool IsCloudMetadataEndpoint(string host)
    {
        // AWS, GCP, Azure metadata endpoints
        return string.Equals(host, "169.254.169.254", StringComparison.Ordinal) ||
               string.Equals(host, "metadata.google.internal", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(host, "metadata.azure.internal", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Result of SSRF URL validation
/// </summary>
public class SsrfValidationResult
{
    public bool IsValid { get; private set; }
    public string? Error { get; private set; }

    public static SsrfValidationResult Success() => new() { IsValid = true };
    public static SsrfValidationResult Fail(string error) => new() { IsValid = false, Error = error };
}
