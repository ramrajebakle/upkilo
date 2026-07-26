using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Upkilo.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace Upkilo.Infrastructure.Services;

public interface IGdprService
{
    Task<bool> RightToBeForgottenAsync(Guid userId);
    Task<string> ExportUserDataAsync(Guid userId);
}

public class GdprService : IGdprService
{
    private readonly AppDbContext _context;
    private readonly ILogger<GdprService> _logger;

    public GdprService(AppDbContext context, ILogger<GdprService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> RightToBeForgottenAsync(Guid userId)
    {
        _logger.LogWarning("GDPR: Processing Right to be Forgotten for User {UserId}.", userId);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return false;

        // Anonymize user data instead of hard delete to preserve financial records integrity
        user.Email = $"deleted-{Guid.NewGuid()}@upkilo.com";
        user.FirstName = "Deleted";
        user.LastName = "User";
        user.PasswordHash = "ERASED";
        user.IsActive = false;
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<string> ExportUserDataAsync(Guid userId)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return "{}";

        return System.Text.Json.JsonSerializer.Serialize(user, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }
}
