using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;


namespace Upkilo.Infrastructure.Services;

public class WebhookService : IWebhookService
{
    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(AppDbContext context, IHttpClientFactory httpClientFactory, ILogger<WebhookService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<Webhook> CreateEndpointAsync(Guid tenantId, string name, string url, string[] events)
    {
        // H-NEW-01 FIX: Validate the user-provided URL against the full SSRF ruleset
        // (private IPs, loopback, cloud metadata endpoints, DNS rebinding) before persisting.
        var (isValid, ssrfError) = await ValidateWebhookUrlAsync(url);
        if (!isValid)
        {
            _logger.LogWarning("SSRF blocked on webhook creation for tenant {TenantId}: {Error}", tenantId, ssrfError);
            throw new InvalidOperationException($"Invalid webhook URL: {ssrfError}");
        }

        var webhook = new Webhook
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Url = url,
            Secret = GenerateSecret(),
            Events = events.ToList()
        };

        _context.Set<Webhook>().Add(webhook);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Webhook endpoint {Name} created for tenant {TenantId}", name, tenantId);
        return webhook;
    }

    public async Task<IEnumerable<Webhook>> GetEndpointsAsync(Guid tenantId)
    {
        return await Task.FromResult(
            _context.Set<Webhook>()
                .Where(e => e.TenantId == tenantId)
                .OrderByDescending(e => e.CreatedAt)
                .ToList()
        );
    }

    /// <summary>
    /// INTERNAL USE ONLY — not tenant-scoped. Used by the cross-tenant background
    /// delivery processor (<see cref="ProcessPendingDeliveriesAsync"/>). Controllers must
    /// NEVER call this with a user-supplied id — use a tenant-scoped lookup to avoid IDOR.
    /// </summary>
    public async Task<Webhook?> GetEndpointAsync(Guid id)
    {
        return await Task.FromResult(_context.Set<Webhook>().Find(id));
    }

