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

public class BufferedAuditLogServiceTests
{
    [Fact]
    public void Log_EnqueuesEntry_WithoutThrowing()
    {
        var loggerMock = new Mock<ILogger<BufferedAuditLogService>>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        var sut = new BufferedAuditLogService(serviceProviderMock.Object, loggerMock.Object);

        var act = () => sut.Log(new Upkilo.Core.Entities.AuditEntry
        {
            TenantId = Guid.NewGuid(),
            EntityType = "Booking",
            EntityId = "1",
            Action = "Create",
            Timestamp = DateTime.UtcNow
        });

        act.Should().NotThrow();
    }

    [Fact]
    public void Log_MultipleEntries_DoesNotThrow()
    {
        var loggerMock = new Mock<ILogger<BufferedAuditLogService>>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        var sut = new BufferedAuditLogService(serviceProviderMock.Object, loggerMock.Object);

        for (int i = 0; i < 200; i++)
        {
            sut.Log(new Upkilo.Core.Entities.AuditEntry
            {
                TenantId = Guid.NewGuid(),
                EntityType = "Entity",
                EntityId = i.ToString(),
                Action = "Update",
                Timestamp = DateTime.UtcNow
            });
        }
        // If we got here, 200 entries were queued without issue
    }
}
