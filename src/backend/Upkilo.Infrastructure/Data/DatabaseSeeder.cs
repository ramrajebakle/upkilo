using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;

namespace Upkilo.Infrastructure.Data;

/// <summary>
/// Seeds essential data on application startup.
/// Only inserts data if tables are empty (idempotent).
/// Note: Pricing plans are seeded by PricingSeeder (PricingPlan entity), not here.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await SeedDefaultRolesAsync(context);
    }

    private static async Task SeedDefaultRolesAsync(AppDbContext context)
    {
        // Custom roles are usually per-tenant, but we can seed system-level templates if needed
        // For now, we seed a global "Super Admin" role template if it doesn't exist
        var superAdminRoleExists = await context.Set<CustomRole>().AnyAsync(r => r.Name == "Super Admin");
        if (!superAdminRoleExists)
        {
            context.Set<CustomRole>().Add(new CustomRole
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.Empty, // System level template
                Name = "Super Admin",
                Description = "System administrator with full access",
                Permissions = new Dictionary<string, bool> { { "*", true } },
                IsSystem = true,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }
    }
}
