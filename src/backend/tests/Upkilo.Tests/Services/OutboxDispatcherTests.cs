using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class OutboxDispatcherTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory = new();
    private readonly Mock<ILogger<OutboxDispatcher>> _loggerMock = new();

    [Fact]
    public async Task ProcessPendingAsync_NoMessages_CompletesWithoutThrow()
    {
        await using var context = _dbFactory.CreateContext();

        var services = new ServiceCollection();
        services.AddSingleton(context);
        var sp = services.BuildServiceProvider();

        var dispatcher = new OutboxDispatcher(sp, _loggerMock.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var act = async () => await dispatcher.StartAsync(cts.Token);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task OutboxDispatcher_CanBeConstructed_WithServiceProvider()
    {
        var spMock = new Mock<IServiceProvider>();
        var scopeMock = new Mock<IServiceScope>();
        var scopedSpMock = new Mock<IServiceProvider>();
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();

        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);
        scopeMock.Setup(s => s.ServiceProvider).Returns(scopedSpMock.Object);
        spMock.Setup(s => s.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactoryMock.Object);

        var dispatcher = new OutboxDispatcher(spMock.Object, _loggerMock.Object);
        dispatcher.Should().NotBeNull();
    }

    public void Dispose() => _dbFactory.Dispose();
}
