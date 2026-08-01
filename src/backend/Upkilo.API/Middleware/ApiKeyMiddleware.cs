using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Middleware;

/// <summary>
/// Validates external API keys (Format: upk_live_...) against the database.
/// If valid, sets the User ClaimsPrincipal and TenantId.
/// Tracks usage by logging each request to AuditEntry.
/// </summary>
public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyMiddleware> _logger;

    public ApiKeyMiddleware(RequestDelegate next, ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext, IAuditService auditService)
    {
        // 1. Extract API Key from headers
        string? apiKey = null;

        if (context.Request.Headers.TryGetValue("X-Api-Key", out var headerValue))
        {
            apiKey = headerValue.ToString();
        }
        else if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var authStr = authHeader.ToString();
            if (authStr.StartsWith("Bearer upk_", StringComparison.OrdinalIgnoreCase))
            {
                apiKey = authStr.Substring(11).Trim();
            }
        }

        if (string.IsNullOrEmpty(apiKey))
        {
            await _next(context);
            return;
        }

        // 2. Hash and validate key
        var keyHash = ComputeSha256Hash(apiKey);

        var keyRecord = await dbContext.ApiKeys
            .IgnoreQueryFilters() // Must check all keys
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash && !k.IsDeleted);

        bool isWithinGracePeriod = keyRecord?.GracePeriodExpiresAt.HasValue == true && keyRecord.GracePeriodExpiresAt > DateTime.UtcNow;

        if (keyRecord == null || (!keyRecord.IsActive && !isWithinGracePeriod))
        {
            _logger.LogWarning("Invalid API Key attempt: {KeyPrefix}...", apiKey.Length > 8 ? apiKey.Substring(0, 8) : "short");
            context.Response.StatusCode = 401; // Unauthorized
            await context.Response.WriteAsJsonAsync(new { error = "Invalid or expired API Key" });
            return;
        }

        // 3. Check expiration
        if (keyRecord.ExpiresAt.HasValue && keyRecord.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Expired API Key used: {KeyId}", keyRecord.Id);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "This API Key has expired" });
            return;
        }

        // H-12 FIX: Constant-time comparison check as an extra layer
        // Since DB query returned a record, we ensure the hash matches exactly in memory
        // using constant-time comparison to prevent timing side channels.
        if (!System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(keyHash),
                System.Text.Encoding.UTF8.GetBytes(keyRecord.KeyHash)))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API Key." });
            return;
        }

        // 4. Successful Auth: Set User context
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, keyRecord.Id.ToString()),
            new Claim("tenant_id", keyRecord.TenantId.ToString()),
            new Claim("auth_type", "ApiKey")
        };

        // Add scopes as permissions
        foreach (var scope in keyRecord.Scopes)
        {
            claims.Add(new Claim("scope", scope));
        }

        var identity = new ClaimsIdentity(claims, "ApiKey");
        context.User = new ClaimsPrincipal(identity);

        // Also set items for other middleware
        context.Items["TenantId"] = keyRecord.TenantId.ToString();
        context.Items["IsApiKeyRequest"] = true;

        // Update last used timestamp
        keyRecord.LastUsedAt = DateTime.UtcNow;

        // 5. Execute request with latency tracking
        var sw = Stopwatch.StartNew();
        await _next(context);
        sw.Stop();

        // 6. Log usage to AuditEntry for real-time tracking (Task 20)
        var endpoint = $"{context.Request.Method} {context.Request.Path}";
        var action = context.Response.StatusCode >= 400 ? "Error" : "ApiRequest";

        try
        {
            await auditService.LogAsync(
                keyRecord.TenantId,
                keyRecord.Id, // UserId = ApiKey.Id for API key requests
                "ApiKey",
                keyRecord.Id.ToString(),
                action,
                oldValues: null,
                newValues: new { endpoint, statusCode = context.Response.StatusCode, latencyMs = sw.ElapsedMilliseconds }
            );
        }
        catch (Exception ex)
        {
            // Don't fail the request if audit logging fails
            _logger.LogError(ex, "Failed to log API key usage for key {KeyId}", keyRecord.Id);
        }

        await dbContext.SaveChangesAsync();
    }

    private static string ComputeSha256Hash(string rawData)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
