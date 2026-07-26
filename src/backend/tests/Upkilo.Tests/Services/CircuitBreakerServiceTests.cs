using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Polly.Registry;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;

namespace Upkilo.Tests.Services;

public class CircuitBreakerServiceTests
{
    private readonly ResiliencePipelineRegistry<string> _pipelineRegistry = new();
    private readonly Mock<ILogger<CircuitBreakerService>> _loggerMock = new();
    private readonly Mock<IBusinessMetrics> _metricsMock = new();
    private readonly CircuitBreakerService _sut;

    public CircuitBreakerServiceTests()
    {
        _metricsMock
            .Setup(m => m.RecordCircuitBreakerTrip(It.IsAny<string>()));

        _sut = new CircuitBreakerService(_pipelineRegistry, _loggerMock.Object, _metricsMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_SuccessfulAction_ReturnsResult()
    {
        var result = await _sut.ExecuteAsync("test-circuit", () => Task.FromResult(42));

        result.Should().Be(42);
    }

    [Fact]
    public async Task ExecuteAsync_ActionThrows_PropagatesException()
    {
        var act = async () => await _sut.ExecuteAsync<string>("failing-circuit", () => throw new InvalidOperationException("Simulated failure"));

        await act.Should().ThrowAsync<Exception>();
    }
}
