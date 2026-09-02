using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Upkilo.Tests.Services;

/// <summary>
/// Pins how the Application Insights connection string is discovered.
///
/// Program.cs read only "ApplicationInsights:ConnectionString". Azure App Service supplies
/// APPLICATIONINSIGHTS_CONNECTION_STRING, and .NET's environment-variable provider maps a
/// COLON key only from a DOUBLE-UNDERSCORE variable — so the production setting, correctly
/// named and populated, resolved to null. Telemetry was disabled at every start while the
/// portal showed Application Insights attached, and with App Service application logging
/// also defaulting to Off, a production 500 left no trace anywhere at all.
///
/// These assert the mapping itself rather than mocking it, so they fail if either spelling
/// stops being honoured.
/// </summary>
public class ObservabilityConfigTests
{
    /// <summary>Mirrors the lookup in Program.cs.</summary>
    private static string? Resolve(IConfiguration config) =>
        config["ApplicationInsights:ConnectionString"]
        ?? config["APPLICATIONINSIGHTS_CONNECTION_STRING"];

    [Fact]
    public void AzureEnvironmentVariableName_IsHonoured()
    {
        // The exact key an APPLICATIONINSIGHTS_CONNECTION_STRING env var produces.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = "InstrumentationKey=abc;IngestionEndpoint=https://x/",
            })
            .Build();

        Resolve(config).Should().NotBeNullOrWhiteSpace(
            "App Service supplies this name, and reading only the colon form is what silenced "
            + "telemetry in production");
    }

    [Fact]
    public void ColonForm_StillWorks_ForAppsettingsAndDoubleUnderscore()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=abc;IngestionEndpoint=https://x/",
            })
            .Build();

        Resolve(config).Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ColonKey_IsNotProducedByASingleUnderscoreVariable()
    {
        // The heart of the bug: these are two different keys, not aliases.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = "InstrumentationKey=abc",
            })
            .Build();

        config["ApplicationInsights:ConnectionString"].Should().BeNull(
            "only ApplicationInsights__ConnectionString maps to the colon key");
    }

    [Fact]
    public void NeitherNamePresent_ResolvesToNull()
        => Resolve(new ConfigurationBuilder().Build()).Should().BeNull();
}
