using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;

namespace Upkilo.Tests.Services;

public class AdCampaignServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory = new();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();

    public AdCampaignServiceTests()
    {
        var platformMock = new Mock<IAdPlatformService>();
        platformMock
            .Setup(p => p.GetCampaignsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Enumerable.Empty<AdCampaignDto>());

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IAdPlatformService)))
            .Returns(platformMock.Object);
    }

    [Fact]
    public async Task GetActiveCampaignsAsync_NoCampaigns_ReturnsEmpty()
    {
        using var context = _dbFactory.CreateContext();
        var sut = new AdCampaignService(context, _serviceProviderMock.Object);

        var result = await sut.GetActiveCampaignsAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTotalAdSpendAsync_NoCampaigns_ReturnsZero()
    {
        using var context = _dbFactory.CreateContext();
        var sut = new AdCampaignService(context, _serviceProviderMock.Object);

        var result = await sut.GetTotalAdSpendAsync(Guid.NewGuid(), DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

        result.Should().Be(0m);
    }

    public void Dispose() => _dbFactory.Dispose();
}