    public async Task<bool> DeleteEndpointAsync(Guid id, Guid tenantId)
    {
        var webhook = _context.Set<Webhook>()
            .FirstOrDefault(e => e.Id == id && e.TenantId == tenantId);
        if (webhook == null) return false;

        _context.Set<Webhook>().Remove(webhook);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateEndpointAsync(Guid id, Guid tenantId, string? name, string? url, string[]? events, bool? isActive)
    {
        var webhook = _context.Set<Webhook>()
            .FirstOrDefault(e => e.Id == id && e.TenantId == tenantId);
        if (webhook == null) return false;

        if (name != null) webhook.Name = name;

        // H-NEW-01 FIX: Validate new URL on update — prevents SSRF via URL edit.
        if (url != null)
        {
            var (isValid, ssrfError) = await ValidateWebhookUrlAsync(url);
            if (!isValid)
            {
                _logger.LogWarning("SSRF blocked on webhook update {Id} for tenant {TenantId}: {Error}", id, tenantId, ssrfError);
                throw new InvalidOperationException($"Invalid webhook URL: {ssrfError}");
            }
            webhook.Url = url;
        }

        if (events != null) webhook.Events = events.ToList();
        if (isActive.HasValue) webhook.IsActive = isActive.Value;
        webhook.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task DispatchEventAsync(Guid tenantId, string eventType, object payload)
    {
        var webhooks = _context.Set<Webhook>()
            .Where(e => e.TenantId == tenantId && e.IsActive)
            .ToList();

        foreach (var webhook in webhooks)
        {
            if (!webhook.Events.Contains(eventType) && !webhook.Events.Contains("*"))
                continue;

            var delivery = new WebhookDelivery
            {
                Id = Guid.NewGuid(),
                WebhookId = webhook.Id,
                EventType = eventType,
                Payload = JsonSerializer.Serialize(payload)
            };

            _context.Set<WebhookDelivery>().Add(delivery);
            webhook.LastTriggeredAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Event {EventType} dispatched to {Count} webhooks", eventType, webhooks.Count);
    }

    public async Task<WebhookDelivery> SendTestEventAsync(Guid endpointId, Guid tenantId)
    {
        var webhook = _context.Set<Webhook>()
            .FirstOrDefault(e => e.Id == endpointId && e.TenantId == tenantId);

        if (webhook == null)
            throw new InvalidOperationException("Webhook not found");

        var delivery = new WebhookDelivery
        {
            Id = Guid.NewGuid(),
            WebhookId = endpointId,
            EventType = "test.event",
            Payload = JsonSerializer.Serialize(new
            {
                message = "This is a test event",
                timestamp = DateTime.UtcNow
            })
        };

        _context.Set<WebhookDelivery>().Add(delivery);
        await _context.SaveChangesAsync();

        // Try to deliver immediately
        await DeliverWebhookAsync(delivery, webhook);
        return delivery;
    }

    public async Task<IEnumerable<WebhookDelivery>> GetDeliveriesAsync(Guid tenantId, Guid? endpointId = null, int limit = 50)
    {
        var webhookIds = _context.Set<Webhook>()
            .Where(w => w.TenantId == tenantId)
            .Select(w => w.Id)
            .ToList();

        var query = _context.Set<WebhookDelivery>()
            .Where(d => webhookIds.Contains(d.WebhookId));

        if (endpointId.HasValue)
            query = query.Where(d => d.WebhookId == endpointId.Value);

        return await Task.FromResult(
            query.OrderByDescending(d => d.CreatedAt)
                 .Take(limit)
                 .ToList()
        );
    }

    public async Task<bool> ResendDeliveryAsync(Guid deliveryId, Guid tenantId)
    {
        var delivery = await _context.Set<WebhookDelivery>().FindAsync(deliveryId);
        if (delivery == null) return false;

        var webhook = await _context.Set<Webhook>().FirstOrDefaultAsync(w => w.Id == delivery.WebhookId && w.TenantId == tenantId);
        if (webhook == null) return false;

        // Reset and attempt immediate send
        delivery.AttemptNumber = 0;
        delivery.Success = false;
        delivery.Error = null;
        delivery.UpdatedAt = DateTime.UtcNow;

        await DeliverWebhookAsync(delivery, webhook);
        return true;
    }

    public async Task<bool> ClearDeliveriesAsync(Guid endpointId, Guid tenantId)
    {
        var webhook = await _context.Set<Webhook>().FirstOrDefaultAsync(w => w.Id == endpointId && w.TenantId == tenantId);
        if (webhook == null) return false;

        var deliveries = await _context.Set<WebhookDelivery>().Where(d => d.WebhookId == endpointId).ToListAsync();
        _context.Set<WebhookDelivery>().RemoveRange(deliveries);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task ProcessPendingDeliveriesAsync()
    {
        var now = DateTime.UtcNow;
        var pending = _context.Set<WebhookDelivery>()
            .Where(d => !d.Success && d.AttemptNumber < 10)
            .ToList();

        // Filter based on exponential backoff: 2^attempt minutes
        var toProcess = pending.Where(d =>
        {
            var delayMinutes = Math.Pow(2, d.AttemptNumber);
            return d.UpdatedAt.AddMinutes(delayMinutes) <= now;
        }).Take(100).ToList();

        foreach (var delivery in toProcess)
        {
            var webhook = await GetEndpointAsync(delivery.WebhookId);
            if (webhook == null || !webhook.IsActive) continue;

            await DeliverWebhookAsync(delivery, webhook);
        }
    }

    private async Task DeliverWebhookAsync(WebhookDelivery delivery, Webhook webhook)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var (isValid, ssrfError) = await ValidateWebhookUrlAsync(webhook.Url);
            if (!isValid)
            {
                delivery.AttemptNumber = 99; // Kill it
                delivery.Error = $"SSRF Protection: {ssrfError}";
                await _context.SaveChangesAsync();
                return;
            }

            // F-02: named DNS-pinned client (registered in Program.cs) — connects only to a
            // validated public IP, closing the rebinding window.
            var client = _httpClientFactory.CreateClient(SsrfGuard.PinnedClientName);

            var signature = ComputeSignature(delivery.Payload, webhook.Secret);

            var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url);
            request.Content = new StringContent(delivery.Payload, Encoding.UTF8, "application/json");
            request.Headers.Add("X-Webhook-Signature", signature);
            request.Headers.Add("X-Webhook-Event", delivery.EventType);
            request.Headers.Add("X-Webhook-Delivery-Id", delivery.Id.ToString());

            var response = await client.SendAsync(request);

            delivery.AttemptNumber++;
            delivery.ResponseStatusCode = (int)response.StatusCode;
            delivery.ResponseBody = (await response.Content.ReadAsStringAsync()).Substring(0, Math.Min(1000, (await response.Content.ReadAsStringAsync()).Length));
            delivery.DurationMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            if (response.IsSuccessStatusCode)
            {
                delivery.Success = true;
                webhook.LastSuccessAt = DateTime.UtcNow;
                webhook.FailureCount = 0;
            }
            else
            {
                delivery.Error = $"HTTP {delivery.ResponseStatusCode}";
                webhook.LastFailureAt = DateTime.UtcNow;
                webhook.FailureCount++;
            }
        }
        catch (Exception ex)
        {
            delivery.AttemptNumber++;
            delivery.Error = ex.Message;
            delivery.DurationMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            webhook.LastFailureAt = DateTime.UtcNow;
            webhook.LastError = ex.Message;
            _logger.LogError(ex, "Failed to deliver webhook {DeliveryId}", delivery.Id);
        }

        await _context.SaveChangesAsync();
    }

    private static string GenerateSecret()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return $"whsec_{Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "")}";
    }

