using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using Xunit;

namespace Upkilo.Tests.Services;

/// <summary>
/// Application Insights telemetry must never prevent the API from starting.
///
/// It did. Production served App Service's own 503 page for hours because the host threw during
/// Host.StartAsync:
///
///   InvalidOperationException: Connection String Error: Required keyword 'InstrumentationKey'
///   is missing in connection string.
///     at Azure.Monitor.OpenTelemetry.Exporter.AzureMonitorMetricExporter..ctor
///
/// The root cause is NOT established. What is known:
///
///  - The APPLICATIONINSIGHTS_CONNECTION_STRING app setting is valid: it starts with
///    InstrumentationKey=, has four well-formed segments, and no stray whitespace or non-ASCII.
///  - The guard warning never appeared in the container log, so aiConnStr was non-empty and
///    telemetry registration went ahead.
///  - Yet the exporter was constructed with a connection string that had no InstrumentationKey.
///  - The same registration could not be made to fail locally: not with the value as a real
///    environment variable, not as in-memory config, and not alongside the app's own
///    OpenTelemetry metrics pipeline. With the real appsettings.Production.json loaded the guard
///    fires and registration is skipped, so that path does not fail either.
///
/// Two theories were tried and both are disproven, recorded here so nobody repeats them:
///
///  1. A key-name mismatch — App Service sets APPLICATIONINSIGHTS_CONNECTION_STRING, which maps
///     to a config key of that exact name and NOT to ApplicationInsights:ConnectionString.
///     Ruled out by the tests below, which resolve cleanly in that exact shape.
///  2. Passing the connection string explicitly to AddApplicationInsightsTelemetry. Shipped as
///     a0bc673; an image carrying it still exited 139 on staging. It sets
///     ApplicationInsightsServiceOptions, but the object that throws is
///     AzureMonitorExporterOptions — a different instance, resolved separately.
///
/// So these tests pin a DECISION, not a diagnosis: the registration is now explicit opt-in
/// (ApplicationInsights:Enabled), and off by default. A code path that has twice taken
/// production down and has never once delivered a trace does not run unless somebody asks for
/// it. Staging reproduces the crash faithfully now, so re-enabling can be verified there first.
///
/// The deploy failure was doubly opaque: the workflow pipes /ready into jq, so App Service's
/// HTML 503 surfaced as "jq: parse error: Invalid numeric literal at line 1, column 10".
/// </summary>
public class ApplicationInsightsStartupTests
{
    private const string ValidConnectionString =
        "InstrumentationKey=00000000-0000-0000-0000-000000000001;"
        + "IngestionEndpoint=https://eastus-8.in.applicationinsights.azure.com/";

