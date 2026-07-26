using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Infrastructure.Services;
using Xunit;

namespace Upkilo.Tests.Services;

public class JsonSchemaValidatorTests
{
    private readonly Mock<ILogger<JsonSchemaValidator>> _loggerMock = new();
    private JsonSchemaValidator CreateSut() => new JsonSchemaValidator(_loggerMock.Object);

    [Fact]
    public void Validate_KnownSchemaWithAllValidKeys_ReturnsValid()
    {
        var sut = CreateSut();
        var data = new Dictionary<string, object>
        {
            ["timezone"] = "UTC",
            ["locale"] = "en",
            ["currency"] = "USD"
        };

        var result = sut.Validate("TenantSettings", data);

        result.IsValid.Should().BeTrue();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Validate_KnownSchemaWithUnknownKey_IsValidButHasWarnings()
    {
        var sut = CreateSut();
        var data = new Dictionary<string, object>
        {
            ["timezone"] = "UTC",
            ["unknownField"] = "surprise"
        };

        var result = sut.Validate("TenantSettings", data);

        // Forward-compatible: valid but warns
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain("unknownField");
    }

    [Fact]
    public void Validate_NullData_ReturnsValid()
    {
        var sut = CreateSut();

        var result = sut.Validate("TenantSettings", null);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_UnknownSchemaName_ReturnsValid()
    {
        var sut = CreateSut();

        var result = sut.Validate("CompletelyUnknownSchema", new Dictionary<string, object> { ["x"] = 1 });

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_DynamicSchema_ClientCustomFields_AlwaysValid()
    {
        var sut = CreateSut();
        var data = new Dictionary<string, object> { ["anything"] = "goes" };

        var result = sut.Validate("ClientCustomFields", data);

        result.IsValid.Should().BeTrue();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void ValidateJson_ValidJson_ReturnsValid()
    {
        var sut = CreateSut();
        var json = """{"timezone":"UTC","locale":"en"}""";

        var result = sut.ValidateJson("TenantSettings", json);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateJson_InvalidJson_ReturnsInvalidWithWarning()
    {
        var sut = CreateSut();

        var result = sut.ValidateJson("TenantSettings", "NOT VALID JSON {{{");

        result.IsValid.Should().BeFalse();
        result.Warnings.Should().Contain(w => w.Contains("Invalid JSON"));
    }

    [Fact]
    public void ValidateJson_NullOrEmptyJson_ReturnsValid()
    {
        var sut = CreateSut();

        sut.ValidateJson("TenantSettings", null).IsValid.Should().BeTrue();
        sut.ValidateJson("TenantSettings", "").IsValid.Should().BeTrue();
    }
}
