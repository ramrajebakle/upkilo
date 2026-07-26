using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class RolePermissionServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;

    public RolePermissionServiceTests()
    {
        _dbFactory = new TestDbContextFactory();
    }

    [Fact]
    public void Constructor_ValidArgs_CreatesInstance()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new RolePermissionService(ctx);
        svc.Should().NotBeNull();
    }

    [Fact]
    public async Task HasPermissionAsync_UserWithNoRolesOrUnknownUser_ReturnsFalse()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new RolePermissionService(ctx);

        var result = await svc.HasPermissionAsync(Guid.NewGuid(), "write");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserPermissionsAsync_UnknownUser_ReturnsEmpty()
    {
        using var ctx = _dbFactory.CreateContext();
        var svc = new RolePermissionService(ctx);

        var result = await svc.GetUserPermissionsAsync(Guid.NewGuid());

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task HasPermissionAsync_OwnerUser_ReturnsTrue()
    {
        using var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "Test", Slug = "test-perm" });
        ctx.Users.Add(new User
        {
            Id = userId,
            TenantId = tenantId,
            Email = "owner@example.com",
            Role = UserRole.Owner,
            FirstName = "Owner",
            LastName = "User"
        });
        ctx.SaveChanges();

        var svc = new RolePermissionService(ctx);
        var result = await svc.HasPermissionAsync(userId, "any_permission");

        result.Should().BeTrue();
    }

    public void Dispose() => _dbFactory.Dispose();
}
