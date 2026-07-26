using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;
using Upkilo.Infrastructure.Services;
using Xunit;

namespace Upkilo.Tests.Services;

public class LocalizationServiceTests
{
    private readonly Mock<IStringLocalizerFactory> _factoryMock = new();
    private readonly Mock<IStringLocalizer> _localizerMock = new();

    public LocalizationServiceTests()
    {
        _factoryMock.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(_localizerMock.Object);
    }

    private LocalizationService CreateSut() => new LocalizationService(_factoryMock.Object);

    [Fact]
    public void GetSupportedLocales_Returns10Locales()
    {
        var sut = CreateSut();
        var locales = sut.GetSupportedLocales();
        locales.Should().HaveCount(10);
        locales.Should().Contain("en");
    }

    [Fact]
    public void IsSupported_KnownLocale_ReturnsTrue()
    {
        var sut = CreateSut();
        sut.IsSupported("en").Should().BeTrue();
        sut.IsSupported("fr").Should().BeTrue();
        sut.IsSupported("zh").Should().BeTrue();
    }

    [Fact]
    public void IsSupported_UnknownLocale_ReturnsFalse()
    {
        var sut = CreateSut();
        sut.IsSupported("xx").Should().BeFalse();
        sut.IsSupported("tlh").Should().BeFalse(); // Klingon not supported
    }

    [Fact]
    public void IsSupported_CaseInsensitive_ReturnsTrue()
    {
        var sut = CreateSut();
        sut.IsSupported("EN").Should().BeTrue();
        sut.IsSupported("Fr").Should().BeTrue();
    }

    [Fact]
    public void DetectLocale_NullHeader_ReturnsEn()
    {
        var sut = CreateSut();
        sut.DetectLocale(null).Should().Be("en");
    }

    [Fact]
    public void DetectLocale_EmptyHeader_ReturnsEn()
    {
        var sut = CreateSut();
        sut.DetectLocale("").Should().Be("en");
    }

    [Fact]
    public void DetectLocale_ValidAcceptLanguageHeader_ReturnsBestMatch()
    {
        var sut = CreateSut();
        var result = sut.DetectLocale("fr;q=0.9,en;q=0.8");
        result.Should().Be("fr");
    }

    [Fact]
    public void DetectLocale_UnsupportedLocaleOnly_FallsBackToEn()
    {
        var sut = CreateSut();
        var result = sut.DetectLocale("xx;q=1.0,yy;q=0.9");
        result.Should().Be("en"); // Neither supported, fallback
    }

    [Fact]
    public void DetectLocale_NormalizedFromRegionalCode()
    {
        var sut = CreateSut();
        // "en-US" normalizes to "en"
        var result = sut.DetectLocale("en-US,fr;q=0.8");
        result.Should().Be("en");
    }
}
