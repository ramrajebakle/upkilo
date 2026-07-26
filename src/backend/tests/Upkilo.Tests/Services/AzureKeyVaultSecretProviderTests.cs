using System;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Infrastructure.Services;
using Xunit;

namespace Upkilo.Tests.Services;

public class AzureKeyVaultSecretProviderTests
{
    private readonly Mock<ILogger<AzureKeyVaultSecretProvider>> _loggerMock = new();

    [Fact]
    public void GetSecret_NoVaultUri_ReturnsNullOrFallsBackToConfig()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string?>("Azure:KeyVault:Uri", ""),
                new System.Collections.Generic.KeyValuePair<string, string?>("SomeKey", "SomeValue"),
            })
            .Build();

        var provider = new AzureKeyVaultSecretProvider(config, _loggerMock.Object);

        // When vault not configured, provider falls back to config or returns null — no throw
        var result = provider.GetSecret("NonExistentKey");

        result.Should().BeNullOrEmpty();
    }

    [Fact]
    public void GetSecret_FromConfig_ReturnsValue()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string?>("Azure:KeyVault:Uri", ""),
                new System.Collections.Generic.KeyValuePair<string, string?>("MySecret", "my-secret-value"),
            })
            .Build();

        var provider = new AzureKeyVaultSecretProvider(config, _loggerMock.Object);

        var result = provider.GetSecret("MySecret");

        result.Should().Be("my-secret-value");
    }
}
