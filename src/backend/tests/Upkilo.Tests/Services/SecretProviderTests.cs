using System;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Infrastructure.Services;
using Xunit;

namespace Upkilo.Tests.Services;

public class SecretProviderTests
{
    private readonly Mock<ILogger<SecretProvider>> _loggerMock = new();

    [Fact]
    public void GetSecret_ExistingConfigKey_ReturnsValue()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string?>("MyKey", "MyValue"),
            })
            .Build();

        var provider = new SecretProvider(config, _loggerMock.Object);

        var result = provider.GetSecret("MyKey");

        result.Should().Be("MyValue");
    }

    [Fact]
    public void GetSecret_UnknownKey_ReturnsNull()
    {
        var config = new ConfigurationBuilder().Build();
        var provider = new SecretProvider(config, _loggerMock.Object);

        var result = provider.GetSecret("KeyThatDoesNotExist_XYZ");

        result.Should().BeNullOrEmpty();
    }
}
