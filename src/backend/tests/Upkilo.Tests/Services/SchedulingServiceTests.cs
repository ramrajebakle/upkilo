using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services
{
    public class SchedulingServiceTests : IDisposable
    {
        private readonly TestDbContextFactory _dbFactory;
        private readonly Mock<IDistributedLockProvider> _lockProviderMock = new();
        private readonly Mock<IRequestCoalescer> _coalescerMock = new();
        private readonly Mock<ITimezoneService> _timezoneServiceMock = new();
        private readonly Mock<IEventService> _eventServiceMock = new();
        private readonly Mock<ICacheService> _cacheMock = new();
        private readonly Mock<ILogger<SchedulingService>> _loggerMock = new();

        public SchedulingServiceTests()
        {
            _dbFactory = new TestDbContextFactory();

            // Set up coalesce mock to execute action immediately
            _coalescerMock.Setup(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<Func<Task<IEnumerable<DateTime>>>>()))
                .Returns<string, Func<Task<IEnumerable<DateTime>>>>((k, f) => f());
            _coalescerMock.Setup(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<Func<Task<bool>>>()))
                .Returns<string, Func<Task<bool>>>((k, f) => f());

            // Set up timezone mock to do simple passthroughs
            _timezoneServiceMock.Setup(t => t.GetBookingTimezone(It.IsAny<Booking>())).Returns("UTC");
            _timezoneServiceMock.Setup(t => t.ConvertToUtc(It.IsAny<DateTime>(), It.IsAny<string>())).Returns<DateTime, string>((dt, tz) => dt);
            _timezoneServiceMock.Setup(t => t.ConvertToUserTimezone(It.IsAny<DateTime>(), It.IsAny<string>())).Returns<DateTime, string>((dt, tz) => dt);

            // Always miss the cache and run the factory. An unconfigured mock returns
            // default(int) == 0, which made CheckConcurrencyLimitAsync read a limit of 0
            // and reject every slot hold instead of resolving the real (unlimited) limit.
            _cacheMock.Setup(c => c.GetOrSetAsync(
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Func<Task<int>>>(), It.IsAny<TimeSpan?>()))
                .Returns<Guid, string, Func<Task<int>>, TimeSpan?>((t, k, f, e) => f());

            // Set up lock provider mock to return a mock lock
            var lockMock = new Mock<IDisposable>();
            _lockProviderMock.Setup(l => l.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>()))
                .ReturnsAsync(lockMock.Object);
        }

        public void Dispose() => _dbFactory.Dispose();

        private (SchedulingService sut, Upkilo.Infrastructure.Data.AppDbContext ctx, Guid tenantId) CreateSut()
        {
            var ctx = _dbFactory.CreateContext();
            var tenantId = Guid.NewGuid();

            // Seed base tenant
            ctx.Tenants.Add(new Tenant { Id = tenantId, Name = "T", Slug = "t" });
            ctx.SaveChanges();

            var sut = new SchedulingService(
                ctx,
                _lockProviderMock.Object,
                _coalescerMock.Object,
                _timezoneServiceMock.Object,
                _eventServiceMock.Object,
                _cacheMock.Object,
                _loggerMock.Object
            );

            return (sut, ctx, tenantId);
        }

        [Fact]
        public async Task UpdateAvailabilityCacheAsync_GeneratesMaskAndSaves()
        {
            var (sut, ctx, tenantId) = CreateSut();
            var staffId = Guid.NewGuid();
            var date = DateOnly.FromDateTime(DateTime.UtcNow);

            // Seed staff member and working hours
            ctx.StaffMembers.Add(new StaffMember { Id = staffId, TenantId = tenantId, FirstName = "S", LastName = "M" });
            ctx.StaffWorkingHours.Add(new WorkingHours
            {
                Id = Guid.NewGuid(),
                StaffId = staffId,
                DayOfWeek = (int)DateTime.UtcNow.DayOfWeek,
                IsWorkingDay = true,
                StartTime = TimeSpan.FromHours(9),
                EndTime = TimeSpan.FromHours(17)
            });
            ctx.SaveChanges();

            await sut.UpdateAvailabilityCacheAsync(tenantId, staffId, date);

            var cache = ctx.AvailabilityCaches.FirstOrDefault(c => c.StaffId == staffId && c.Date == date);
            cache.Should().NotBeNull();
            cache!.AvailableSlotsMask.Should().Contain("1");
        }

        [Fact]
        public async Task InvalidateStaffCacheAsync_RemovesCache()
        {
            var (sut, ctx, tenantId) = CreateSut();
            var staffId = Guid.NewGuid();
            var date = DateOnly.FromDateTime(DateTime.UtcNow);

            ctx.AvailabilityCaches.Add(new AvailabilityCache
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                StaffId = staffId,
                Date = date,
                AvailableSlotsMask = new string('1', 96)
            });
            ctx.SaveChanges();

            await sut.InvalidateStaffCacheAsync(tenantId, staffId, date);

            ctx.AvailabilityCaches.Any(c => c.StaffId == staffId && c.Date == date).Should().BeFalse();
        }

        [Fact]
        public async Task CreateSlotHoldAsync_SavesHoldInDb()
        {
            var (sut, ctx, tenantId) = CreateSut();
            var staffId = Guid.NewGuid();
            var serviceId = Guid.NewGuid();
            var date = DateTime.UtcNow.Date.AddDays(1).AddHours(10); // 10:00 AM tomorrow
            var dateOnly = DateOnly.FromDateTime(date);

            // Seed dependencies
            ctx.StaffMembers.Add(new StaffMember { Id = staffId, TenantId = tenantId, FirstName = "S", LastName = "M" });
            ctx.Services.Add(new Service { Id = serviceId, TenantId = tenantId, Name = "Serv", DurationMinutes = 30 });
            ctx.StaffWorkingHours.Add(new WorkingHours
            {
                Id = Guid.NewGuid(),
                StaffId = staffId,
                DayOfWeek = (int)date.DayOfWeek,
                IsWorkingDay = true,
                StartTime = TimeSpan.FromHours(9),
                EndTime = TimeSpan.FromHours(17)
            });
            ctx.SaveChanges();

            // Populate cache first
            await sut.UpdateAvailabilityCacheAsync(tenantId, staffId, dateOnly);

            var hold = await sut.CreateSlotHoldAsync(tenantId, serviceId, staffId, date, "session-token-123");

            hold.Should().NotBeNull();
            hold.SessionToken.Should().Be("session-token-123");
            hold.IsReleased.Should().BeFalse();

            ctx.SlotHolds.Any(h => h.Id == hold.Id).Should().BeTrue();
        }

        [Fact]
        public async Task ReleaseSlotHoldAsync_MarksAsReleased()
        {
            var (sut, ctx, tenantId) = CreateSut();
            var holdId = Guid.NewGuid();

            ctx.SlotHolds.Add(new SlotHold
            {
                Id = holdId,
                TenantId = tenantId,
                StaffId = Guid.NewGuid(),
                ServiceId = Guid.NewGuid(),
                SlotDateTime = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                IsReleased = false
            });
            ctx.SaveChanges();

            await sut.ReleaseSlotHoldAsync(holdId);

            var hold = ctx.SlotHolds.Find(holdId);
            hold.Should().NotBeNull();
            hold!.IsReleased.Should().BeTrue();
        }
    }
}
