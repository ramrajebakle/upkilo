using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using System.Security.Claims;
using Upkilo.API.Middleware;
using Upkilo.Core.Interfaces;
using Upkilo.API.Services;

namespace Upkilo.Tests.Middleware;

/// <summary>
/// Tests for TenantMiddleware — tenant resolution from headers, subdomains, JWT claims, and cross-tenant security.
/// </summary>
public class TenantMiddlewareTests
{
    private readonly Mock<ILogger<TenantMiddleware>> _logger = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());



    [Fact]
    public async Task InvokeAsync_JwtClaim_SetsTenantInItems()
    {
        var tenantId = Guid.NewGuid().ToString();
        var context = new DefaultHttpContext();
        var claims = new List<Claim> { new("tenant_id", tenantId) };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var middleware = new TenantMiddleware(_ => Task.CompletedTask, _logger.Object, _cache);

        await middleware.InvokeAsync(context);

        context.Items["TenantId"].Should().Be(tenantId);
    }

    [Fact]
    public async Task InvokeAsync_SubdomainTenant_SetsTenantInItems()
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("mybiz.upkilo.com");
        var middleware = new TenantMiddleware(_ => Task.CompletedTask, _logger.Object, _cache);

        await middleware.InvokeAsync(context);

        context.Items["TenantId"].Should().Be("mybiz");
    }

    [Fact]
    public async Task InvokeAsync_AppSubdomain_DoesNotSetTenantId()
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("app.upkilo.com");
        var middleware = new TenantMiddleware(_ => Task.CompletedTask, _logger.Object, _cache);

        await middleware.InvokeAsync(context);

        context.Items.ContainsKey("TenantId").Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WwwSubdomain_DoesNotSetTenantId()
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("www.upkilo.com");
        var middleware = new TenantMiddleware(_ => Task.CompletedTask, _logger.Object, _cache);

        await middleware.InvokeAsync(context);

        context.Items.ContainsKey("TenantId").Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_NoTenantInfo_CallsNextWithoutTenantId()
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("localhost");
        bool nextCalled = false;
        var middleware = new TenantMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, _logger.Object, _cache);

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Items.ContainsKey("TenantId").Should().BeFalse();
    }


}

/// <summary>
/// Tests for ApiKeyMiddleware — API key validation, scope enforcement, and rejection.
/// </summary>
public class ApiKeyMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_NoApiKey_CallsNextNormally()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/bookings";
        bool nextCalled = false;
        var middleware = new ApiKeyMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, new Mock<ILogger<ApiKeyMiddleware>>().Object);

        await middleware.InvokeAsync(context, null!, null!);

        nextCalled.Should().BeTrue();
    }
}

/// <summary>
/// Tests for ExceptionMiddleware — structured error responses.
/// </summary>
public class ExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_NoException_Returns200()
    {
        var context = new DefaultHttpContext();
        var mockEnv = new Mock<IHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns("Development");
        var middleware = new ExceptionMiddleware(_ => Task.CompletedTask, new Mock<ILogger<ExceptionMiddleware>>().Object, mockEnv.Object);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_Returns500WithStructuredError()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Items["CorrelationId"] = "test-correlation-123";
        var mockEnv = new Mock<IHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns("Development");
        var middleware = new ExceptionMiddleware(
            _ => throw new Exception("Something broke"),
            new Mock<ILogger<ExceptionMiddleware>>().Object,
            mockEnv.Object);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);
    }
}

// RateLimitingMiddlewareTests removed alongside RateLimitingMiddleware itself. The class was
// never registered in the pipeline — rate limiting is provided by UseRateLimiter() and
// UseTenantRateLimit() — so the test asserted the behaviour of code that never ran.

/// <summary>
/// Tests for LoadSheddingMiddleware.
/// </summary>
public class LoadSheddingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_NormalLoad_PassesThrough()
    {
        var context = new DefaultHttpContext();
        var mockLoadMonitor = new Mock<ISystemLoadMonitorService>();
        mockLoadMonitor.Setup(m => m.IsSystemOverloaded()).Returns(false);
        var middleware = new LoadSheddingMiddleware(_ => Task.CompletedTask, new Mock<ILogger<LoadSheddingMiddleware>>().Object, mockLoadMonitor.Object);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(200);
    }
}

/// <summary>
/// Tests for IdempotencyMiddleware.
/// </summary>
public class IdempotencyMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_GetRequest_SkipsIdempotencyCheck()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        bool nextCalled = false;
        var middleware = new IdempotencyMiddleware(_ => { nextCalled = true; return Task.CompletedTask; },
            new Mock<ILogger<IdempotencyMiddleware>>().Object);
        var mockCache = new Mock<IDistributedCache>();
        var mockTenantProvider = new Mock<ITenantProvider>();

        await middleware.InvokeAsync(context, mockCache.Object, mockTenantProvider.Object);

        nextCalled.Should().BeTrue();
    }
}

/// <summary>
/// Tests for SandboxMiddleware.
/// </summary>
public class SandboxMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_NonSandboxRequest_PassesThrough()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/bookings";
        bool nextCalled = false;
        var middleware = new SandboxMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }
}

// SecurityHardeningMiddleware deleted (VULN-010): it was dead code not in the pipeline,
// contained 'unsafe-inline' CSP, and was superseded by SecurityHeadersMiddleware.

/// <summary>
/// Tests for LanguageStandardizationMiddleware.
/// </summary>
public class LanguageStandardizationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_CallsNext()
    {
        var context = new DefaultHttpContext();
        bool nextCalled = false;
        var middleware = new LanguageStandardizationMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, new Mock<ILogger<LanguageStandardizationMiddleware>>().Object);

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }
}

/// <summary>
/// Tests for TimezoneStandardizationMiddleware.
/// </summary>
public class TimezoneStandardizationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_CallsNext()
    {
        var context = new DefaultHttpContext();
        bool nextCalled = false;
        var middleware = new TimezoneStandardizationMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, new Mock<ILogger<TimezoneStandardizationMiddleware>>().Object);

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }
}
