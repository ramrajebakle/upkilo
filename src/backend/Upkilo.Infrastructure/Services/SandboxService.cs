using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Implementation of ISandboxService for developer environment management.
/// </summary>
public class SandboxService : ISandboxService
{
    private readonly AppDbContext _context;

    public SandboxService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SandboxEnvironment> CreateSandboxAsync(Guid userId, string? seedConfig = null)
    {
        var sandboxId = Guid.NewGuid();
        var sandbox = new SandboxEnvironment
        {
            Id = Guid.NewGuid(),
            TenantId = sandboxId, // Using a unique ID as the Sandbox TenantId
            SandboxId = sandboxId.ToString("N"),
            ApiKeyId = Guid.NewGuid(),
            SeedDataConfig = seedConfig,
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<SandboxEnvironment>().Add(sandbox);
        
        // 1. Find Template Tenant
        var templateTenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Slug == "template") ?? await _context.Tenants.FirstOrDefaultAsync();

        if (templateTenant != null)
        {
            await CloneTenantDataAsync(templateTenant.Id, sandboxId);
        }

        await _context.SaveChangesAsync();
        return sandbox;
    }

    public async Task<SandboxEnvironment> ResetSandboxAsync(string sandboxId)
    {
        var sandbox = await _context.Set<SandboxEnvironment>()
            .FirstOrDefaultAsync(s => s.SandboxId == sandboxId);

        if (sandbox == null)
            throw new ArgumentException("Sandbox not found", nameof(sandboxId));

        // 1. Wipe current sandbox data
        await WipeSandboxDataAsync(sandbox.TenantId);

        // 2. Re-clone from template
        var templateTenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Slug == "template") ?? await _context.Tenants.FirstOrDefaultAsync();

        if (templateTenant != null)
        {
            await CloneTenantDataAsync(templateTenant.Id, sandbox.TenantId);
        }

        sandbox.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return sandbox;
    }

    private async Task CloneTenantDataAsync(Guid sourceTenantId, Guid targetTenantId)
    {
        // Clone Locations
        var locations = await _context.Set<Location>()
            .Where(l => l.TenantId == sourceTenantId)
            .ToListAsync();
        foreach (var l in locations)
        {
            var newLoc = new Location { TenantId = targetTenantId, Name = l.Name, City = l.City, AddressLine1 = l.AddressLine1 };
            _context.Set<Location>().Add(newLoc);
        }

        // Clone Services
        var services = await _context.Services
            .Where(s => s.TenantId == sourceTenantId)
            .ToListAsync();
        var serviceMap = new Dictionary<Guid, Guid>();
        foreach (var s in services)
        {
            var newService = new Service 
            { 
                TenantId = targetTenantId, 
                Name = s.Name, 
                DurationMinutes = s.DurationMinutes, 
                Price = s.Price, 
                IsActive = true 
            };
            _context.Services.Add(newService);
            serviceMap[s.Id] = newService.Id;
        }

        // Clone Staff
        var staffList = await _context.StaffMembers
            .Where(s => s.TenantId == sourceTenantId)
            .ToListAsync();
        foreach (var st in staffList)
        {
            var newStaff = new StaffMember 
            { 
                TenantId = targetTenantId, 
                FirstName = st.FirstName, 
                LastName = st.LastName, 
                Email = "sandbox_" + st.Email, 
                IsActive = true 
            };
            _context.StaffMembers.Add(newStaff);
        }
    }

    private async Task WipeSandboxDataAsync(Guid tenantId)
    {
        // In a real production app, we would use a more efficient bulk delete or raw SQL
        var bookings = await _context.Bookings.Where(b => b.TenantId == tenantId).ToListAsync();
        _context.Bookings.RemoveRange(bookings);

        var services = await _context.Services.Where(s => s.TenantId == tenantId).ToListAsync();
        _context.Services.RemoveRange(services);

        var staff = await _context.StaffMembers.Where(s => s.TenantId == tenantId).ToListAsync();
        _context.StaffMembers.RemoveRange(staff);
        
        var locations = await _context.Set<Location>().Where(l => l.TenantId == tenantId).ToListAsync();
        _context.Set<Location>().RemoveRange(locations);
    }

    public async Task DeleteSandboxAsync(string sandboxId)
    {
        var sandbox = await _context.Set<SandboxEnvironment>()
            .FirstOrDefaultAsync(s => s.SandboxId == sandboxId);

        if (sandbox != null)
        {
            await WipeSandboxDataAsync(sandbox.TenantId);
            _context.Set<SandboxEnvironment>().Remove(sandbox);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> IsSandboxValidAsync(string sandboxId)
    {
        var sandbox = await _context.Set<SandboxEnvironment>()
            .FirstOrDefaultAsync(s => s.SandboxId == sandboxId);

        if (sandbox == null || !sandbox.IsActive)
            return false;

        if (sandbox.ExpiresAt.HasValue && sandbox.ExpiresAt < DateTime.UtcNow)
            return false;

        return true;
    }

    public async Task RecordAccessAsync(string sandboxId)
    {
        var sandbox = await _context.Set<SandboxEnvironment>()
            .FirstOrDefaultAsync(s => s.SandboxId == sandboxId);

        if (sandbox != null)
        {
            sandbox.LastAccessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}

