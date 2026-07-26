using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Tests.Helpers;

/// <summary>
/// Base class for controller unit tests.
/// Provides a seeded DbContext, tenant resolution, and authenticated user context.
/// </summary>
public abstract class ControllerTestBase : IDisposable
{
    protected readonly TestDbContextFactory DbFactory;
    protected readonly AppDbContext Context;
    protected readonly Guid TenantId = Guid.NewGuid();
    protected readonly Guid UserId = Guid.NewGuid();
    protected readonly Mock<ITenantProvider> TenantProvider;

    protected ControllerTestBase()
    {
        DbFactory = new TestDbContextFactory();
        Context = DbFactory.CreateContext();
        TenantProvider = MockFactory.CreateTenantProvider(TenantId);

        // Seed a tenant
        Context.Tenants.Add(TestFixtures.CreateTenant(TenantId));
        Context.SaveChanges();
    }

    /// <summary>
    /// Configures a controller with an authenticated HttpContext
    /// </summary>
    protected T WithAuth<T>(T controller) where T : ControllerBase
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, UserId.ToString()),
            new("sub", UserId.ToString()),
            new("id", UserId.ToString()),
            new("tenant_id", TenantId.ToString()),
            new(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal,
                Connection = { RemoteIpAddress = System.Net.IPAddress.Loopback }
            }
        };
        controller.ControllerContext.HttpContext.Items["TenantId"] = TenantId.ToString();
        return controller;
    }

    /// <summary>
    /// Asserts that an IActionResult is OkObjectResult
    /// </summary>
    protected static T AssertOk<T>(IActionResult result)
    {
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        return (T)ok.Value!;
    }

    /// <summary>
    /// Asserts that result is 400
    /// </summary>
    protected static void AssertBadRequest(IActionResult result) =>
        result.Should().BeOfType<BadRequestObjectResult>();

    /// <summary>
    /// Asserts that result is 401
    /// </summary>
    protected static void AssertUnauthorized(IActionResult result) =>
        result.Should().BeOfType<UnauthorizedObjectResult>();

    /// <summary>
    /// Asserts that result is 404
    /// </summary>
    protected static void AssertNotFound(IActionResult result) =>
        result.Should().BeOfType<NotFoundObjectResult>();

    public void Dispose()
    {
        DbFactory.Dispose();
        GC.SuppressFinalize(this);
    }
}
