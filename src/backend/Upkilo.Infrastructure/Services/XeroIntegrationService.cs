using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Integrates with Xero accounting via OAuth2 + REST API (no SDK).
/// </summary>
public class XeroIntegrationService
{
    private const string TokenUrl = "https://identity.xero.com/connect/token";
    private const string InvoicesUrl = "https://api.xero.com/api.xro/2.0/Invoices";

    private readonly ILogger<XeroIntegrationService> _logger;
    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public XeroIntegrationService(
        ILogger<XeroIntegrationService> logger,
        AppDbContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _context = context;
        _httpClientFactory = httpClientFactory;
        _clientId = configuration["Integrations:Xero:ClientId"] ?? string.Empty;
        _clientSecret = configuration["Integrations:Xero:ClientSecret"] ?? string.Empty;
    }

    /// <summary>Exchange auth code for tokens and persist in TenantIntegrations.</summary>
    public async Task ConnectXeroAsync(Guid tenantId, string authCode)
    {
        _logger.LogInformation("Connecting Xero for tenant {TenantId}", tenantId);

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", authCode),
            new KeyValuePair<string, string>("redirect_uri", string.Empty) // caller sets redirect_uri if needed
        });

        var resp = await client.PostAsync(TokenUrl, form);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var accessToken = root.GetProperty("access_token").GetString()!;
        var refreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
        var xeroTenantId = root.TryGetProperty("xero_tenant_id", out var xti) ? xti.GetString() : null;

        var integration = await _context.TenantIntegrations
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.IntegrationId == "xero");

        if (integration == null)
        {
            integration = new TenantIntegration
            {
                TenantId = tenantId,
                IntegrationId = "xero",
                Provider = "Xero",
                IntegrationType = "Accounting"
            };
            _context.TenantIntegrations.Add(integration);
        }

        integration.AccessToken = accessToken;
        integration.RefreshToken = refreshToken;
        integration.ExternalAccountId = xeroTenantId;
        integration.IsActive = true;
        integration.IsConnected = true;
        integration.ConnectedAt = DateTime.UtcNow;
        integration.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Xero connected for tenant {TenantId}", tenantId);
    }

    /// <summary>Fetch authorised Xero invoices and upsert into local Invoices table.</summary>
    public async Task SyncInvoicesAsync(Guid tenantId)
    {
        _logger.LogInformation("Syncing Xero invoices for tenant {TenantId}", tenantId);

        var integration = await _context.TenantIntegrations
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.IntegrationId == "xero");

        if (integration == null || !integration.IsConnected || string.IsNullOrEmpty(integration.AccessToken))
        {
            _logger.LogWarning("Xero not connected for tenant {TenantId}", tenantId);
            return;
        }

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", integration.AccessToken);
        if (!string.IsNullOrEmpty(integration.ExternalAccountId))
            client.DefaultRequestHeaders.Add("Xero-tenant-id", integration.ExternalAccountId);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var resp = await client.GetAsync($"{InvoicesUrl}?where=Status==\"AUTHORISED\"");
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("Invoices", out var invoicesEl))
            return;

        foreach (var inv in invoicesEl.EnumerateArray())
        {
            var xeroInvoiceId = inv.TryGetProperty("InvoiceID", out var idEl) ? idEl.GetString() : null;
            if (string.IsNullOrEmpty(xeroInvoiceId)) continue;

            var existing = await _context.Invoices
                .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.StripeInvoiceId == xeroInvoiceId);

            if (existing == null)
            {
                existing = new Invoice
                {
                    TenantId = tenantId,
                    StripeInvoiceId = xeroInvoiceId, // repurposed as external id
                    InvoiceNumber = inv.TryGetProperty("InvoiceNumber", out var numEl) ? numEl.GetString() ?? string.Empty : string.Empty,
                    Status = InvoiceStatus.Sent,
                    CustomerName = inv.TryGetProperty("Contact", out var contactEl) && contactEl.TryGetProperty("Name", out var nameEl)
                        ? nameEl.GetString() ?? string.Empty : string.Empty,
                    TotalAmount = inv.TryGetProperty("Total", out var totalEl) ? (decimal)totalEl.GetDouble() : 0m,
                    IssueDate = inv.TryGetProperty("DateString", out var dateEl) && DateTime.TryParse(dateEl.GetString(), out var dt) ? dt : DateTime.UtcNow,
                    DueDate = inv.TryGetProperty("DueDateString", out var dueDateEl) && DateTime.TryParse(dueDateEl.GetString(), out var ddt) ? ddt : DateTime.UtcNow.AddDays(30)
                };
                _context.Invoices.Add(existing);
            }
            else
            {
                if (inv.TryGetProperty("Total", out var totalEl2))
                    existing.TotalAmount = (decimal)totalEl2.GetDouble();
                existing.UpdatedAt = DateTime.UtcNow;
            }
        }

        integration.LastSyncAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        _logger.LogInformation("Xero invoice sync complete for tenant {TenantId}", tenantId);
    }

    /// <summary>Refresh the Xero access token using the stored refresh token.</summary>
    public async Task RefreshTokenAsync(Guid tenantId)
    {
        var integration = await _context.TenantIntegrations
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.IntegrationId == "xero");

        if (integration == null || string.IsNullOrEmpty(integration.RefreshToken))
        {
            _logger.LogWarning("No refresh token found for Xero tenant {TenantId}", tenantId);
            return;
        }

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", integration.RefreshToken)
        });

        var resp = await client.PostAsync(TokenUrl, form);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        integration.AccessToken = root.GetProperty("access_token").GetString();
        if (root.TryGetProperty("refresh_token", out var rt))
            integration.RefreshToken = rt.GetString();
        integration.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Xero token refreshed for tenant {TenantId}", tenantId);
    }
}
