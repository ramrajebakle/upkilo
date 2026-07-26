using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Infrastructure.Services;
using Xunit;

namespace Upkilo.Tests.Services;

public class ThrottlingServiceTests
{
    private readonly Mock<ILogger<ThrottlingService>> _loggerMock = new();

    [Fact]
    public async Task IsThrottledAsync_FirstCall_ReturnsFalse()
    {
        var service = new ThrottlingService(_loggerMock.Object);
        var tenantId = Guid.NewGuid();

        var result = await service.IsThrottledAsync(tenantId, "email", 100);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsThrottledAsync_ExceedsLimit_ReturnsTrue()
    {
        var service = new ThrottlingService(_loggerMock.Object);
        var tenantId = Guid.NewGuid();
        const int limit = 5;

        bool throttled = false;
        for (int i = 0; i <= limit + 2; i++)
        {
            throttled = await service.IsThrottledAsync(tenantId, "sms-bulk", limit);
        }

        throttled.Should().BeTrue();
    }
}
