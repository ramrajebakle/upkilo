using System.Net;
using System.Net.Sockets;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Shared SSRF protection for all outbound calls to tenant-supplied URLs.
/// Lives in Infrastructure so it has no dependency on the API layer.
/// </summary>
public static class SsrfGuard
{
    public const string PinnedClientName = "ssrf-safe";

    /// <summary>
    /// F-01: Rejects private, loopback, link-local, CGNAT, multicast, reserved, cloud-metadata,
    /// and IPv6 ULA/link-local addresses — including IPv4-mapped-IPv6 (::ffff:a.b.c.d) which
    /// would otherwise bypass an IPv4-only check.
    /// </summary>
    public static bool IsDisallowedAddress(IPAddress address)
    {
        var addr = address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;

        if (IPAddress.IsLoopback(addr)) return true;

        if (addr.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = addr.GetAddressBytes();
            if (b[0] == 0) return true;                               // 0.0.0.0/8 reserved
            if (b[0] == 10) return true;                              // 10.0.0.0/8 private
            if (b[0] == 127) return true;                             // loopback
            if (b[0] == 169 && b[1] == 254) return true;             // link-local + metadata
            if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return true; // 172.16.0.0/12 private
            if (b[0] == 192 && b[1] == 168) return true;             // 192.168.0.0/16 private
            if (b[0] == 100 && b[1] >= 64 && b[1] <= 127) return true; // 100.64.0.0/10 CGNAT
            if (b[0] >= 224) return true;                             // multicast/reserved
        }
        else if (addr.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (addr.IsIPv6LinkLocal || addr.IsIPv6SiteLocal || addr.IsIPv6Multicast) return true;
            var b = addr.GetAddressBytes();
            if ((b[0] & 0xFE) == 0xFC) return true;                  // fc00::/7 unique-local
        }

        return false;
    }

    /// <summary>
    /// F-02: A SocketsHttpHandler whose ConnectCallback re-resolves DNS and connects only to a
    /// validated public address. Closes the TOCTOU/DNS-rebinding window between URL validation
    /// and the actual socket connect. TLS SNI/cert validation still use the original hostname.
    /// </summary>
    public static SocketsHttpHandler CreatePinnedHandler() => new()
    {
        ConnectCallback = async (ctx, ct) =>
        {
            var host = ctx.DnsEndPoint.Host;
            var addresses = await Dns.GetHostAddressesAsync(host, ct);
            var target = addresses.FirstOrDefault(a => !IsDisallowedAddress(a));
            if (target is null)
                throw new IOException($"SSRF protection: '{host}' did not resolve to an allowed address.");

            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(target, ctx.DnsEndPoint.Port, ct);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
    };
}
