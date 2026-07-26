using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Upkilo.Infrastructure.Data;
using Upkilo.Core.Entities;

namespace Upkilo.Infrastructure.Services;

public interface IRolePermissionService
{
    Task<bool> HasPermissionAsync(Guid userId, string permission);
    Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId);
}

public class RolePermissionService : IRolePermissionService
{
    private readonly AppDbContext _context;

    public RolePermissionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string permission)
    {
        var permissions = await GetUserPermissionsAsync(userId);
        return permissions.Contains(permission) || permissions.Contains("*");
    }

    public async Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return Enumerable.Empty<string>();

        // For now, return a basic set based on Role name if permissions are not explicitly stored
        // In a full implementation, we would query a RolePermissions junction table
        if (user.Role == UserRole.Owner) return new[] { "*" };
        if (user.Role == UserRole.Admin) return new[] { "read", "write", "delete" };
        
        return new[] { "read" };
    }
}
