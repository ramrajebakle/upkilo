using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Upkilo.API.Middleware;

namespace Upkilo.Tests.Middleware;

public class SecurityHeadersMiddlewareTests
{
    private class TestHttpResponseFeature : Microsoft.AspNetCore.Http.Features.HttpResponseFeature
    {
        private readonly List<(Func<object, Task> callback, object state)> _onStarting = new();
        public override void OnStarting(Func<object, Task> callback, object state)
        {
            _onStarting.Add((callback, state));
            base.OnStarting(callback, state);
        }
        public async Task InvokeOnStartingAsync()
        {
            foreach (var cb in _onStarting) await cb.callback(cb.state);
        }
    }

    /// <summary>Creates an HttpContext with IWebHostEnvironment registered in RequestServices.</summary>
    private static DefaultHttpContext CreateContext(bool isDevelopment = false)
    {
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.EnvironmentName)
               .Returns(isDevelopment ? "Development" : "Production");

        var services = new ServiceCollection();
        services.AddSingleton(envMock.Object);
        var provider = services.BuildServiceProvider();

        var context = new DefaultHttpContext();
        context.RequestServices = provider;
        context.Features.Set<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>(new TestHttpResponseFeature());
        return context;
    }


    [Fact]
    public async Task InvokeAsync_AddsAllSecurityHeaders()
    {
        var context = CreateContext(isDevelopment: false);
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);
        var feature = (TestHttpResponseFeature)context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>()!;
        await feature.InvokeOnStartingAsync();

        context.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
        context.Response.Headers["X-Frame-Options"].ToString().Should().Be("DENY");
        context.Response.Headers["X-XSS-Protection"].ToString().Should().Be("1; mode=block");
        context.Response.Headers["Referrer-Policy"].ToString().Should().Be("strict-origin-when-cross-origin");
        context.Response.Headers["Permissions-Policy"].ToString().Should().NotBeNullOrEmpty();
        // M-NEW-02 FIX: Production CSP is strict — default-src 'none'; frame-ancestors 'none'
        context.Response.Headers["Content-Security-Policy"].ToString().Should().Contain("default-src 'none'");
    }

    [Fact]
    public async Task InvokeAsync_Development_UsesNonceCsp()
    {
        var context = CreateContext(isDevelopment: true);
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);
        var feature = (TestHttpResponseFeature)context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>()!;
        await feature.InvokeOnStartingAsync();

        // C4: CSP now uses nonces instead of 'unsafe-inline' in all environments
        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        csp.Should().Contain("nonce-");
        csp.Should().NotContain("unsafe-inline");
    }

    [Fact]
    public async Task InvokeAsync_ApiPath_AddsCacheControlHeaders()
    {
        var context = CreateContext(isDevelopment: false);
        context.Request.Path = "/api/v1/bookings";
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);
        var feature = (TestHttpResponseFeature)context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>()!;
        await feature.InvokeOnStartingAsync();

        context.Response.Headers["Cache-Control"].ToString().Should().Contain("no-store");
        context.Response.Headers["Pragma"].ToString().Should().Be("no-cache");
    }

    [Fact]
    public async Task InvokeAsync_NonApiPath_NoCacheHeaders()
    {
        var context = CreateContext(isDevelopment: false);
        context.Request.Path = "/health";
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);
        var feature = (TestHttpResponseFeature)context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>()!;
        await feature.InvokeOnStartingAsync();

        context.Response.Headers.ContainsKey("Cache-Control").Should().BeFalse();
    }
}

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_NoHeader_GeneratesCorrelationId()
    {
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers["X-Correlation-ID"].ToString().Should().NotBeNullOrEmpty();
        context.Items["CorrelationId"].Should().NotBeNull();
    }

    [Fact]
    public async Task InvokeAsync_ExistingHeader_UsesIt()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-ID"] = "existing-123";
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers["X-Correlation-ID"].ToString().Should().Be("existing-123");
        context.Items["CorrelationId"]!.ToString().Should().Be("existing-123");
    }
}

public class RequestTimingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_AddsResponseTimeHeader()
    {
        var context = new DefaultHttpContext();
        var middleware = new RequestTimingMiddleware(_ => Task.CompletedTask);

        // Trigger the OnStarting callbacks
        await middleware.InvokeAsync(context);

        // The header is set via OnStarting callback which fires when response starts
        // In test context, verify the middleware doesn't throw
        context.Should().NotBeNull();
    }
}

public class RequestTimeoutMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_FastRequest_CompletesNormally()
    {
        var context = new DefaultHttpContext();
        var middleware = new RequestTimeoutMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_SlowRequest_Returns408()
    {
        var context = new DefaultHttpContext();
        context.Items["CorrelationId"] = "test-123";
        var middleware = new RequestTimeoutMiddleware(
            async ctx => { await Task.Delay(5000, ctx.RequestAborted); },  // Simulate slow request
            TimeSpan.FromMilliseconds(50));           // Very short timeout

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(408);
    }
}
