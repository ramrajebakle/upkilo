using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;

using Xunit;

namespace Upkilo.Tests.Services;

public class FileServiceTests
{
    private readonly Mock<ILogger<FileService>> _loggerMock = new();
    private readonly Mock<ISecretProvider> _secretProviderMock = new();
    private readonly IConfiguration _configuration;

    // Azurite local emulator connection string (safe for unit tests — no real I/O attempted at construction)
    private const string FakeConnectionString =
        "DefaultEndpointsProtocol=https;AccountName=devstoreaccount1;" +
        "AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;" +
        "BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;";

    public FileServiceTests()
    {
        _configuration = new ConfigurationBuilder().Build();
        _secretProviderMock
            .Setup(s => s.GetSecret("Azure--Storage--ConnectionString"))
            .Returns(FakeConnectionString);
        _secretProviderMock
            .Setup(s => s.GetSecret(It.IsNotIn("Azure--Storage--ConnectionString")))
            .Returns((string?)null);
    }

    [Fact]
    public void Instantiation_WithConnectionString_DoesNotThrow()
    {
        var act = () => new FileService(_configuration, _secretProviderMock.Object, _loggerMock.Object);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task UploadAsync_InvalidContentType_ThrowsInvalidOperationException()
    {
        var service = new FileService(_configuration, _secretProviderMock.Object, _loggerMock.Object);
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var act = async () => await service.UploadAsync(stream, "test.exe", "application/octet-stream", Guid.NewGuid(), Upkilo.Core.Interfaces.FileCategory.Exports);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
