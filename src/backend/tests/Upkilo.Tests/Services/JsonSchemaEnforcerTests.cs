using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Infrastructure.Services.AI;
using Xunit;

namespace Upkilo.Tests.Services;

public class JsonSchemaEnforcerTests
{
    private readonly Mock<ILogger<JsonSchemaEnforcer>> _loggerMock = new();

    [Fact]
    public async Task EnforceAsync_ValidJson_ReturnsSuccess()
    {
        var enforcer = new JsonSchemaEnforcer(_loggerMock.Object);
        var json = """{"name": "John", "age": 30}""";
        var schema = "name,age";

        var result = await enforcer.EnforceAsync(json, schema);

        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EnforceAsync_EmptyInput_ReturnsFailure()
    {
        var enforcer = new JsonSchemaEnforcer(_loggerMock.Object);

        var result = await enforcer.EnforceAsync("", "{}");

        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task EnforceAsync_InvalidJson_ReturnsFailure()
    {
        var enforcer = new JsonSchemaEnforcer(_loggerMock.Object);

        var result = await enforcer.EnforceAsync("not valid json {{{", "{}");

        result.IsValid.Should().BeFalse();
    }
}
