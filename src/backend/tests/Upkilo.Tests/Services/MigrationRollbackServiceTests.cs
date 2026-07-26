using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;

namespace Upkilo.Tests.Services;

public class MigrationRollbackServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory = new();
    private readonly Mock<ILogger<MigrationRollbackService>> _loggerMock = new();

    [Fact]
    public async Task GetAppliedMigrationsAsync_ReturnsMigrationList()
    {
        using var context = _dbFactory.CreateContext();
        var sut = new MigrationRollbackService(context, _loggerMock.Object);

        // SQLite in-memory will have applied migrations from EnsureCreated
        var result = await sut.GetAppliedMigrationsAsync();

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPendingMigrationsAsync_ReturnsList()
    {
        using var context = _dbFactory.CreateContext();
        var sut = new MigrationRollbackService(context, _loggerMock.Object);

        var act = async () => await sut.GetPendingMigrationsAsync();

        // The call should complete without throwing
        await act.Should().NotThrowAsync();
    }

    public void Dispose() => _dbFactory.Dispose();
}
