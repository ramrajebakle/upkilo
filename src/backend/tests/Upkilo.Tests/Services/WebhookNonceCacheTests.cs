using System;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Infrastructure.Services;
using Xunit;

namespace Upkilo.Tests.Services;

public class WebhookNonceCacheTests
{
    private readonly Mock<ILogger<WebhookNonceCache>> _loggerMock = new();
    private WebhookNonceCache CreateSut() => new WebhookNonceCache(_loggerMock.Object);

    [Fact]
    public void ValidateNonce_FirstTime_ReturnsTrue()
    {
        var sut = CreateSut();
        var result = sut.ValidateNonce("nonce-abc-123");
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateNonce_SameNonceTwice_SecondReturnsFalse()
    {
        var sut = CreateSut();
        sut.ValidateNonce("replay-nonce");

        var result = sut.ValidateNonce("replay-nonce");

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateNonce_DifferentNonces_BothReturnTrue()
    {
        var sut = CreateSut();

        var r1 = sut.ValidateNonce("nonce-1");
        var r2 = sut.ValidateNonce("nonce-2");

        r1.Should().BeTrue();
        r2.Should().BeTrue();
    }

    [Fact]
    public void GenerateNonce_ReturnsDifferentValuesEachTime()
    {
        var sut = CreateSut();

        var n1 = sut.GenerateNonce();
        var n2 = sut.GenerateNonce();

        n1.Should().NotBe(n2);
    }

    [Fact]
    public void GenerateNonce_ReturnedNonce_ContainsTimestamp()
    {
        var sut = CreateSut();
        var nonce = sut.GenerateNonce();

        // Format: "{guid}_{timestamp}"
        nonce.Should().Contain("_");
    }

    [Fact]
    public void CleanupExpired_RemovesExpiredNonces()
    {
        var sut = CreateSut();

        // Use reflection to add an expired nonce and modify _lastCleanup
        var noncesField = typeof(WebhookNonceCache).GetField("_nonces", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var nonces = (System.Collections.Concurrent.ConcurrentDictionary<string, DateTime>)noncesField!.GetValue(sut)!;
        nonces!.TryAdd("expired-nonce", DateTime.UtcNow.AddMinutes(-10));

        var lastCleanupField = typeof(WebhookNonceCache).GetField("_lastCleanup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        lastCleanupField!.SetValue(sut, DateTime.UtcNow.AddMinutes(-5));

        // Act - should trigger cleanup
        var result = sut.ValidateNonce("new-nonce");

        // Assert
        result.Should().BeTrue();
        nonces.ContainsKey("expired-nonce").Should().BeFalse();
        nonces.ContainsKey("new-nonce").Should().BeTrue();
    }
}
