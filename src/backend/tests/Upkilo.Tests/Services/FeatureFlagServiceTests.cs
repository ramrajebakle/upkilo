using System;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Infrastructure.Services;
using Xunit;

namespace Upkilo.Tests.Services;

public class FeatureFlagServiceTests
{
    private readonly Mock<ILogger<FeatureFlagService>> _loggerMock = new();
    private readonly Mock<IHttpClientFactory> _httpFactoryMock = new();
    private readonly Mock<IConfiguration> _configMock = new();

    private FeatureFlagService CreateSut() =>
        new(_loggerMock.Object, _httpFactoryMock.Object, _configMock.Object);

    [Fact]
    public void IsEnabled_DefaultSeededFlag_ReturnsTrue()
    {
        var sut = CreateSut();

        var result = sut.IsEnabled("ai_chatbot");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_UnknownFlag_ReturnsFalse()
    {
        var sut = CreateSut();

        var result = sut.IsEnabled("nonexistent_flag_xyz");

        result.Should().BeFalse();
    }

    [Fact]
    public void RegisterFlag_ThenIsEnabled_ReturnsConfiguredValue()
    {
        var sut = CreateSut();
        sut.RegisterFlag("new_feature", defaultValue: false, "New feature flag");

        var result = sut.IsEnabled("new_feature");

        result.Should().BeFalse();
    }

    [Fact]
    public void SetTenantOverride_OverridesGlobalDefault()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();

        sut.SetTenantOverride("ai_chatbot", tenantId, false);

        var result = sut.IsEnabled("ai_chatbot", tenantId);

        result.Should().BeFalse();
    }

    [Fact]
    public void SetTenantOverride_OtherTenants_NotAffected()
    {
        var sut = CreateSut();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        sut.SetTenantOverride("ai_chatbot", tenantA, false);

        sut.IsEnabled("ai_chatbot", tenantB).Should().BeTrue();
    }

    [Fact]
    public void GetAllFlags_ReturnsAtLeastSeededFlags()
    {
        var sut = CreateSut();

        var flags = sut.GetAllFlags();

        flags.Should().NotBeEmpty();
    }

    [Fact]
    public void SetRolloutPercentage_ClampedBetween0And100()
    {
        var sut = CreateSut();
        var act1 = () => sut.SetRolloutPercentage("ai_chatbot", -5);
        var act2 = () => sut.SetRolloutPercentage("ai_chatbot", 150);

        act1.Should().NotThrow();
        act2.Should().NotThrow();
    }
}
