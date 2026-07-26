using System.Linq;
using FluentAssertions;
using Upkilo.Infrastructure.Services;
using Xunit;

namespace Upkilo.Tests.Services;

public class WebhookRegistryTests
{
    [Fact]
    public void Constructor_RegistersDefaultEvents()
    {
        var registry = new WebhookRegistry();
        var all = registry.GetAll().ToList();

        all.Should().NotBeEmpty();
        all.Should().Contain(e => e.EventType == "booking.created");
        all.Should().Contain(e => e.EventType == "client.created");
    }

    [Fact]
    public void Register_AddsNewEvent_OnlyIfUnique()
    {
        var registry = new WebhookRegistry();
        
        registry.Register("custom.event", "description");
        var all = registry.GetAll().ToList();
        all.Should().Contain(e => e.EventType == "custom.event");

        // Adding duplicate should not increase size
        var countBefore = registry.GetAll().Count();
        registry.Register("custom.event", "new desc");
        registry.GetAll().Count().Should().Be(countBefore);
    }

    [Fact]
    public void IsValid_ChecksCorrectly()
    {
        var registry = new WebhookRegistry();

        registry.IsValid("*").Should().BeTrue();
        registry.IsValid("booking.created").Should().BeTrue();
        registry.IsValid("nonexistent.event").Should().BeFalse();
    }
}
