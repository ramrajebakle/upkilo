using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Upkilo.Infrastructure.Services;

namespace Upkilo.Tests.Services;

public class TenantProviderTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
    private readonly TenantProvider _sut;

    public TenantProviderTests()
    {
        _httpContextAccessor = new Mock<IHttpContextAccessor>();
        _sut = new TenantProvider(_httpContextAccessor.Object);
    }

    [Fact]
    public void GetTenantId_WhenTenantIdInItems_ReturnsParsedGuid()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var context = new DefaultHttpContext();
        context.Items["TenantId"] = tenantId.ToString();
        _httpContextAccessor.Setup(x => x.HttpContext).Returns(context);

        // Act
        var result = _sut.GetTenantId();

        // Assert
        result.Should().Be(tenantId);
    }

    [Fact]
    public void GetTenantId_WhenNoHttpContext_ReturnsNull()
    {
        // Arrange
        _httpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _sut.GetTenantId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetTenantId_WhenTenantIdNotInItems_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        _httpContextAccessor.Setup(x => x.HttpContext).Returns(context);

        // Act
        var result = _sut.GetTenantId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetTenantId_WhenTenantIdIsInvalidGuid_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Items["TenantId"] = "not-a-guid";
        _httpContextAccessor.Setup(x => x.HttpContext).Returns(context);

        // Act
        var result = _sut.GetTenantId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetUserId_WhenNameIdentifierClaimExists_ReturnsParsedGuid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "test");
        context.User = new ClaimsPrincipal(identity);
        _httpContextAccessor.Setup(x => x.HttpContext).Returns(context);

        // Act
        var result = _sut.GetUserId();

        // Assert
        result.Should().Be(userId);
    }

    [Fact]
    public void GetUserId_WhenNoNameIdentifierClaim_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        _httpContextAccessor.Setup(x => x.HttpContext).Returns(context);

        // Act
        var result = _sut.GetUserId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetUserId_WhenClaimIsNotValidGuid_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "not-a-guid")
        }, "test");
        context.User = new ClaimsPrincipal(identity);
        _httpContextAccessor.Setup(x => x.HttpContext).Returns(context);

        // Act
        var result = _sut.GetUserId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetUserId_WhenNoHttpContext_ReturnsNull()
    {
        // Arrange
        _httpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _sut.GetUserId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetTenantId_FallbackToJwtClaim_ReturnsGuid()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var context = new DefaultHttpContext();
        // Item is not set, but User claim is
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("tenant_id", tenantId.ToString())
        }, "test");
        context.User = new ClaimsPrincipal(identity);
        _httpContextAccessor.Setup(x => x.HttpContext).Returns(context);

        // Act
        var result = _sut.GetTenantId();

        // Assert
        result.Should().Be(tenantId);
    }

    [Fact]
    public void GetTimezone_WhenHeaderExists_ReturnsTimezone()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Timezone"] = "America/New_York";
        _httpContextAccessor.Setup(x => x.HttpContext).Returns(context);

        // Act
        var result = _sut.GetTimezone();

        // Assert
        result.Should().Be("America/New_York");
    }

    [Fact]
    public void GetTimezone_WhenHeaderDoesNotExist_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        _httpContextAccessor.Setup(x => x.HttpContext).Returns(context);

        // Act
        var result = _sut.GetTimezone();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetTimezone_WhenNoHttpContext_ReturnsNull()
    {
        // Arrange
        _httpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _sut.GetTimezone();

        // Assert
        result.Should().BeNull();
    }
}
