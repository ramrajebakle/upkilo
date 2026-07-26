using Xunit;
using Upkilo.Infrastructure.Services;
using Upkilo.Core.Interfaces;
using Moq;
using Microsoft.Extensions.Logging;
using Upkilo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Upkilo.Tests.Services
{
    public class RecurrenceTests
    {
        private readonly SchedulingService _schedulingService;
        private readonly Guid _tenantId = Guid.NewGuid();

        public RecurrenceTests()
        {
            var loggerMock = new Mock<ILogger<SchedulingService>>();
            var contextMock = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
            var lockProviderMock = new Mock<IDistributedLockProvider>();
            var coalescerMock = new Mock<IRequestCoalescer>();
            var timezoneServiceMock = new Mock<ITimezoneService>();
            var eventServiceMock = new Mock<IEventService>();

            _schedulingService = new SchedulingService(
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                loggerMock.Object);
        }

        [Fact]
        public async Task GenerateDaily_ShouldReturnCorrectDates()
        {
            // Arrange
            var startDate = new DateTime(2026, 3, 1);
            var occurrences = 5;

            // Act
            var dates = await _schedulingService.GenerateRecurrenceDatesAsync(
                _tenantId, "Daily", 1, startDate, null, occurrences, null);

            // Assert
            Assert.Equal(5, dates.Count);
            Assert.Equal(startDate, dates[0]);
            Assert.Equal(startDate.AddDays(4), dates[4]);
        }

        [Fact]
        public async Task GenerateWeekly_WithInterval_ShouldSkipWeeks()
        {
            // Arrange
            var startDate = new DateTime(2026, 3, 2); // Monday
            var interval = 2; // Every 2 weeks
            var occurrences = 3;

            // Act
            var dates = await _schedulingService.GenerateRecurrenceDatesAsync(
                _tenantId, "Weekly", interval, startDate, null, occurrences, null);

            // Assert
            Assert.Equal(3, dates.Count);
            Assert.Equal(startDate, dates[0]);
            Assert.Equal(startDate.AddDays(14), dates[1]);
            Assert.Equal(startDate.AddDays(28), dates[2]);
        }

        [Fact]
        public async Task GenerateWeekly_WithDaysOfWeek_ShouldReturnMultiDaysPerWeek()
        {
            // Arrange
            var startDate = new DateTime(2026, 3, 1); // Sunday
            var daysOfWeek = new List<int> { 1, 3, 5 }; // Mon, Wed, Fri
            var occurrences = 6;

            // Act
            var dates = await _schedulingService.GenerateRecurrenceDatesAsync(
                _tenantId, "Weekly", 1, startDate, null, occurrences, daysOfWeek);

            // Assert
            Assert.Equal(6, dates.Count);
            Assert.Equal(new DateTime(2026, 3, 2), dates[0]); // Mon
            Assert.Equal(new DateTime(2026, 3, 4), dates[1]); // Wed
            Assert.Equal(new DateTime(2026, 3, 6), dates[2]); // Fri
            Assert.Equal(new DateTime(2026, 3, 9), dates[3]); // Next Mon
        }

        [Fact]
        public async Task GenerateMonthly_ShouldReturnCorrectDates()
        {
            // Arrange
            var startDate = new DateTime(2026, 3, 15);
            var occurrences = 3;

            // Act
            var dates = await _schedulingService.GenerateRecurrenceDatesAsync(
                _tenantId, "Monthly", 1, startDate, null, occurrences, null);

            // Assert
            Assert.Equal(3, dates.Count);
            Assert.Equal(new DateTime(2026, 3, 15), dates[0]);
            Assert.Equal(new DateTime(2026, 4, 15), dates[1]);
            Assert.Equal(new DateTime(2026, 5, 15), dates[2]);
        }
    }
}
