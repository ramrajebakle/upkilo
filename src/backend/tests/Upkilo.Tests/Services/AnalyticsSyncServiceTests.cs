using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;

namespace Upkilo.Tests.Services;

public class AnalyticsSyncServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory = new();
    private readonly Mock<ILogger<AnalyticsSyncService>> _loggerMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();

    public AnalyticsSyncServiceTests()
    {
        _cacheMock
            .Setup(c => c.GetOrSetAsync(It.IsAny<string>(), It.IsAny<Func<Task<string>>>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync("cached-value");
    }

    [Fact]
    public async Task SyncDataAsync_EmptyDb_CompletesWithoutThrow()
    {
        using var context = _dbFactory.CreateContext();
        var sut = new AnalyticsSyncService(context, _loggerMock.Object, _cacheMock.Object);

        var act = async () => await sut.SyncDataAsync();

        await act.Should().NotThrowAsync();
    }

    public void Dispose() => _dbFactory.Dispose();
}
