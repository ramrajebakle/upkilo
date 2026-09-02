using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Upkilo.Infrastructure.Services.Security;
using Xunit;

namespace Upkilo.Tests.Services;

/// <summary>
/// Production had no AzureContentSafety settings, and this service THREW from its constructor
/// in that case. It is a scoped dependency of AiService and AzureOpenAIService, which are in
/// turn injected widely, so DI resolution failed for every endpoint whose graph merely touched
/// them — the dashboard showed "Couldn't load this. Check your connection." across many pages
/// for the owner. Nothing caught it because App Service application logging was Off and
/// Application Insights was receiving nothing.
///
/// The requirement itself is legitimate, so it moved to the point of use: construction always
/// succeeds, and moderation REFUSES in Production when it cannot run.
/// </summary>
public class ContentModerationServiceTests
{
    private static ContentModerationService Build(string environment, bool configured)
    {
        var settings = new Dictionary<string, string?>();
        if (configured)
        {
            settings["AzureContentSafety:Endpoint"] = "https://example.cognitiveservices.azure.com/";
            settings["AzureContentSafety:ApiKey"] = "test-key";
        }

        var env = new Mock<IHostEnvironment>();
        env.SetupGet(e => e.EnvironmentName).Returns(environment);

        return new ContentModerationService(
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build(),
            NullLogger<ContentModerationService>.Instance,
            env.Object);
    }

    [Fact]
    public void Constructor_InProductionWithoutConfig_DoesNotThrow()
    {
        // The regression that took out a large part of the API.
        var act = () => Build("Production", configured: false);

        act.Should().NotThrow(
            "throwing here fails DI for every endpoint that transitively depends on this "
            + "service, including ones that never moderate anything");
    }

    [Fact]
    public async Task Moderation_InProductionWithoutConfig_RefusesRatherThanAllowing()
    {
        var sut = Build("Production", configured: false);

        var result = await sut.ModerateTextAsync("anything at all");

        // Fail CLOSED: an unavailable moderator is not evidence the text is safe. Before this,
        // the disabled path returned Allowed() and only the constructor throw stood between
        // production and unmoderated AI content.
        result.IsAllowed.Should().BeFalse();
        result.FlaggedCategories.Should().Contain(c => c.Category == "ModerationUnavailable");
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Staging")]
    public async Task Moderation_OutsideProductionWithoutConfig_StaysPermissive(string environment)
    {
        // Local and CI runs must not need an Azure Content Safety resource.
        var sut = Build(environment, configured: false);

        (await sut.ModerateTextAsync("anything at all")).IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task Moderation_EmptyText_IsAllowedEverywhere()
    {
        var sut = Build("Development", configured: false);

        (await sut.ModerateTextAsync("   ")).IsAllowed.Should().BeTrue();
    }
}
