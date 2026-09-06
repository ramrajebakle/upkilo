using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

/// <summary>
/// SmsService must be configured from keys that exist, and must refuse when it is not.
///
/// Two defects met here and hid each other in production:
///
///   1. The sending number was read from "Twilio:FromNumber". That key was defined NOWHERE — not
///      appsettings.json, not deploy.yml, not .env.example, not App Service. The only occurrence
///      of the string in the repository was the line reading it, so the number was always "".
///
///   2. _isEnabled checked only AccountSid and AuthToken. Both ARE set in production, so the
///      service reported itself enabled and called Twilio with from: "". Every send failed at the
///      API, one message at a time, rather than the service saying it was not set up.
///
/// Between them, SMS was silently broken for booking reminders, campaigns, broadcasts, birthday
/// campaigns and review requests — while looking configured.
/// </summary>
public class SmsServiceConfigTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory = new();
    private readonly Mock<ILogger<SmsService>> _logger = new();
    private readonly Mock<ISecretProvider> _secrets = new();

    public SmsServiceConfigTests() =>
        _secrets.Setup(s => s.GetSecret(It.IsAny<string>())).Returns((string?)null);

    private SmsService Build(params (string Key, string Value)[] settings)
    {
        var dict = new Dictionary<string, string?>();
        foreach (var (k, v) in settings) dict[k] = v;

        var config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
        return new SmsService(config, _logger.Object, _dbFactory.CreateContext(), _secrets.Object);
    }

    private static (string, string) Sid => ("Twilio:AccountSid", "AC_test_sid");
    private static (string, string) Token => ("Twilio:AuthToken", "test_token");

    [Fact]
    public async Task CredentialsButNoSendingNumber_IsDisabledRatherThanFailingAtTheApi()
    {
        // Exactly production's state: SID and token present, no number.
        var sut = Build(Sid, Token);

        var result = await sut.SendSmsAsync(Guid.NewGuid(), "+15551234567", "Your appointment is tomorrow.");

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("disabled",
            "a missing sending number must disable the service, not let it call Twilio with from: \"\"");
    }

    [Fact]
    public async Task DisabledForAMissingNumber_SaysWhichSettingIsMissing()
    {
        var sut = Build(Sid, Token);

        await sut.SendSmsAsync(Guid.NewGuid(), "+15551234567", "test");

        // "Missing Twilio credentials" sent whoever read it to check the SID and token, which
        // were both fine. The log has to name the part that is actually absent.
        _logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Twilio:PhoneNumber")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task MissingCredentials_SaysSoRatherThanBlamingTheNumber()
    {
        var sut = Build(("Twilio:PhoneNumber", "+15559999999"));

        await sut.SendSmsAsync(Guid.NewGuid(), "+15551234567", "test");

        _logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Twilio:AccountSid")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ReadsTheSendingNumberFromTwilioPhoneNumber()
    {
        // The key that actually exists in appsettings.json and deploy.yml. With it present the
        // service considers itself configured, so the failure is no longer "disabled" — it gets
        // as far as attempting a real send.
        var sut = Build(Sid, Token, ("Twilio:PhoneNumber", "+15559999999"));

        var result = await sut.SendSmsAsync(Guid.NewGuid(), "+15551234567", "test");

        result.Error.Should().NotBe("SMS Service is disabled",
            "Twilio:PhoneNumber is a valid sending number, so the service is configured");
    }

    [Fact]
    public async Task StillHonoursTwilioFromNumberWhereSomeoneHadSetIt()
    {
        // Backwards compatibility: the old key wins if an environment somehow defines it, so this
        // change cannot take SMS away from anyone it was working for.
        var sut = Build(Sid, Token, ("Twilio:FromNumber", "+15558888888"));

        var result = await sut.SendSmsAsync(Guid.NewGuid(), "+15551234567", "test");

        result.Error.Should().NotBe("SMS Service is disabled");
    }

    public void Dispose() => _dbFactory.Dispose();
}
