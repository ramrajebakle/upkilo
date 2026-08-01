using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class SupportTicketServiceTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<ILogger<SupportTicketService>> _loggerMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();

    public SupportTicketServiceTests() => _dbFactory = new TestDbContextFactory();
    public void Dispose() => _dbFactory.Dispose();

    private (SupportTicketService sut, Upkilo.Infrastructure.Data.AppDbContext ctx, Guid tenantId, Guid userId) CreateSut()
    {
        var ctx = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "T", Slug = "t" });
        ctx.Users.Add(new User
        {
            Id = userId,
            TenantId = tenantId,
            FirstName = "Staff",
            LastName = "Member",
            Email = $"staff-{userId}@test.com",
            PasswordHash = "hash"
        });
        ctx.SaveChanges();

        _emailServiceMock.Setup(e => e.SendSystemEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        return (new SupportTicketService(ctx, _emailServiceMock.Object, _loggerMock.Object), ctx, tenantId, userId);
    }

    [Fact]
    public async Task CreateTicketAsync_NormalPriority_SetsSla24Hours()
    {
        var (sut, _, tenantId, userId) = CreateSut();
        var before = DateTime.UtcNow.AddHours(23);

        var ticket = await sut.CreateTicketAsync(new SupportTicket
        {
            TenantId = tenantId,
            SubmittedByUserId = userId,
            Subject = "Help!",
            ContactEmail = "user@test.com",
            Priority = TicketPriority.Normal
        });

        ticket.SlaExpiresAt.Should().BeAfter(before);
        ticket.Status.Should().Be(TicketStatus.Open);
        ticket.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateTicketAsync_HighPriority_SetsSla4Hours()
    {
        var (sut, _, tenantId, userId) = CreateSut();

        var ticket = await sut.CreateTicketAsync(new SupportTicket
        {
            TenantId = tenantId,
            SubmittedByUserId = userId,
            Subject = "URGENT!",
            ContactEmail = "user@test.com",
            Priority = TicketPriority.High
        });

        // SLA should be ~4 hours, not 24
        ticket.SlaExpiresAt.Should().BeBefore(DateTime.UtcNow.AddHours(5));
    }

    [Fact]
    public async Task GetTicketAsync_WhenFound_ReturnsTicket()
    {
        var (sut, _, tenantId, userId) = CreateSut();
        var ticket = await sut.CreateTicketAsync(new SupportTicket
        {
            TenantId = tenantId,
            SubmittedByUserId = userId,
            Subject = "Test",
            ContactEmail = "x@t.com"
        });

        var found = await sut.GetTicketAsync(ticket.Id, tenantId);

        found.Should().NotBeNull();
        found!.Subject.Should().Be("Test");
    }

    [Fact]
    public async Task GetTicketAsync_WhenWrongTenant_ReturnsNull()
    {
        var (sut, _, tenantId, userId) = CreateSut();
        var ticket = await sut.CreateTicketAsync(new SupportTicket
        {
            TenantId = tenantId,
            SubmittedByUserId = userId,
            Subject = "Test",
            ContactEmail = "x@t.com"
        });

        var result = await sut.GetTicketAsync(ticket.Id, Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateStatusAsync_SetsResolvedAt_WhenResolved()
    {
        var (sut, ctx, tenantId, userId) = CreateSut();
        var ticket = await sut.CreateTicketAsync(new SupportTicket
        {
            TenantId = tenantId,
            SubmittedByUserId = userId,
            Subject = "Fix me",
            ContactEmail = "x@t.com"
        });

        await sut.UpdateStatusAsync(ticket.Id, TicketStatus.Resolved);

        ctx.ChangeTracker.Clear();
        var updated = ctx.SupportTickets.Find(ticket.Id);
        updated!.Status.Should().Be(TicketStatus.Resolved);
        updated.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task AddCommentAsync_WhenTicketNotFound_ThrowsKeyNotFoundException()
    {
        var (sut, _, _, _) = CreateSut();

        var act = () => sut.AddCommentAsync(Guid.NewGuid(), new SupportTicketComment { Content = "hi" });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
