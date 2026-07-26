using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// User management controller for admin operations
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize(Roles = "Owner,Admin")]
public class UserManagementController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<UserManagementController> _logger;

    public UserManagementController(
        AppDbContext context,
        ITenantProvider tenantProvider,
        ILogger<UserManagementController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get all users in tenant
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var users = await _context.Users
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.FirstName)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                Role = u.Role.ToString(),
                IsActive = u.Status == UserStatus.Active,
                u.LastLoginAt,
                u.CreatedAt
            })
            .ToListAsync();

        return Ok(new { data = users });
    }

    /// <summary>
    /// Deactivate a user
    /// </summary>
    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> DeactivateUser(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);

        if (user == null) return NotFound();

        // Can't deactivate owner
        if (user.Role == UserRole.Owner)
            return BadRequest("Cannot deactivate the account owner");

        user.Status = UserStatus.Inactive;
        user.UpdatedAt = DateTime.UtcNow;

        // Log the activity
        await LogActivityAsync(id, UserActivityType.RoleChange, "User deactivated");

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} deactivated", id);

        return Ok(new { success = true, message = "User deactivated" });
    }

    /// <summary>
    /// Reactivate a user
    /// </summary>
    [HttpPost("{id}/activate")]
    public async Task<IActionResult> ActivateUser(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);

        if (user == null) return NotFound();

        user.Status = UserStatus.Active;
        user.UpdatedAt = DateTime.UtcNow;

        await LogActivityAsync(id, UserActivityType.RoleChange, "User reactivated");

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} reactivated", id);

        return Ok(new { success = true, message = "User reactivated" });
    }

    /// <summary>
    /// Get user activity logs
    /// </summary>
    [HttpGet("{id}/activity")]
    public async Task<IActionResult> GetUserActivity(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.Set<UserActivityLog>()
            .Where(a => a.UserId == id && a.TenantId == tenantId);

        var total = await query.CountAsync();
        var activities = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.ActivityType,
                a.Description,
                a.IpAddress,
                a.ResourceType,
                a.ResourceId,
                a.CreatedAt
            })
            .ToListAsync();

        return Ok(new { data = activities, total, page, pageSize });
    }

    /// <summary>
    /// Get login history for a user
    /// </summary>
    [HttpGet("{id}/login-history")]
    public async Task<IActionResult> GetLoginHistory(Guid id, [FromQuery] int limit = 20)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var logins = await _context.Set<UserActivityLog>()
            .Where(a => a.UserId == id &&
                        a.TenantId == tenantId &&
                        (a.ActivityType == UserActivityType.Login || a.ActivityType == UserActivityType.Logout))
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .Select(a => new
            {
                Type = a.ActivityType.ToString(),
                a.IpAddress,
                a.CreatedAt
            })
            .ToListAsync();

        return Ok(new { data = logins });
    }

    /// <summary>
    /// Download CSV template for bulk user import
    /// </summary>
    [HttpGet("import-template")]
    public IActionResult GetImportTemplate()
    {
        var csv = "Email,FirstName,LastName,Role,Phone\n" +
                  "john@example.com,John,Doe,Staff,555-1234\n" +
                  "jane@example.com,Jane,Smith,Admin,555-5678";

        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", "user_import_template.csv");
    }

    /// <summary>
    /// Bulk import users from CSV
    /// </summary>
    [HttpPost("import")]
    public async Task<IActionResult> ImportUsers([FromForm] IFormFile file)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only CSV files are allowed");

        var results = new List<object>();
        var successCount = 0;
        var failCount = 0;

        using (var reader = new StreamReader(file.OpenReadStream()))
        {
            // Skip header
            await reader.ReadLineAsync();

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var values = line.Split(',');
                if (values.Length < 3)
                {
                    results.Add(new { line, status = "failed", error = "Invalid CSV format" });
                    failCount++;
                    continue;
                }

                var email = values[0].Trim();
                var firstName = values[1].Trim();
                var lastName = values[2].Trim();
                var role = values.Length > 3 ? values[3].Trim() : "Staff";
                var phone = values.Length > 4 ? values[4].Trim() : null;

                // Validate email
                if (string.IsNullOrEmpty(email) || !email.Contains("@"))
                {
                    results.Add(new { email, status = "failed", error = "Invalid email" });
                    failCount++;
                    continue;
                }

                // Check if user already exists
                var exists = await _context.Users.AnyAsync(u =>
                    u.Email.ToLower() == email.ToLower() && u.TenantId == tenantId);

                if (exists)
                {
                    results.Add(new { email, status = "failed", error = "User already exists" });
                    failCount++;
                    continue;
                }

                // Parse role
                if (!Enum.TryParse<UserRole>(role, true, out var userRole))
                {
                    userRole = UserRole.Staff; // Default to staff
                }

                try
                {
                    var user = new User
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId.Value,
                        Email = email,
                        FirstName = firstName,
                        LastName = lastName,
                        Role = userRole,
                        Phone = phone,
                        Status = UserStatus.Pending,
                        EmailVerified = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Users.Add(user);
                    results.Add(new { email, status = "success", userId = user.Id });
                    successCount++;
                }
                catch (Exception ex)
                {
                    results.Add(new { email, status = "failed", error = ex.Message });
                    failCount++;
                }
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Bulk import completed: {SuccessCount} succeeded, {FailCount} failed",
            successCount, failCount);

        return Ok(new
        {
            success = true,
            message = $"Import completed: {successCount} succeeded, {failCount} failed",
            successCount,
            failCount,
            results
        });
    }

    private async Task LogActivityAsync(Guid userId, UserActivityType type, string description)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return;

        var log = new UserActivityLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            UserId = userId,
            ActivityType = type,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<UserActivityLog>().Add(log);
    }
}
