using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

/// <summary>
/// SendGrid account-level click tracking rewrites every link in an outgoing message to the
/// link-branding host, url9658.upkilo.com. That host is a CNAME to sendgrid.net, which serves
/// a *.sendgrid.net certificate that does not cover it, so Chrome aborts the navigation with
/// ERR_CERT_COMMON_NAME_INVALID — and because upkilo.com sends HSTS for its subdomains, the
/// user cannot click through the interstitial. Every verification and password-reset link was
/// a dead end, so nobody could complete registration.
///
/// Account-security mail must therefore opt out of link rewriting per message, so the link
/// reaches the user exactly as written and does not depend on that host being healthy.
/// These tests assert the flag on the actual JSON handed to the SendGrid API.
/// </summary>
public class EmailClickTrackingTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private string? _lastPayload;

    public EmailClickTrackingTests() => _dbFactory = new TestDbContextFactory();
    public void Dispose() => _dbFactory.Dispose();

    private EmailService CreateSut()
    {
        // Captures the request body SendGrid would have received, and answers 202 so the
        // service treats the send as successful.
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage req, CancellationToken _) =>
            {
                _lastPayload = req.Content is null ? null : await req.Content.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.Accepted)
                {
                    Content = new StringContent(string.Empty)
                };
            });

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>()))
               .Returns(() => new HttpClient(handler.Object));

        var secrets = new Mock<ISecretProvider>();
        secrets.Setup(s => s.GetSecretAsync("SendGrid:ApiKey"))
               .ReturnsAsync("SG.a-real-looking-key-for-tests");

        var env = new Mock<IHostEnvironment>();
        env.SetupGet(e => e.EnvironmentName).Returns("Development");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:FromEmail"] = "noreply@upkilo.com",
                ["Email:FromName"] = "Upkilo",
                ["App:FrontendUrl"] = "https://app.upkilo.com"
            })
            .Build();

        return new EmailService(
            config,
            NullLogger<EmailService>.Instance,
            _dbFactory.CreateContext(),
            secrets.Object,
            factory.Object,
            env.Object);
    }

    /// <summary>Reads tracking_settings.click_tracking.enable out of the sent payload.</summary>
    private bool? ClickTrackingFlagInLastSend()
    {
        _lastPayload.Should().NotBeNull("the service must actually have called SendGrid");
        using var doc = JsonDocument.Parse(_lastPayload!);
        if (!doc.RootElement.TryGetProperty("tracking_settings", out var tracking)) return null;
        if (!tracking.TryGetProperty("click_tracking", out var click)) return null;
        if (!click.TryGetProperty("enable", out var enable)) return null;
        return enable.GetBoolean();
    }

    [Fact]
    public async Task SecurityEmail_DisablesClickTracking()
    {
        await CreateSut().SendSecurityEmailAsync(
            "user@example.com", "Verify Your Email - Upkilo",
            "<a href='https://app.upkilo.com/verify-email?token=abc'>Verify</a>");

        ClickTrackingFlagInLastSend().Should().BeFalse(
            "a rewritten link points at a host whose certificate does not cover it, and HSTS "
            + "stops the user clicking through");
    }

    [Fact]
    public async Task EmailVerification_DisablesClickTracking()
    {
        await CreateSut().SendEmailVerificationAsync("user@example.com", "tok-123");

        ClickTrackingFlagInLastSend().Should().BeFalse();
    }

    [Fact]
    public async Task PasswordReset_DisablesClickTracking()
    {
        await CreateSut().SendPasswordResetAsync("user@example.com", "tok-456");

        ClickTrackingFlagInLastSend().Should().BeFalse();
    }

    /// <summary>
    /// Campaign mail goes through SendSystemEmailAsync and legitimately wants click
    /// analytics, so the opt-out must NOT leak into it.
    /// </summary>
    [Fact]
    public async Task SystemEmail_LeavesClickTrackingToTheAccountDefault()
    {
        await CreateSut().SendSystemEmailAsync(
            "user@example.com", "Monthly newsletter", "<a href='https://upkilo.com'>Read</a>");

        ClickTrackingFlagInLastSend().Should().BeNull(
            "no per-message override means the SendGrid account setting still applies");
    }

    [Fact]
    public async Task SecurityEmail_SendsTheLinkVerbatim()
    {
        await CreateSut().SendEmailVerificationAsync("user@example.com", "tok-789");

        _lastPayload.Should().Contain("app.upkilo.com/verify-email?token=tok-789");
    }
}
