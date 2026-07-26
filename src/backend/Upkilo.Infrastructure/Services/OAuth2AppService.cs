using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Manages OAuth2 app registration for third-party developer access.
/// Handles client_id/client_secret generation, authorization codes,
/// token issuance/refresh, and scope enforcement.
/// </summary>
public class OAuth2AppService
{
    private readonly AppDbContext _context;
    private readonly ILogger<OAuth2AppService> _logger;

    public OAuth2AppService(AppDbContext context, ILogger<OAuth2AppService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Register a new OAuth2 application (developer portal).
    /// </summary>
    public async Task<(OAuthApp App, string PlainSecret)> RegisterAppAsync(
        Guid tenantId, string appName, string? description, string[] redirectUris, string[] scopes,
        string? websiteUrl = null, string? privacyPolicyUrl = null)
    {
        var plainSecret = GenerateClientSecret();
        var app = new OAuthApp
        {
            TenantId = tenantId,
            AppName = appName,
            ClientId = Guid.NewGuid().ToString("N"),
            ClientSecretHash = HashSecret(plainSecret),
            Description = description,
            RedirectUris = System.Text.Json.JsonSerializer.Serialize(redirectUris),
            Scopes = System.Text.Json.JsonSerializer.Serialize(scopes),
            WebsiteUrl = websiteUrl,
            PrivacyPolicyUrl = privacyPolicyUrl,
            IsApproved = false,
            IsActive = true
        };

        _context.Set<OAuthApp>().Add(app);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Registered OAuth2 app: {AppName} (ClientId: {ClientId})", appName, app.ClientId);

        return (app, plainSecret);
    }

    /// <summary>
    /// Validate client credentials (client_id + client_secret).
    /// </summary>
    public async Task<OAuthApp?> ValidateClientAsync(string clientId, string clientSecret)
    {
        var app = await _context.Set<OAuthApp>()
            .FirstOrDefaultAsync(a => a.ClientId == clientId && a.IsActive);

        if (app == null) return null;

        if (!VerifySecret(clientSecret, app.ClientSecretHash))
        {
            _logger.LogWarning("Invalid client secret for app {ClientId}", clientId);
            return null;
        }

        app.LastUsedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return app;
    }

    /// <summary>
    /// Issue an authorization code for the authorization code flow.
    /// </summary>
    public async Task<string> IssueAuthorizationCodeAsync(
        Guid tenantId, Guid oauthAppId, Guid userId, string[] scopes)
    {
        var code = GenerateAuthorizationCode();

        var token = new OAuthToken
        {
            TenantId = tenantId,
            OAuthAppId = oauthAppId,
            UserId = userId,
            AuthorizationCode = code,
            AccessTokenHash = string.Empty,
            Scopes = System.Text.Json.JsonSerializer.Serialize(scopes),
            ExpiresAt = DateTime.UtcNow.AddMinutes(10) // Auth codes expire in 10 min
        };

        _context.Set<OAuthToken>().Add(token);
        await _context.SaveChangesAsync();

        return code;
    }

    /// <summary>
    /// Exchange authorization code for access + refresh token.
    /// </summary>
    public async Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)?> ExchangeCodeAsync(
        string authorizationCode, string clientId)
    {
        var tokenRecord = await _context.Set<OAuthToken>()
            .Include(t => t.OAuthApp)
            .FirstOrDefaultAsync(t =>
                t.AuthorizationCode == authorizationCode &&
                t.OAuthApp != null && t.OAuthApp.ClientId == clientId &&
                t.ExpiresAt > DateTime.UtcNow &&
                t.RevokedAt == null);

        if (tokenRecord == null) return null;

        var accessToken = GenerateToken();
        var refreshToken = GenerateToken();
        var expiresAt = DateTime.UtcNow.AddHours(1);

        tokenRecord.AccessTokenHash = HashSecret(accessToken);
        tokenRecord.RefreshTokenHash = HashSecret(refreshToken);
        tokenRecord.AuthorizationCode = null; // Invalidate code
        tokenRecord.ExpiresAt = expiresAt;

        await _context.SaveChangesAsync();

        return (accessToken, refreshToken, expiresAt);
    }

    /// <summary>
    /// Revoke an OAuth2 token.
    /// </summary>
    public async Task RevokeTokenAsync(Guid tokenId)
    {
        var token = await _context.Set<OAuthToken>().FindAsync(tokenId);
        if (token != null)
        {
            token.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Revoked OAuth2 token {TokenId}", tokenId);
        }
    }

    /// <summary>
    /// List all apps registered by a tenant.
    /// </summary>
    public async Task<List<OAuthApp>> ListAppsAsync(Guid tenantId)
    {
        return await _context.Set<OAuthApp>()
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    // --- Crypto helpers ---

    private static string GenerateClientSecret() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static string GenerateAuthorizationCode() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)).Replace("+", "-").Replace("/", "_");

    private static string GenerateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    private static string HashSecret(string secret)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(secret));
        return Convert.ToBase64String(hash);
    }

    private static bool VerifySecret(string plainSecret, string storedHash) =>
        HashSecret(plainSecret) == storedHash;
}
