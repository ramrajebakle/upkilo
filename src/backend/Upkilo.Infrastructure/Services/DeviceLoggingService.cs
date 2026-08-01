using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Service for tracking device and IP history for security auditing and risk assessment.
/// </summary>
public class DeviceLoggingService
{
    private readonly AppDbContext _context;
    private readonly ILogger<DeviceLoggingService> _logger;

    public DeviceLoggingService(AppDbContext context, ILogger<DeviceLoggingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogDeviceAccessAsync(Guid userId, string ipAddress, string userAgent)
    {
        var hash = GenerateDeviceHash(ipAddress, userAgent);

        var existing = await _context.Set<UserDevice>()
            .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceHash == hash);

        if (existing == null)
        {
            var device = new UserDevice
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                DeviceHash = hash,
                FirstSeenAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow,
                IsTrusted = false
            };
            _context.Set<UserDevice>().Add(device);
            _logger.LogInformation("New device detected for user {UserId}: {IpAddress}", userId, ipAddress);
        }
        else
        {
            existing.LastSeenAt = DateTime.UtcNow;
            existing.IpAddress = ipAddress; // Update case IP changed for same user agent (common in mobile)
        }

        await _context.SaveChangesAsync();
    }

    private string GenerateDeviceHash(string ip, string ua)
    {
        // Simple hash for identification (In production use SHA256 of combined details)
        return $"{ip}_{ua}".GetHashCode().ToString("X");
    }
}

public class UserDevice : TenantEntity
{
    public Guid UserId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string DeviceHash { get; set; } = string.Empty;
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public bool IsTrusted { get; set; }
    public string? Location { get; set; }
}