    private static string ComputeSignature(string payload, string secret)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return $"sha256={Convert.ToHexString(hash).ToLower()}";
    }

    public async Task<bool> SendWebhookRequestAsync(string url, string method, object payload, Dictionary<string, string>? headers = null)
    {
        try
        {
            var (isValid, ssrfError) = await ValidateWebhookUrlAsync(url);
            if (!isValid)
            {
                _logger.LogWarning("SSRF Protection blocked webhook request to {Url}: {Error}", url, ssrfError);
                return false;
            }

            // F-02: named DNS-pinned client (registered in Program.cs).
            var client = _httpClientFactory.CreateClient(SsrfGuard.PinnedClientName);

            var httpMethod = new HttpMethod(method.ToUpperInvariant());
            var request = new HttpRequestMessage(httpMethod, url);

            if (payload != null && (httpMethod == HttpMethod.Post || httpMethod == HttpMethod.Put || httpMethod == HttpMethod.Patch))
            {
                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            var response = await client.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute workflow HTTP request to {Url}", url);
            return false;
        }
    }

    private static bool IsInternalIP(string host)
    {
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return true;
        if (System.Net.IPAddress.TryParse(host, out var ip))
        {
            // Simple check for common private ranges
            var bytes = ip.GetAddressBytes();
            if (bytes[0] == 10) return true;
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
            if (bytes[0] == 192 && bytes[1] == 168) return true;
            if (bytes[0] == 127) return true;
        }
        return false;
    }

    /// <summary>
    /// H-NEW-01: Full SSRF validation — scheme, loopback, cloud metadata, DNS resolution.
    /// Self-contained so Infrastructure does not depend on Upkilo.API.
    /// </summary>
    private static async Task<(bool IsValid, string? Error)> ValidateWebhookUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return (false, "URL cannot be empty.");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return (false, "Invalid URL format.");
        if (uri.Scheme != "http" && uri.Scheme != "https")
            return (false, $"Scheme '{uri.Scheme}' is not allowed. Only HTTP/HTTPS.");

        var host = uri.Host;

        // Block loopback by name
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            host == "127.0.0.1" || host == "::1" || host == "0.0.0.0")
            return (false, "Localhost/loopback addresses are not allowed.");

        // Block cloud metadata endpoints
        if (host == "169.254.169.254" ||
            host.Equals("metadata.google.internal", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("metadata.azure.internal", StringComparison.OrdinalIgnoreCase))
            return (false, "Cloud metadata endpoints are not allowed.");

        // DNS resolution check — covers IPv4, IPv6 (ULA/link-local), and IPv4-mapped-IPv6.
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host);
            if (addresses.Length == 0) return (false, "URL could not be resolved.");
            foreach (var address in addresses)
            {
                if (SsrfGuard.IsDisallowedAddress(address))
                    return (false, $"URL resolves to a private/reserved address ({address}).");
            }
        }
        catch (SocketException ex)
        {
            return (false, $"DNS resolution failed: {ex.Message}");
        }

        return (true, null);
    }
}
