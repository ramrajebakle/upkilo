using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.Json;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class IntegrationsController : ControllerBase
{
    private readonly ILogger<IntegrationsController> _logger;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IEncryptionService _encryption;

    public IntegrationsController(
        ILogger<IntegrationsController> logger,
        AppDbContext context,
        ITenantProvider tenantProvider,
        IEncryptionService encryption)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
        _encryption = encryption;
    }

    // ── Static catalog ──────────────────────────────────────────────────────────

    private static readonly object[] IntegrationCatalog = new object[]
    {
        // Payment gateways
        new { id = "stripe",       name = "Stripe",          category = "payment",
              description = "Accept cards, subscriptions, and invoices globally.",
              icon = "/integrations/stripe.svg",
              authType = "api_key",
              fields = new[] { new { key = "api_key", label = "Secret Key", secret = true, placeholder = "sk_live_..." } },
              features = new[] { "Card payments", "Subscriptions", "Invoicing", "Connect" } },
        new { id = "razorpay",     name = "Razorpay",        category = "payment",
              description = "Accept payments in India via UPI, cards, wallets, and net banking.",
              icon = "/integrations/razorpay.svg",
              authType = "key_pair",
              fields = new[] {
                  new { key = "key_id",     label = "Key ID",     secret = false, placeholder = "rzp_live_..." },
                  new { key = "key_secret", label = "Key Secret", secret = true,  placeholder = "Your Razorpay key secret" }
              },
              features = new[] { "UPI", "Cards", "Wallets", "Net Banking" } },
        new { id = "paypal",       name = "PayPal",           category = "payment",
              description = "Accept PayPal payments and international transfers.",
              icon = "/integrations/paypal.svg",
              authType = "key_pair",
              fields = new[] {
                  new { key = "client_id",     label = "Client ID",     secret = false, placeholder = "Your PayPal client ID" },
                  new { key = "client_secret", label = "Client Secret", secret = true,  placeholder = "Your PayPal client secret" }
              },
              features = new[] { "PayPal Checkout", "International", "Buyer Protection" } },
        // Email
        new { id = "sendgrid",     name = "SendGrid",         category = "email",
              description = "Transactional email delivery with analytics.",
              icon = "/integrations/sendgrid.svg",
              authType = "api_key",
              fields = new[] { new { key = "api_key", label = "API Key", secret = true, placeholder = "SG...." } },
              features = new[] { "Transactional email", "Templates", "Analytics" } },
        new { id = "mailgun",      name = "Mailgun",           category = "email",
              description = "Reliable email API for developers.",
              icon = "/integrations/mailgun.svg",
              authType = "key_pair",
              fields = new[] {
                  new { key = "api_key", label = "API Key",  secret = true,  placeholder = "key-..." },
                  new { key = "domain",  label = "Domain",   secret = false, placeholder = "mg.yourdomain.com" }
              },
              features = new[] { "Transactional email", "Webhooks", "Analytics" } },
        // SMS
        new { id = "twilio",       name = "Twilio",            category = "sms",
              description = "SMS notifications, two-way messaging, and phone verification.",
              icon = "/integrations/twilio.svg",
              authType = "key_pair",
              fields = new[] {
                  new { key = "account_sid", label = "Account SID", secret = false, placeholder = "ACxxxxxxxxxxxxxxx" },
                  new { key = "auth_token",  label = "Auth Token",  secret = true,  placeholder = "Your auth token" },
                  new { key = "from_number", label = "From Number", secret = false, placeholder = "+1..." }
              },
              features = new[] { "SMS messaging", "Two-way SMS", "Voice" } },
        // Calendar
        new { id = "google-calendar", name = "Google Calendar", category = "calendar",
              description = "Sync appointments with Google Calendar automatically.",
              icon = "/integrations/google-calendar.svg",
              authType = "oauth",
              fields = new[] { new { key = "access_token", label = "OAuth Token", secret = true, placeholder = "Granted via OAuth flow" } },
              features = new[] { "Two-way sync", "Real-time updates", "Multiple calendars" } },
        new { id = "outlook",      name = "Microsoft Outlook", category = "calendar",
              description = "Sync appointments with Outlook Calendar.",
              icon = "/integrations/outlook.svg",
              authType = "oauth",
              fields = new[] { new { key = "access_token", label = "OAuth Token", secret = true, placeholder = "Granted via OAuth flow" } },
              features = new[] { "Two-way sync", "Real-time updates" } },
        // Storage
        new { id = "aws-s3",       name = "Amazon S3",         category = "storage",
              description = "Store uploads, documents, and backups in S3.",
              icon = "/integrations/aws-s3.svg",
              authType = "key_pair",
              fields = new[] {
                  new { key = "access_key_id",     label = "Access Key ID",     secret = false, placeholder = "AKIA..." },
                  new { key = "secret_access_key", label = "Secret Access Key", secret = true,  placeholder = "Your AWS secret" },
                  new { key = "bucket",            label = "Bucket Name",       secret = false, placeholder = "my-upkilo-bucket" },
                  new { key = "region",            label = "Region",            secret = false, placeholder = "ap-south-1" }
              },
              features = new[] { "File storage", "CDN", "Backups" } },
        // Analytics
        new { id = "google-analytics", name = "Google Analytics", category = "analytics",
              description = "Track booking funnel, client sources, and revenue attribution.",
              icon = "/integrations/google-analytics.svg",
              authType = "api_key",
              fields = new[] { new { key = "measurement_id", label = "Measurement ID", secret = false, placeholder = "G-XXXXXXXXXX" } },
              features = new[] { "Funnel tracking", "Goal attribution", "Audience insights" } },
        new { id = "mixpanel",     name = "Mixpanel",           category = "analytics",
              description = "Event-based analytics and retention analysis.",
              icon = "/integrations/mixpanel.svg",
              authType = "api_key",
              fields = new[] { new { key = "project_token", label = "Project Token", secret = false, placeholder = "Your Mixpanel token" } },
              features = new[] { "Event tracking", "Funnels", "Retention" } },
        // CRM
        new { id = "hubspot",      name = "HubSpot",            category = "crm",
              description = "Sync clients and bookings with HubSpot CRM.",
              icon = "/integrations/hubspot.svg",
              authType = "api_key",
              fields = new[] { new { key = "api_key", label = "API Key", secret = true, placeholder = "pat-na1-..." } },
              features = new[] { "Contact sync", "Deal tracking", "Email automation" } },
        // Notifications
        new { id = "slack",        name = "Slack",              category = "notifications",
              description = "Receive booking alerts and daily reports in Slack.",
              icon = "/integrations/slack.svg",
              authType = "api_key",
              fields = new[] { new { key = "webhook_url", label = "Webhook URL", secret = true, placeholder = "https://hooks.slack.com/services/..." } },
              features = new[] { "Booking alerts", "Daily reports", "Staff notifications" } },
        // Automation
        new { id = "zapier",       name = "Zapier",             category = "automation",
              description = "Connect 5,000+ apps with no-code automations.",
              icon = "/integrations/zapier.svg",
              authType = "api_key",
              fields = new[] { new { key = "api_key", label = "Upkilo API Key", secret = true, placeholder = "Generated by Upkilo" } },
              features = new[] { "Triggers", "Actions", "Multi-step zaps" } },
    };

    // F-09: valid integration ids, derived once from the catalog above.
    private static readonly HashSet<string> ValidIntegrationIds =
        IntegrationCatalog.Select(i => (string)((dynamic)i).id).ToHashSet(StringComparer.OrdinalIgnoreCase);

    // ── GET /integrations ───────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetIntegrations()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var connectedRows = await _context.TenantIntegrations
            .Where(ti => ti.TenantId == tenantId.Value && ti.IsConnected && !ti.IsDeleted)
            .Select(ti => new
            {
                ti.IntegrationId,
                ti.IsVerified,
                ti.LastVerifiedAt,
                ti.VerificationError,
                ti.ConnectedAt,
                ti.LastSyncAt,
                ti.ExternalAccountId
            })
            .ToListAsync();

        var connectedMap = connectedRows.ToDictionary(r => r.IntegrationId);

        var result = IntegrationCatalog.Select(item =>
        {
            dynamic d = item;
            string id = d.id;
            connectedMap.TryGetValue(id, out var row);
            return new
            {
                item,
                isConnected = row != null,
                isVerified = row?.IsVerified ?? false,
                lastVerifiedAt = row?.LastVerifiedAt,
                verificationError = row?.VerificationError,
                connectedAt = row?.ConnectedAt,
                lastSyncAt = row?.LastSyncAt,
                externalAccountId = row?.ExternalAccountId
            };
        });

        return Ok(new { data = result });
    }

    // ── GET /integrations/{id} ──────────────────────────────────────────────────

    [HttpGet("{id}")]
    public async Task<IActionResult> GetIntegration(string id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var integration = await _context.TenantIntegrations
            .FirstOrDefaultAsync(ti => ti.IntegrationId == id && ti.TenantId == tenantId.Value && !ti.IsDeleted);

        return Ok(new
        {
            id,
            isConnected = integration?.IsConnected ?? false,
            isVerified = integration?.IsVerified ?? false,
            connectedAt = integration?.ConnectedAt,
            lastSyncAt = integration?.LastSyncAt,
            lastVerifiedAt = integration?.LastVerifiedAt,
            verificationError = integration?.VerificationError,
            externalAccountId = integration?.ExternalAccountId,
            settings = integration?.Settings,
            hasCredentials = integration != null && !string.IsNullOrEmpty(integration.EncryptedCredentials)
        });
    }

    // ── POST /integrations/{id}/connect ────────────────────────────────────────

    [HttpPost("{id}/connect")]
    [EnableRateLimiting("fixed")]
    public async Task<IActionResult> Connect(string id, [FromBody] ConnectIntegrationRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // F-09: reject ids that aren't in the catalog instead of persisting junk rows.
        if (!ValidIntegrationIds.Contains(id))
            return BadRequest(new { error = "unknown_integration", message = $"'{id}' is not a supported integration." });

        var creds = request.Credentials ?? new Dictionary<string, string>();
        if (creds.Count == 0)
        {
            if (!string.IsNullOrEmpty(request.ApiKey)) creds["api_key"] = request.ApiKey;
            if (!string.IsNullOrEmpty(request.AccessToken)) creds["access_token"] = request.AccessToken;
            if (!string.IsNullOrEmpty(request.RefreshToken)) creds["refresh_token"] = request.RefreshToken;
        }

        if (creds.Count == 0)
            return BadRequest(new { error = "credentials_required", message = "No credentials supplied." });

        var encryptedJson = _encryption.Encrypt(JsonSerializer.Serialize(creds));

        var existing = await _context.TenantIntegrations
            .FirstOrDefaultAsync(ti => ti.IntegrationId == id && ti.TenantId == tenantId.Value);

        if (existing != null)
        {
            existing.EncryptedCredentials = encryptedJson;
            existing.IsConnected = true;
            existing.IsActive = true;
            existing.IsVerified = false;
            existing.VerificationError = null;
            existing.ConnectedAt = DateTime.UtcNow;
            existing.AccessToken = null;
            existing.RefreshToken = null;
            existing.ApiKey = null;
            if (request.Settings != null)
                existing.Settings = JsonSerializer.Serialize(request.Settings);
            existing.IsDeleted = false;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.TenantIntegrations.Add(new TenantIntegration
            {
                TenantId = tenantId.Value,
                IntegrationId = id,
                IsConnected = true,
                IsActive = true,
                IsVerified = false,
                EncryptedCredentials = encryptedJson,
                ConnectedAt = DateTime.UtcNow,
                Settings = request.Settings != null ? JsonSerializer.Serialize(request.Settings) : null
            });
        }

        await _context.SaveChangesAsync();
        await WriteAuditAsync(tenantId.Value, id, "connected");

        _logger.LogInformation("[Integration] Tenant={TenantId} connected {IntegrationId}", tenantId, id);

        return Ok(new
        {
            success = true,
            isConnected = true,
            connectedAt = DateTime.UtcNow.ToString("o"),
            message = $"{id} connected. Run a connection test to verify credentials."
        });
    }

    // ── POST /integrations/{id}/disconnect ─────────────────────────────────────

    [HttpPost("{id}/disconnect")]
    [EnableRateLimiting("fixed")]
    public async Task<IActionResult> Disconnect(string id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var integration = await _context.TenantIntegrations
            .FirstOrDefaultAsync(ti => ti.IntegrationId == id && ti.TenantId == tenantId.Value && !ti.IsDeleted);

        if (integration != null)
        {
            integration.IsConnected = false;
            integration.IsActive = false;
            integration.IsVerified = false;
            integration.EncryptedCredentials = null;
            integration.AccessToken = null;
            integration.RefreshToken = null;
            integration.ApiKey = null;
            integration.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        await WriteAuditAsync(tenantId.Value, id, "disconnected");
        _logger.LogInformation("[Integration] Tenant={TenantId} disconnected {IntegrationId}", tenantId, id);

        return Ok(new { success = true, isConnected = false, message = $"{id} disconnected and credentials removed." });
    }

    // ── POST /integrations/{id}/test ───────────────────────────────────────────

    [HttpPost("{id}/test")]
    [EnableRateLimiting("fixed")]
    public async Task<IActionResult> TestConnection(string id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var integration = await _context.TenantIntegrations
            .FirstOrDefaultAsync(ti => ti.IntegrationId == id && ti.TenantId == tenantId.Value && ti.IsConnected && !ti.IsDeleted);

        if (integration == null)
            return Ok(new { success = false, status = "not_connected", message = "Integration is not connected." });

        Dictionary<string, string> creds;
        try
        {
            var json = _encryption.DecryptOrNull(integration.EncryptedCredentials)
                       ?? BuildLegacyCredsJson(integration);
            creds = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Integration] Credential decrypt failed for {IntegrationId}", id);
            return Ok(new { success = false, status = "error", message = "Could not decrypt credentials. Please reconnect." });
        }

        bool valid;
        string message;
        string? externalAccountId = integration.ExternalAccountId;

        try
        {
            (valid, message, externalAccountId) = await VerifyProviderAsync(id, creds, integration.ExternalAccountId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Integration] Verification failed for {IntegrationId}", id);
            valid = false;
            message = "Connection check failed: " + ex.Message;
        }

        integration.IsVerified = valid;
        integration.LastVerifiedAt = DateTime.UtcNow;
        integration.VerificationError = valid ? null : message;
        if (valid)
        {
            integration.LastSyncAt = DateTime.UtcNow;
            if (externalAccountId != null) integration.ExternalAccountId = externalAccountId;
        }
        await _context.SaveChangesAsync();

        await WriteAuditAsync(tenantId.Value, id, valid ? "verified" : "verify_failed",
            details: valid ? null : new { error = message });

        _logger.LogInformation("[Integration] Test {IntegrationId}: {Status}", id, valid ? "OK" : "FAILED");
        return Ok(new { success = valid, status = valid ? "connected" : "error", message });
    }

    // ── PUT /integrations/{id}/settings ────────────────────────────────────────

    [HttpPut("{id}/settings")]
    public async Task<IActionResult> UpdateSettings(string id, [FromBody] UpdateIntegrationSettingsRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var integration = await _context.TenantIntegrations
            .FirstOrDefaultAsync(ti => ti.IntegrationId == id && ti.TenantId == tenantId.Value && !ti.IsDeleted);

        if (integration == null) return NotFound();

        if (request.CustomSettings != null)
            integration.Settings = JsonSerializer.Serialize(request.CustomSettings);
        integration.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await WriteAuditAsync(tenantId.Value, id, "settings_updated");
        return Ok(new { success = true });
    }

    // ── POST /integrations/{id}/disable ────────────────────────────────────────

    [HttpPost("{id}/disable")]
    public async Task<IActionResult> DisableIntegration(string id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var integration = await _context.TenantIntegrations
            .FirstOrDefaultAsync(ti => ti.IntegrationId == id && ti.TenantId == tenantId.Value && !ti.IsDeleted);

        if (integration == null) return NotFound();

        integration.IsActive = false;
        integration.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, isActive = false });
    }

    // ── GET /integrations/{id}/logs ────────────────────────────────────────────

    [HttpGet("{id}/logs")]
    public async Task<IActionResult> GetIntegrationLogs(string id, [FromQuery] int limit = 50)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        limit = Math.Clamp(limit, 1, 200);

        var logs = await _context.TenantIntegrationAudits
            .Where(a => a.TenantId == tenantId.Value && a.IntegrationId == id)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .Select(a => new { a.Id, a.Action, a.Timestamp, a.Details, a.ActorUserId })
            .ToListAsync();

        return Ok(new { data = logs });
    }

    // ── POST /integrations/{id}/api-key ────────────────────────────────────────

    [HttpPost("{id}/api-key")]
    [EnableRateLimiting("fixed")]
    public async Task<IActionResult> GenerateApiKey(string id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var integration = await _context.TenantIntegrations
            .FirstOrDefaultAsync(ti => ti.IntegrationId == id && ti.TenantId == tenantId.Value && !ti.IsDeleted);

        if (integration == null) return NotFound();

        var randomBytes = new byte[24];
        RandomNumberGenerator.Fill(randomBytes);
        var rawKey = Convert.ToBase64String(randomBytes).Replace("+", "").Replace("/", "").Replace("=", "");
        var apiKey = $"upk_int_{rawKey}";

        var existingCreds = _encryption.DecryptOrNull(integration.EncryptedCredentials) is { } c
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(c) ?? new()
            : new Dictionary<string, string>();
        existingCreds["api_key"] = apiKey;
        integration.EncryptedCredentials = _encryption.Encrypt(JsonSerializer.Serialize(existingCreds));
        integration.ApiKey = null;
        integration.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await WriteAuditAsync(tenantId.Value, id, "api_key_generated");

        return Ok(new { success = true, apiKey, message = "Save this key now. It will not be shown again." });
    }

    // ── NE4: Migration wizard ───────────────────────────────────────────────────

    [HttpGet("migration/platforms")]
    public IActionResult GetMigrationPlatforms()
        => Ok(new { platforms = MigrationPlatforms.Supported, count = MigrationPlatforms.Supported.Length });

    [HttpPost("migration/start")]
    public async Task<IActionResult> StartMigration([FromBody] MigrationStartRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var platform = MigrationPlatforms.Supported
            .FirstOrDefault(p => ((dynamic)p).id == request.SourcePlatform);
        if (platform == null)
            return BadRequest(new { error = "unsupported_platform", message = $"'{request.SourcePlatform}' is not supported." });

        var jobId = Guid.NewGuid();
        _logger.LogInformation("[NE4] Migration started: tenant={TenantId} from={Platform} jobId={JobId}",
            tenantId, request.SourcePlatform, jobId);

        return Accepted(new
        {
            jobId,
            status = "queued",
            sourcePlatform = request.SourcePlatform,
            dataTypes = request.DataTypes ?? new[] { "clients", "services", "bookings" },
            estimatedMinutes = 15,
            pollUrl = $"/api/v1/integrations/migration/status/{jobId}",
            instructions = $"Export your data from {request.SourcePlatform} as CSV and upload via /settings/import.",
            supportEmail = "migration-support@upkilo.com"
        });
    }

    [HttpGet("migration/status/{jobId}")]
    public IActionResult GetMigrationStatus(Guid jobId)
        => Ok(new { jobId, status = "processing", progress = 0, estimatedCompletionMinutes = 10 });

    // ── Private helpers ─────────────────────────────────────────────────────────

    private async Task<(bool valid, string message, string? externalId)> VerifyProviderAsync(
        string integrationId, Dictionary<string, string> creds, string? existingAccountId)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        switch (integrationId)
        {
            case "stripe":
            {
                var key = creds.GetValueOrDefault("api_key") ?? creds.GetValueOrDefault("access_token") ?? string.Empty;
                if (string.IsNullOrEmpty(key)) return (false, "No API key stored. Please reconnect.", null);
                http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
                var res = await http.GetAsync("https://api.stripe.com/v1/account");
                if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return (false, "Stripe API key is invalid or revoked.", null);
                if (!res.IsSuccessStatusCode)
                    return (false, $"Stripe returned HTTP {(int)res.StatusCode}.", null);
                var body = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                var accountId = doc.RootElement.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                return (true, "Stripe connected successfully.", accountId);
            }

            case "razorpay":
            {
                var keyId = creds.GetValueOrDefault("key_id") ?? string.Empty;
                var keySecret = creds.GetValueOrDefault("key_secret") ?? string.Empty;
                if (string.IsNullOrEmpty(keyId) || string.IsNullOrEmpty(keySecret))
                    return (false, "Missing Razorpay Key ID or Key Secret.", null);
                var encoded = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{keyId}:{keySecret}"));
                http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encoded);
                var res = await http.GetAsync("https://api.razorpay.com/v1/payments?count=1");
                if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return (false, "Razorpay credentials are invalid.", null);
                return res.IsSuccessStatusCode
                    ? (true, "Razorpay connected successfully.", keyId)
                    : (false, $"Razorpay returned HTTP {(int)res.StatusCode}.", null);
            }

            case "paypal":
            {
                var clientId = creds.GetValueOrDefault("client_id") ?? string.Empty;
                var clientSecret = creds.GetValueOrDefault("client_secret") ?? string.Empty;
                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                    return (false, "Missing PayPal Client ID or Client Secret.", null);
                var encoded = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}"));
                var tokenReq = new HttpRequestMessage(HttpMethod.Post, "https://api-m.paypal.com/v1/oauth2/token");
                tokenReq.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encoded);
                tokenReq.Content = new FormUrlEncodedContent(
                    new[] { new KeyValuePair<string, string>("grant_type", "client_credentials") });
                var res = await http.SendAsync(tokenReq);
                if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return (false, "PayPal credentials are invalid.", null);
                return res.IsSuccessStatusCode
                    ? (true, "PayPal connected successfully.", clientId)
                    : (false, $"PayPal returned HTTP {(int)res.StatusCode}.", null);
            }

            case "sendgrid":
            {
                var key = creds.GetValueOrDefault("api_key") ?? string.Empty;
                if (string.IsNullOrEmpty(key)) return (false, "No SendGrid API key stored.", null);
                http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
                var res = await http.GetAsync("https://api.sendgrid.com/v3/user/profile");
                if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return (false, "SendGrid API key is invalid.", null);
                return res.IsSuccessStatusCode
                    ? (true, "SendGrid connected successfully.", null)
                    : (false, $"SendGrid returned HTTP {(int)res.StatusCode}.", null);
            }

            case "mailgun":
            {
                var key = creds.GetValueOrDefault("api_key") ?? string.Empty;
                var domain = creds.GetValueOrDefault("domain") ?? string.Empty;
                if (string.IsNullOrEmpty(key)) return (false, "No Mailgun API key stored.", null);
                var encoded = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"api:{key}"));
                http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encoded);
                var res = await http.GetAsync("https://api.mailgun.net/v3/domains");
                if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return (false, "Mailgun API key is invalid.", null);
                return res.IsSuccessStatusCode
                    ? (true, "Mailgun connected successfully.", domain)
                    : (false, $"Mailgun returned HTTP {(int)res.StatusCode}.", null);
            }

            case "twilio":
            {
                var sid = creds.GetValueOrDefault("account_sid") ?? string.Empty;
                var token = creds.GetValueOrDefault("auth_token") ?? string.Empty;
                if (string.IsNullOrEmpty(sid) || string.IsNullOrEmpty(token))
                    return (false, "Missing Twilio Account SID or Auth Token.", null);
                var encoded = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{sid}:{token}"));
                http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encoded);
                var res = await http.GetAsync($"https://api.twilio.com/2010-04-01/Accounts/{sid}.json");
                if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return (false, "Twilio credentials are invalid.", null);
                return res.IsSuccessStatusCode
                    ? (true, "Twilio connected successfully.", sid)
                    : (false, $"Twilio returned HTTP {(int)res.StatusCode}.", null);
            }

            case "google-calendar":
            {
                var accessToken = creds.GetValueOrDefault("access_token") ?? string.Empty;
                if (string.IsNullOrEmpty(accessToken))
                    return (false, "No access token stored. Please reconnect.", null);
                http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var res = await http.GetAsync("https://www.googleapis.com/calendar/v3/users/me/calendarList?maxResults=1");
                if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return (false, "Access token expired. Please reconnect Google Calendar.", null);
                return res.IsSuccessStatusCode
                    ? (true, "Google Calendar connected successfully.", null)
                    : (false, $"Google Calendar returned HTTP {(int)res.StatusCode}.", null);
            }

            case "hubspot":
            {
                var key = creds.GetValueOrDefault("api_key") ?? string.Empty;
                if (string.IsNullOrEmpty(key)) return (false, "No HubSpot API key stored.", null);
                http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
                var res = await http.GetAsync("https://api.hubapi.com/crm/v3/objects/contacts?limit=1");
                if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return (false, "HubSpot API key is invalid.", null);
                return res.IsSuccessStatusCode
                    ? (true, "HubSpot connected successfully.", null)
                    : (false, $"HubSpot returned HTTP {(int)res.StatusCode}.", null);
            }

            case "slack":
            {
                var webhookUrl = creds.GetValueOrDefault("webhook_url") ?? string.Empty;
                if (string.IsNullOrEmpty(webhookUrl) || !webhookUrl.StartsWith("https://hooks.slack.com/"))
                    return (false, "Invalid Slack webhook URL.", null);
                var testPayload = JsonSerializer.Serialize(new { text = "Upkilo connected to Slack successfully." });
                var res = await http.PostAsync(webhookUrl,
                    new StringContent(testPayload, System.Text.Encoding.UTF8, "application/json"));
                return res.IsSuccessStatusCode
                    ? (true, "Slack connected successfully.", null)
                    : (false, "Slack webhook URL returned an error.", null);
            }

            default:
                return creds.Count > 0
                    ? (true, $"{integrationId} credentials are present.", existingAccountId)
                    : (false, "No credentials stored. Please reconnect.", null);
        }
    }

    private static string BuildLegacyCredsJson(TenantIntegration integration)
    {
        var dict = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(integration.AccessToken)) dict["access_token"] = integration.AccessToken;
        if (!string.IsNullOrEmpty(integration.RefreshToken)) dict["refresh_token"] = integration.RefreshToken;
        if (!string.IsNullOrEmpty(integration.ApiKey)) dict["api_key"] = integration.ApiKey;
        if (!string.IsNullOrEmpty(integration.Settings))
        {
            try
            {
                var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(integration.Settings);
                if (settings != null)
                    foreach (var kv in settings) dict.TryAdd(kv.Key, kv.Value);
            }
            catch { /* ignore */ }
        }
        return JsonSerializer.Serialize(dict);
    }

    private async Task WriteAuditAsync(Guid tenantId, string integrationId, string action, object? details = null)
    {
        _context.TenantIntegrationAudits.Add(new TenantIntegrationAudit
        {
            TenantId = tenantId,
            IntegrationId = integrationId,
            Action = action,
            ActorUserId = _tenantProvider.GetUserId()?.ToString(),
            ActorIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
            Details = details != null ? JsonSerializer.Serialize(details) : null,
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }
}

// ── DTOs ─────────────────────────────────────────────────────────────────────

public class ConnectIntegrationRequest
{
    public Dictionary<string, string>? Credentials { get; set; }
    // Legacy fields — accepted for backward compat
    public string? ApiKey { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public Dictionary<string, string>? Settings { get; set; }
}

public class UpdateIntegrationSettingsRequest
{
    public bool? Enabled { get; set; }
    public bool? SyncEnabled { get; set; }
    public string? SyncDirection { get; set; }
    public Dictionary<string, string>? CustomSettings { get; set; }
}

public class MigrationStartRequest
{
    public string SourcePlatform { get; set; } = string.Empty;
    public string[]? DataTypes { get; set; }
    public string? ApiKey { get; set; }
    public string? ExportFileUrl { get; set; }
}

public static class MigrationPlatforms
{
    public static readonly object[] Supported = new object[]
    {
        new { id = "mindbody",     name = "Mindbody",            category = "scheduling", dataTypes = new[] { "clients", "services", "bookings", "memberships", "staff" }, estimatedDays = 1 },
        new { id = "vagaro",       name = "Vagaro",              category = "scheduling", dataTypes = new[] { "clients", "services", "appointments", "products" },          estimatedDays = 1 },
        new { id = "square_appts", name = "Square Appointments", category = "scheduling", dataTypes = new[] { "clients", "services", "appointments", "staff" },             estimatedDays = 1 },
        new { id = "acuity",       name = "Acuity Scheduling",   category = "scheduling", dataTypes = new[] { "clients", "appointment_types", "bookings" },                 estimatedDays = 1 },
        new { id = "booker",       name = "Booker",              category = "spa",        dataTypes = new[] { "clients", "services", "appointments", "memberships" },        estimatedDays = 2 },
        new { id = "quickbooks",   name = "QuickBooks",          category = "accounting", dataTypes = new[] { "customers", "invoices", "payments" },                        estimatedDays = 1 },
        new { id = "xero",         name = "Xero",                category = "accounting", dataTypes = new[] { "contacts", "invoices", "payments" },                         estimatedDays = 1 },
        new { id = "csv",          name = "Generic CSV",         category = "universal",  dataTypes = new[] { "clients", "services", "bookings" },                          estimatedDays = 1 }
    };
}
