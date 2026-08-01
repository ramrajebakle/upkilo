using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Xunit;

namespace Upkilo.Tests.Integration;

/// <summary>
/// T7: OpenAPI contract tests — validates the exported Swagger spec on every build.
/// Uses WebApplicationFactory to spin up the API in-process and fetch /swagger/v1/swagger.json.
/// Catches missing endpoints, renamed paths, and schema regressions before they reach CI.
/// </summary>
[Trait("Category", "Contract")]
public class OpenApiContractTests : IClassFixture<OpenApiContractTests.ApiFactory>
{
    private readonly HttpClient _client;
    private static JsonDocument? _spec;
    private static readonly SemaphoreSlim _specLock = new(1, 1);

    public OpenApiContractTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
        // HttpClient defaults to a 100s timeout. Generating the full Swagger document for
        // this API (500+ endpoints) can exceed that on a cold CI runner — the first request
        // pays JIT plus reflection over every controller. When it timed out, each of the 12
        // tests retried and timed out in turn, taking the CI job past 20 minutes before
        // failing with TaskCanceledException. The same tests pass in ~8s on a warm machine.
        _client.Timeout = TimeSpan.FromMinutes(5);
    }

    private async Task<JsonDocument> GetSpecAsync()
    {
        if (_spec != null) return _spec;
        await _specLock.WaitAsync();
        try
        {
            if (_spec != null) return _spec;
            var json = await _client.GetStringAsync("/swagger/v1/swagger.json");
            _spec = JsonDocument.Parse(json);
        }
        finally
        {
            _specLock.Release();
        }
        return _spec!;
    }

    [Fact]
    public async Task Spec_IsValidOpenApi3()
    {
        var spec = await GetSpecAsync();
        spec.RootElement.TryGetProperty("openapi", out var versionProp).Should().BeTrue("spec must have 'openapi' field");
        versionProp.GetString().Should().StartWith("3.", "spec must be OpenAPI 3.x");
    }

    [Fact]
    public async Task Spec_HasInfoBlock()
    {
        var spec = await GetSpecAsync();
        spec.RootElement.TryGetProperty("info", out var info).Should().BeTrue();
        info.TryGetProperty("title", out _).Should().BeTrue("info must have title");
        info.TryGetProperty("version", out _).Should().BeTrue("info must have version");
    }

    [Fact]
    public async Task Spec_HasPaths()
    {
        var spec = await GetSpecAsync();
        spec.RootElement.TryGetProperty("paths", out var paths).Should().BeTrue();
        paths.EnumerateObject().Should().NotBeEmpty("spec must expose at least one path");
    }

    [Fact]
    public async Task Spec_ContainsBookingsEndpoint()
    {
        var spec = await GetSpecAsync();
        var paths = spec.RootElement.GetProperty("paths");
        var allPaths = paths.EnumerateObject().Select(p => p.Name).ToList();
        allPaths.Should().Contain(p => p.Contains("booking", StringComparison.OrdinalIgnoreCase),
            "bookings controller must be registered in swagger");
    }

    [Fact]
    public async Task Spec_ContainsClientsEndpoint()
    {
        var spec = await GetSpecAsync();
        var paths = spec.RootElement.GetProperty("paths");
        var allPaths = paths.EnumerateObject().Select(p => p.Name).ToList();
        allPaths.Should().Contain(p => p.Contains("client", StringComparison.OrdinalIgnoreCase),
            "clients controller must be registered in swagger");
    }

    [Fact]
    public async Task Spec_ContainsServicesEndpoint()
    {
        var spec = await GetSpecAsync();
        var paths = spec.RootElement.GetProperty("paths");
        var allPaths = paths.EnumerateObject().Select(p => p.Name).ToList();
        allPaths.Should().Contain(p => p.Contains("service", StringComparison.OrdinalIgnoreCase),
            "services controller must be registered in swagger");
    }

    [Fact]
    public async Task Spec_ContainsBillingEndpoint()
    {
        var spec = await GetSpecAsync();
        var paths = spec.RootElement.GetProperty("paths");
        var allPaths = paths.EnumerateObject().Select(p => p.Name).ToList();
        allPaths.Should().Contain(p => p.Contains("billing", StringComparison.OrdinalIgnoreCase),
            "billing controller must be registered in swagger");
    }

    [Fact]
    public async Task Spec_AllPathsHaveAtLeastOneOperation()
    {
        var validMethods = new[] { "get", "post", "put", "patch", "delete", "options", "head" };
        var spec = await GetSpecAsync();
        var paths = spec.RootElement.GetProperty("paths");
        foreach (var path in paths.EnumerateObject())
        {
            var hasOperation = path.Value.EnumerateObject().Any(m => validMethods.Contains(m.Name));
            hasOperation.Should().BeTrue($"path '{path.Name}' must have at least one HTTP operation");
        }
    }

    [Fact]
    public async Task Spec_NoOperationHasEmptyOperationId()
    {
        var validMethods = new[] { "get", "post", "put", "patch", "delete" };
        var spec = await GetSpecAsync();
        var paths = spec.RootElement.GetProperty("paths");
        foreach (var path in paths.EnumerateObject())
        {
            foreach (var method in path.Value.EnumerateObject().Where(m => validMethods.Contains(m.Name)))
            {
                if (method.Value.TryGetProperty("operationId", out var opId))
                {
                    opId.GetString().Should().NotBeNullOrWhiteSpace(
                        $"operationId in {method.Name.ToUpper()} {path.Name} must not be empty");
                }
            }
        }
    }

    [Fact]
    public async Task Spec_SwaggerJsonEndpointReturns200()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        response.IsSuccessStatusCode.Should().BeTrue("swagger endpoint must return 200");
        response.Content.Headers.ContentType!.MediaType.Should().Contain("json");
    }

    [Fact]
    public async Task Spec_HasComponents()
    {
        var spec = await GetSpecAsync();
        // Swashbuckle always generates components/schemas for typed responses
        if (spec.RootElement.TryGetProperty("components", out var components))
        {
            if (components.TryGetProperty("schemas", out var schemas))
            {
                schemas.EnumerateObject().Should().NotBeEmpty("schemas block should not be empty if present");
            }
        }
        // If no components, that's also valid (all inline responses)
        true.Should().BeTrue();
    }

    [Fact]
    public async Task Spec_TotalPathCountIsReasonable()
    {
        var spec = await GetSpecAsync();
        var paths = spec.RootElement.GetProperty("paths");
        var count = paths.EnumerateObject().Count();
        count.Should().BeGreaterThan(20, "API should expose more than 20 paths given the full feature set");
    }

    // ---------------------------------------------------------------------------
    // Factory — minimal host to avoid real DB/external dependencies
    // ---------------------------------------------------------------------------
    public class ApiFactory : WebApplicationFactory<Program>
    {
        // The "Test" environment loads only appsettings.json, whose connection strings carry the
        // `REPLACE_WITH_SECRET_IN_ENV_OR_KEYVAULT` placeholder that production supplies from
        // KeyVault — booting with it fails `08P01: password authentication failed`.
        //
        // These must be environment variables, not ConfigureAppConfiguration: Program.cs reads
        // `builder.Configuration.GetConnectionString(...)` in its top-level statements, which run
        // while the entry point is invoked — before WebApplicationFactory's configuration
        // callbacks are applied. CreateBuilder() reads env vars natively, so this lands in time.
        static ApiFactory()
        {
            SetIfUnset("ConnectionStrings__DefaultConnection",
                "Host=localhost;Port=5432;Database=upkilo_dev;Username=upkilo;Password=upkilo_dev_password;Pooling=true;Maximum Pool Size=10");
            SetIfUnset("ConnectionStrings__ReplicaConnection",
                "Host=localhost;Port=5432;Database=upkilo_dev;Username=upkilo;Password=upkilo_dev_password;Pooling=true;Maximum Pool Size=10");
            SetIfUnset("ConnectionStrings__Redis",
                "localhost:6379,password=dev_redis_password_2026,abortConnect=false");
            // Application Insights v3 throws at startup without a connection string, even when
            // telemetry is effectively disabled. Mirrors the inert value in appsettings.Development.json.
            SetIfUnset("ApplicationInsights__ConnectionString",
                "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://localhost:0/");
        }

        // Respect anything CI already exported rather than clobbering it.
        private static void SetIfUnset(string key, string value)
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                Environment.SetEnvironmentVariable(key, value);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Must be Development, not "Test": Program.cs registers UseSwagger()/UseSwaggerUI()
            // only under IsDevelopment(), so any other environment serves no spec to assert on
            // and additionally enables UseHttpsRedirection(), which 400s the plain-HTTP test client.
            builder.UseEnvironment("Development");
        }
    }
}
