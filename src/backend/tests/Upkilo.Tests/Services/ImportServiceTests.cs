using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Hangfire;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class ImportServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory = new();
    private readonly Mock<ILogger<ImportService>> _loggerMock = new();
    private readonly Mock<IBackgroundJobClient> _jobClientMock = new();

    public ImportServiceTests()
    {
        _jobClientMock.Setup(j => j.Create(It.IsAny<Hangfire.Common.Job>(), It.IsAny<Hangfire.States.IState>()))
                      .Returns("job-id");
    }

    [Fact]
    public async Task AnalyzeImportAsync_ValidCsvStream_ReturnsAnalysis()
    {
        using var context = _dbFactory.CreateContext();
        var service = new ImportService(context, _loggerMock.Object, _jobClientMock.Object);

        var csv = "FirstName,LastName,Email\nJohn,Doe,john@example.com\nJane,Doe,jane@example.com";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await service.AnalyzeImportAsync(stream, "clients");

        result.Should().NotBeNull();
        result.EstimatedRows.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetJobHistoryAsync_NoImports_ReturnsEmpty()
    {
        using var context = _dbFactory.CreateContext();
        var service = new ImportService(context, _loggerMock.Object, _jobClientMock.Object);

        var history = await service.GetJobHistoryAsync(Guid.NewGuid());

        history.Should().NotBeNull();
        history.Should().BeEmpty();
    }

    public void Dispose() => _dbFactory.Dispose();
}