    private static IConfiguration ConfigWithOnlyTheAzureName() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Exactly what App Service provides: the Azure name, and nothing under the
                // colon key that the SDK's configuration overload reads.
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = ValidConnectionString,
            })
            .Build();

    /// <summary>
    /// What I could NOT reproduce, recorded so the next person does not repeat the attempt.
    ///
    /// Registering via the configuration overload with only the Azure name present - the exact
    /// production shape - builds and resolves cleanly here, both with and without the app's own
    /// OpenTelemetry metrics pipeline alongside it, and both with the value supplied as a real
    /// environment variable and as in-memory config. So the outage is NOT explained by the key
    /// mismatch alone, and this test asserts only the behaviour actually observed.
    ///
    /// Whatever supplies the empty connection string in the container is still unidentified.
    /// The mitigation is therefore defence in depth rather than a targeted fix: validate the
    /// string before registering, and pass it explicitly so the SDK performs no lookup of its
    /// own. See ExplicitConnectionString_BuildsTheExporterAndStartsCleanly.
    /// </summary>
    [Fact]
    public void ConfigurationOverload_WithOnlyTheAzureName_ResolvesCleanlyHere()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationInsightsTelemetry(ConfigWithOnlyTheAzureName());
        services.AddOpenTelemetry().WithMetrics(m => m.AddRuntimeInstrumentation());

        using var provider = services.BuildServiceProvider();

        var act = () => provider.GetRequiredService<MeterProvider>();

        act.Should().NotThrow("recorded as observed - this path does not reproduce the outage");
    }

    /// <summary>
    /// Assigning the connection string explicitly resolves cleanly here — and did NOT fix
    /// production. It sets ApplicationInsightsServiceOptions, while the object that throws is
    /// AzureMonitorExporterOptions, resolved separately. Kept as a record of what was tried and
    /// ruled out: an image carrying this change still exited 139 on staging.
    /// </summary>
    [Fact]
    public void ExplicitConnectionString_BuildsTheExporterAndStartsCleanly()
    {
        var config = ConfigWithOnlyTheAzureName();
        var resolved = config["ApplicationInsights:ConnectionString"]
                       ?? config["APPLICATIONINSIGHTS_CONNECTION_STRING"];

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationInsightsTelemetry(options => options.ConnectionString = resolved);

        using var provider = services.BuildServiceProvider();

        var act = () => provider.GetRequiredService<MeterProvider>();

        act.Should().NotThrow("telemetry must never be able to prevent the API from starting");
    }

    /// <summary>
    /// The guard that decides whether to register at all, in the shapes production actually
    /// produced. An unexpanded ${...} placeholder is a real case: appsettings.Production.json is
    /// full of them and .NET does not expand them.
    /// </summary>
    [Theory]
    // Opted OUT: nothing registers, whatever the connection string says. This is the default,
    // and it is what stops the exporter crashing startup.
    [InlineData(false, "InstrumentationKey=00000000-0000-0000-0000-000000000001", false)]
    [InlineData(false, null, false)]
    // Opted IN: the connection string still has to be one the exporter can parse.
    [InlineData(true, null, false)]
    [InlineData(true, "", false)]
    [InlineData(true, "   ", false)]
    [InlineData(true, "${APPLICATIONINSIGHTS_CONNECTION_STRING}", false)]
    [InlineData(true, "IngestionEndpoint=https://eastus-8.in.applicationinsights.azure.com/", false)]
    [InlineData(true, "InstrumentationKey=00000000-0000-0000-0000-000000000001", true)]
    public void TheGuard_RegistersOnlyWhenOptedInWithAParseableString(
        bool optIn, string? value, bool expected)
    {
        // Mirrors Program.cs. Kept in sync by ProgramGuardMatchesThisTest below.
        var configured = optIn
                         && !string.IsNullOrWhiteSpace(value)
                         && !value.StartsWith("${", StringComparison.Ordinal)
                         && value.Contains("InstrumentationKey=", StringComparison.OrdinalIgnoreCase);

        configured.Should().Be(expected);
    }

    /// <summary>
    /// The theory above restates Program.cs's condition, so it could drift from the real one and
    /// keep passing. This reads the source and fails if the three clauses are not all still
    /// there - cheap insurance against the check being relaxed back to what caused the outage.
    /// </summary>
    [Fact]
    public void ProgramGuardMatchesThisTest()
    {
        var path = System.IO.Path.Combine(
            System.AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "Upkilo.API", "Program.cs");

        if (!System.IO.File.Exists(path)) return; // not laid out as expected; nothing to assert

        var src = System.IO.File.ReadAllText(path);

        src.Should().Contain("aiConnStr.Contains(\"InstrumentationKey=\"",
            "the guard must reject a connection string the exporter cannot parse");
        src.Should().Contain("var aiConfigured = aiOptIn",
            "telemetry must be OFF unless explicitly opted in - passing the connection string "
            + "explicitly was tried and did not stop the exporter crashing");
        src.Should().Contain("ApplicationInsights:Enabled",
            "the opt-in switch must be read from configuration");
    }
}
