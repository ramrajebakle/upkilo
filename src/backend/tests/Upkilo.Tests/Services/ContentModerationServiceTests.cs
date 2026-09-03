using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Upkilo.Core.Interfaces;
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

        // Returns null so the service falls back to IConfiguration, which is the live behaviour
        // today: no Key Vault is provisioned, so ISecretProvider has nothing to hand back.
        var secrets = new Mock<ISecretProvider>();
        secrets.Setup(s => s.GetSecret(It.IsAny<string>())).Returns((string?)null);

        return new ContentModerationService(
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build(),
            NullLogger<ContentModerationService>.Instance,
            env.Object,
            secrets.Object);
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

        // The reason carries a suffix naming which path could not produce a verdict
        // ("NotConfigured" here), so logs and callers can tell "this text was harmful" apart
        // from "we could not check it" — very different operationally, even though both deny.
        result.FlaggedCategories.Should()
            .Contain(c => c.Category.StartsWith("ModerationUnavailable"));
    }

    /// <summary>
    /// The dangerous asymmetry this class did NOT cover before.
    ///
    /// An unconfigured service refused in Production, but a null response or ANY exception
    /// returned Allowed() — the code said "Fail open to avoid blocking legitimate content". So
    /// a misconfiguration blocked everything while a genuine outage, or anything that could
    /// induce an exception, allowed everything: the protection was strongest exactly when
    /// nothing was wrong, and absent when something was.
    ///
    /// A bad endpoint makes the SDK call fail, which is the exception path.
    /// </summary>
    [Fact]
    public async Task Moderation_InProductionWhenTheCallFails_RefusesRatherThanAllowing()
    {
        var settings = new Dictionary<string, string?>
        {
            // Routable-looking but non-resolving, so AnalyzeTextAsync throws rather than 200s.
            ["AzureContentSafety:Endpoint"] = "https://upkilo-invalid-host.invalid/",
            ["AzureContentSafety:ApiKey"] = "test-key",
        };

        var env = new Mock<IHostEnvironment>();
        env.SetupGet(e => e.EnvironmentName).Returns("Production");
        var secrets = new Mock<ISecretProvider>();
        secrets.Setup(s => s.GetSecret(It.IsAny<string>())).Returns((string?)null);

        var sut = new ContentModerationService(
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build(),
            NullLogger<ContentModerationService>.Instance,
            env.Object,
            secrets.Object);

        var result = await sut.ModerateTextAsync("anything at all");

        result.IsAllowed.Should().BeFalse(
            "an outage is not evidence the text is safe; failing open here meant any induced "
            + "error was a complete bypass of moderation");
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
