using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class GdprServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<GdprService>> _loggerMock = new();

    public GdprServiceTests() => _dbFactory = new TestDbContextFactory();
    public void Dispose() => _dbFactory.Dispose();

    [Fact]
    public async Task RightToBeForgottenAsync_WhenUserExists_AnonymizesAndReturnsTrue()
    {
        // Arrange
        var ctx = _dbFactory.CreateContext();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "T1", Slug = "t1" };
        ctx.Tenants.Add(tenant);
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = "real@email.com",
            FirstName = "John",
            LastName = "Doe",
            PasswordHash = "hash123"
        };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        var sut = new GdprService(ctx, _loggerMock.Object);

        // Act
        var result = await sut.RightToBeForgottenAsync(user.Id);

        // Assert
        result.Should().BeTrue();
        ctx.ChangeTracker.Clear();
        var updated = ctx.Users.IgnoreQueryFilters().First(u => u.Id == user.Id);
        updated.Email.Should().Contain("deleted-");
        updated.FirstName.Should().Be("Deleted");
        updated.IsDeleted.Should().BeTrue();
        updated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task RightToBeForgottenAsync_WhenUserNotFound_ReturnsFalse()
    {
        var ctx = _dbFactory.CreateContext();
        var sut = new GdprService(ctx, _loggerMock.Object);

        var result = await sut.RightToBeForgottenAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExportUserDataAsync_WhenUserExists_ReturnsJsonWithUserData()
    {
        // Arrange
        var ctx = _dbFactory.CreateContext();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "T1", Slug = "t1" };
        ctx.Tenants.Add(tenant);
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = "export@test.com",
            FirstName = "Jane",
            LastName = "Smith"
        };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        var sut = new GdprService(ctx, _loggerMock.Object);

        // Act
        var json = await sut.ExportUserDataAsync(user.Id);

        // Assert
        json.Should().Contain("export@test.com");
        json.Should().Contain("Jane");
    }

    [Fact]
    public async Task ExportUserDataAsync_WhenUserNotFound_ReturnsEmptyJson()
    {
        var ctx = _dbFactory.CreateContext();
        var sut = new GdprService(ctx, _loggerMock.Object);

        var json = await sut.ExportUserDataAsync(Guid.NewGuid());

        json.Should().Be("{}");
    }
}
