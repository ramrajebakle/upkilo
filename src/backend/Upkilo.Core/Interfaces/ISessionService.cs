using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

public interface ISessionService
{
    Task<UserSession> CreateSessionAsync(Guid userId, Guid tenantId, string refreshToken, string? ipAddress, string? userAgent);
    Task<IEnumerable<UserSession>> GetActiveSessionsAsync(Guid userId);
    Task<bool> RevokeSessionAsync(Guid sessionId, Guid userId);
    Task<int> RevokeAllSessionsAsync(Guid userId, Guid? exceptSessionId = null);
    Task UpdateLastActiveAsync(Guid sessionId);
    Task<UserSession?> GetSessionByRefreshTokenAsync(string refreshToken);
}
