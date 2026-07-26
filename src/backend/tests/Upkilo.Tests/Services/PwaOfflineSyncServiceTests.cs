using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class PwaOfflineSyncServiceTests : IDisposable
{
    private readonly Mock<ILogger<PwaOfflineSyncService>> _loggerMock = new();
    private readonly TestDbContextFactory _dbFactory = new();

    private PwaOfflineSyncService CreateSut() =>
        new(_loggerMock.Object, _dbFactory.CreateContext());

    [Fact]
    public async Task ProcessOfflineQueueAsync_EmptyQueue_ReturnsZeroCounts()
    {
        var service = CreateSut();
        var tenantId = Guid.NewGuid();

        var result = await service.ProcessOfflineQueueAsync(tenantId, new List<OfflineMutation>());

        result.Should().NotBeNull();
        result.Resolved.Should().Be(0);
        result.Conflicts.Should().Be(0);
    }

    [Fact]
    public async Task ProcessOfflineQueueAsync_WithMutations_CompletesWithoutThrow()
    {
        var service = CreateSut();
        var tenantId = Guid.NewGuid();

        var mutations = new List<OfflineMutation>
        {
            new() { EntityId = Guid.NewGuid().ToString(), EntityType = "Booking",
                    ClientVersion = 2, ServerVersion = 1, PayloadJson = "{}" },
            new() { EntityId = Guid.NewGuid().ToString(), EntityType = "Client",
                    ClientVersion = 1, ServerVersion = 3, PayloadJson = "{}" }
        };

        var act = () => service.ProcessOfflineQueueAsync(tenantId, mutations);

        await act.Should().NotThrowAsync();
    }

    public void Dispose() => _dbFactory.Dispose();
}
