using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class WebhookServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<ILogger<WebhookService>> _loggerMock = new();
    private readonly Mock<HttpMessageHandler> _httpHandlerMock = new();

    public WebhookServiceTests()
    {
        _dbFactory = new TestDbContextFactory();

        // Configure default httpClient mock behavior
        var client = new HttpClient(_httpHandlerMock.Object);
        _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("SuccessResponse")
            });
    }

    public void Dispose() => _dbFactory.Dispose();

    private (WebhookService sut, Upkilo.Infrastructure.Data.AppDbContext ctx, Guid tenantId) CreateSut()
    {
        var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();
        ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "T", Slug = "t" });
        ctx.SaveChanges();
        return (new WebhookService(ctx, _httpClientFactoryMock.Object, _loggerMock.Object), ctx, tenantId);
    }

    [Fact]
    public async Task CreateEndpointAsync_PersistsWebhookWithSecret()
    {
        var (sut, ctx, tenantId) = CreateSut();

        var hook = await sut.CreateEndpointAsync(tenantId, "MyHook",
            "https://example.com/webhook", new[] { "booking.created" });

        hook.Should().NotBeNull();
        hook.Secret.Should().StartWith("whsec_");
        hook.Events.Should().Contain("booking.created");
        ctx.ChangeTracker.Clear();
        ctx.Set<Webhook>().Should().HaveCount(1);
    }

    [Fact]
    public async Task GetEndpointsAsync_ReturnsOnlyTenantWebhooks()
    {
        var (sut, _, tenantId) = CreateSut();
        await sut.CreateEndpointAsync(tenantId, "H1", "https://example.com/1", new[] { "*" });
        await sut.CreateEndpointAsync(Guid.NewGuid(), "H2", "https://example.com/2", new[] { "*" });

        var endpoints = await sut.GetEndpointsAsync(tenantId);

        endpoints.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetEndpointAsync_ReturnsCorrectEndpoint()
    {
        var (sut, _, tenantId) = CreateSut();
        var hook = await sut.CreateEndpointAsync(tenantId, "H1", "https://example.com/1", new[] { "*" });

        var fetched = await sut.GetEndpointAsync(hook.Id);
        fetched.Should().NotBeNull();
        fetched!.Id.Should().Be(hook.Id);
    }

    [Fact]
    public async Task DeleteEndpointAsync_WhenFound_RemovesAndReturnsTrue()
    {
        var (sut, ctx, tenantId) = CreateSut();
        var hook = await sut.CreateEndpointAsync(tenantId, "H1", "https://example.com", new[] { "*" });

        var result = await sut.DeleteEndpointAsync(hook.Id, tenantId);

        result.Should().BeTrue();
        ctx.ChangeTracker.Clear();
        ctx.Set<Webhook>().Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteEndpointAsync_WhenNotFound_ReturnsFalse()
    {
        var (sut, _, tenantId) = CreateSut();

        var result = await sut.DeleteEndpointAsync(Guid.NewGuid(), tenantId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateEndpointAsync_UpdatesAllFields()
    {
        var (sut, ctx, tenantId) = CreateSut();
        var hook = await sut.CreateEndpointAsync(tenantId, "Old Name", "https://example.com", new[] { "*" });

        // Use a real resolvable domain — H-NEW-01 SSRF check does DNS resolution at update time.
        var result = await sut.UpdateEndpointAsync(hook.Id, tenantId, name: "New Name", url: "https://example.com/updated", events: new[] { "booking.created" }, isActive: false);

        result.Should().BeTrue();
        ctx.ChangeTracker.Clear();
        var updated = ctx.Set<Webhook>().Find(hook.Id);
        updated!.Name.Should().Be("New Name");
        updated.Url.Should().Be("https://example.com/updated");
        updated.Events.Should().ContainSingle().Which.Should().Be("booking.created");
        updated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DispatchEventAsync_CreatesDeliveryForMatchingSubscribedWebhooks()
    {
        var (sut, ctx, tenantId) = CreateSut();
        await sut.CreateEndpointAsync(tenantId, "H1", "https://example.com", new[] { "booking.created" });

        await sut.DispatchEventAsync(tenantId, "booking.created", new { Id = "bk-001" });

        ctx.ChangeTracker.Clear();
        ctx.Set<WebhookDelivery>().Should().HaveCount(1);
        ctx.Set<WebhookDelivery>().First().EventType.Should().Be("booking.created");
    }

    [Fact]
    public async Task DispatchEventAsync_WhenEventNotSubscribed_DoesNotCreateDelivery()
    {
        var (sut, ctx, tenantId) = CreateSut();
        await sut.CreateEndpointAsync(tenantId, "H1", "https://example.com", new[] { "client.created" });

        await sut.DispatchEventAsync(tenantId, "booking.created", new { Id = "bk-001" });

        ctx.ChangeTracker.Clear();
        ctx.Set<WebhookDelivery>().Should().BeEmpty();
    }

    [Fact]
    public async Task DispatchEventAsync_WildcardSubscription_AlwaysCreatesDelivery()
    {
        var (sut, ctx, tenantId) = CreateSut();
        await sut.CreateEndpointAsync(tenantId, "H1", "https://example.com", new[] { "*" });

        await sut.DispatchEventAsync(tenantId, "any.event", new { });

        ctx.ChangeTracker.Clear();
        ctx.Set<WebhookDelivery>().Should().HaveCount(1);
    }

    [Fact]
    public async Task SendTestEventAsync_CreatesAndDeliversTestEvent()
    {
        var (sut, ctx, tenantId) = CreateSut();
        var hook = await sut.CreateEndpointAsync(tenantId, "H1", "https://example.com", new[] { "*" });

        var delivery = await sut.SendTestEventAsync(hook.Id, tenantId);

        delivery.Should().NotBeNull();
        delivery.EventType.Should().Be("test.event");
        delivery.Success.Should().BeTrue();

        ctx.ChangeTracker.Clear();
        ctx.Set<WebhookDelivery>().Find(delivery.Id).Should().NotBeNull();
    }

    [Fact]
    public async Task GetDeliveriesAsync_ReturnsFilteredList()
    {
        var (sut, ctx, tenantId) = CreateSut();
        var hook1 = await sut.CreateEndpointAsync(tenantId, "H1", "https://example.com/1", new[] { "*" });
        var hook2 = await sut.CreateEndpointAsync(tenantId, "H2", "https://example.com/2", new[] { "*" });

        var d1 = new WebhookDelivery { Id = Guid.NewGuid(), WebhookId = hook1.Id, EventType = "t1", Payload = "{}" };
        var d2 = new WebhookDelivery { Id = Guid.NewGuid(), WebhookId = hook2.Id, EventType = "t2", Payload = "{}" };
        ctx.Set<WebhookDelivery>().AddRange(d1, d2);
        await ctx.SaveChangesAsync();

        var all = await sut.GetDeliveriesAsync(tenantId);
        all.Should().HaveCount(2);

        var filtered = await sut.GetDeliveriesAsync(tenantId, endpointId: hook1.Id);
        filtered.Should().HaveCount(1);
        filtered.First().WebhookId.Should().Be(hook1.Id);
    }

    [Fact]
    public async Task ResendDeliveryAsync_ResetsAndDelivers()
    {
        var (sut, ctx, tenantId) = CreateSut();
        var hook = await sut.CreateEndpointAsync(tenantId, "H1", "https://example.com", new[] { "*" });
        var delivery = new WebhookDelivery
        {
            Id = Guid.NewGuid(),
            WebhookId = hook.Id,
            EventType = "t1",
            Payload = "{}",
            AttemptNumber = 3,
            Success = false
        };
        ctx.Set<WebhookDelivery>().Add(delivery);
        await ctx.SaveChangesAsync();

        var result = await sut.ResendDeliveryAsync(delivery.Id, tenantId);

        result.Should().BeTrue();
        ctx.ChangeTracker.Clear();
        var updated = ctx.Set<WebhookDelivery>().Find(delivery.Id);
        updated!.AttemptNumber.Should().Be(1); // 0 in code before deliver, then incremented during deliver
        updated.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ClearDeliveriesAsync_RemovesAll()
    {
        var (sut, ctx, tenantId) = CreateSut();
        var hook = await sut.CreateEndpointAsync(tenantId, "H1", "https://example.com", new[] { "*" });
        var d1 = new WebhookDelivery { Id = Guid.NewGuid(), WebhookId = hook.Id, EventType = "t1", Payload = "{}" };
        ctx.Set<WebhookDelivery>().Add(d1);
        await ctx.SaveChangesAsync();

        var result = await sut.ClearDeliveriesAsync(hook.Id, tenantId);
        result.Should().BeTrue();

        ctx.ChangeTracker.Clear();
        ctx.Set<WebhookDelivery>().Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessPendingDeliveriesAsync_SendsPendingWithBackoff()
    {
        var (sut, ctx, tenantId) = CreateSut();
        var hook = await sut.CreateEndpointAsync(tenantId, "H1", "https://example.com", new[] { "*" });
        
        // Backoff: 2^attempt minutes. If attempt = 1, delay = 2 mins. If UpdatedAt = 3 mins ago, should process.
        var delivery = new WebhookDelivery
        {
            Id = Guid.NewGuid(),
            WebhookId = hook.Id,
            EventType = "t1",
            Payload = "{}",
            AttemptNumber = 1,
            Success = false,
            UpdatedAt = DateTime.UtcNow.AddMinutes(-5) // 5 minutes ago (limit is 2 mins)
        };
        ctx.Set<WebhookDelivery>().Add(delivery);
        await ctx.SaveChangesAsync();
        // AppDbContext auto-sets UpdatedAt = UtcNow on save; override via ExecuteUpdateAsync to put it in the past
        var pastTime = DateTime.UtcNow.AddMinutes(-5);
        var deliveryId = delivery.Id;
        await ctx.Set<WebhookDelivery>()
            .Where(d => d.Id == deliveryId)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.UpdatedAt, pastTime));
        ctx.ChangeTracker.Clear();

        await sut.ProcessPendingDeliveriesAsync();

        ctx.ChangeTracker.Clear();
        var updated = ctx.Set<WebhookDelivery>().Find(delivery.Id);
        updated!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task CreateEndpointAsync_BlocksLocalhostUrl_ThrowsInvalidOperation()
    {
        var (sut, _, tenantId) = CreateSut();

        // H-NEW-01 FIX: SSRF validation now runs at creation time.
        // Localhost URLs are rejected before they are ever persisted.
        var act = async () => await sut.CreateEndpointAsync(tenantId, "BadHook", "https://localhost/webhook", new[] { "*" });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid webhook URL*");
    }

    [Fact]
    public async Task CreateEndpointAsync_BlocksPrivateIpUrl_ThrowsInvalidOperation()
    {
        var (sut, _, tenantId) = CreateSut();

        var act = async () => await sut.CreateEndpointAsync(tenantId, "BadHook", "https://192.168.1.1/webhook", new[] { "*" });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid webhook URL*");
    }

    [Fact]
    public async Task CreateEndpointAsync_BlocksCloudMetadataUrl_ThrowsInvalidOperation()
    {
        var (sut, _, tenantId) = CreateSut();

        var act = async () => await sut.CreateEndpointAsync(tenantId, "BadHook", "https://169.254.169.254/latest/meta-data", new[] { "*" });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid webhook URL*");
    }

    [Fact]
    public async Task SendWebhookRequestAsync_DeliversHttpRequests()
    {
        var (sut, _, _) = CreateSut();

        var success = await sut.SendWebhookRequestAsync("https://example.com/external", "POST", new { data = 1 });
        success.Should().BeTrue();

        // SSRF blocked
        var block = await sut.SendWebhookRequestAsync("https://127.0.0.1/local", "POST", new { });
        block.Should().BeFalse();
    }
}
