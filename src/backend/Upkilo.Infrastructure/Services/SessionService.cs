using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using StackExchange.Redis;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Upkilo.Infrastructure.Services;

public class SessionService : ISessionService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SessionService> _logger;
    private readonly IDatabase _redis;
    private const string SessionKeyPrefix = "sess:";

    public SessionService(AppDbContext context, ILogger<SessionService> logger, IConnectionMultiplexer redis)
    {
        _context = context;
        _logger = logger;
        _redis = redis.GetDatabase();
    }

    public async Task<UserSession> CreateSessionAsync(Guid userId, Guid tenantId, string refreshToken, string? ipAddress, string? userAgent)
    {
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            RefreshToken = refreshToken,
            IpAddress = ipAddress,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            DeviceType = ParseDeviceType(userAgent),
            Browser = ParseBrowser(userAgent),
            OperatingSystem = ParseOS(userAgent)
        };

        _context.Set<UserSession>().Add(session);
        await _context.SaveChangesAsync();

        // Cache in Redis
        await _redis.StringSetAsync($"{SessionKeyPrefix}{session.Id}", JsonSerializer.Serialize(session), TimeSpan.FromDays(1));
        await _redis.StringSetAsync($"{SessionKeyPrefix}rt:{refreshToken}", session.Id.ToString(), TimeSpan.FromDays(30));

        _logger.LogInformation("Session created for user {UserId} from {IpAddress}", userId, ipAddress);
        return session;
    }

    public async Task<IEnumerable<UserSession>> GetActiveSessionsAsync(Guid userId)
    {
        return await _context.Set<UserSession>()
            .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastActiveAt)
            .ToListAsync();
    }

    public async Task<bool> RevokeSessionAsync(Guid sessionId, Guid userId)
    {
        var session = _context.Set<UserSession>()
            .FirstOrDefault(s => s.Id == sessionId && s.UserId == userId);
        
        if (session == null) return false;

        session.IsRevoked = true;
        await _context.SaveChangesAsync();

        // Clear cache
        await _redis.KeyDeleteAsync($"{SessionKeyPrefix}{sessionId}");
        await _redis.KeyDeleteAsync($"{SessionKeyPrefix}rt:{session.RefreshToken}");

        _logger.LogInformation("Session {SessionId} revoked for user {UserId}", sessionId, userId);
        return true;
    }

    public async Task<int> RevokeAllSessionsAsync(Guid userId, Guid? exceptSessionId = null)
    {
        var sessions = _context.Set<UserSession>()
            .Where(s => s.UserId == userId && !s.IsRevoked);

        if (exceptSessionId.HasValue)
            sessions = sessions.Where(s => s.Id != exceptSessionId.Value);

        var count = 0;
        foreach (var session in sessions.ToList())
        {
            session.IsRevoked = true;
            count++;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Revoked {Count} sessions for user {UserId}", count, userId);
        return count;
    }

    public async Task UpdateLastActiveAsync(Guid sessionId)
    {
        var session = _context.Set<UserSession>().Find(sessionId);
        if (session != null)
        {
            session.LastActiveAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<UserSession?> GetSessionByRefreshTokenAsync(string refreshToken)
    {
        // Check Redis for SessionId mapping
        var cachedId = await _redis.StringGetAsync($"{SessionKeyPrefix}rt:{refreshToken}");
        if (cachedId.HasValue && Guid.TryParse(cachedId, out var sessionId))
        {
            var cachedSession = await _redis.StringGetAsync($"{SessionKeyPrefix}{sessionId}");
            if (cachedSession.HasValue)
            {
                return JsonSerializer.Deserialize<UserSession>(cachedSession!);
            }
        }

        var session = await _context.Set<UserSession>()
            .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken && !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow);

        if (session != null)
        {
            // Update cache
            await _redis.StringSetAsync($"{SessionKeyPrefix}{session.Id}", JsonSerializer.Serialize(session), TimeSpan.FromMinutes(30));
        }

        return session;
    }

    private static string? ParseDeviceType(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return null;
        if (userAgent.Contains("Tablet") || userAgent.Contains("iPad")) return "tablet";
        if (userAgent.Contains("Mobile") || userAgent.Contains("iPhone")) return "mobile";
        return "desktop";
    }

    private static string? ParseBrowser(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return null;
        if (userAgent.Contains("Chrome")) return "Chrome";
        if (userAgent.Contains("Firefox")) return "Firefox";
        if (userAgent.Contains("Safari")) return "Safari";
        if (userAgent.Contains("Edge")) return "Edge";
        return "Other";
    }

    private static string? ParseOS(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return null;
        if (userAgent.Contains("Windows")) return "Windows";
        if (userAgent.Contains("iPhone") || userAgent.Contains("iPad")) return "iOS";
        if (userAgent.Contains("Linux")) return "Linux";
        if (userAgent.Contains("Android")) return "Android";
        if (userAgent.Contains("Mac")) return "macOS";
        return "Other";
    }
}
