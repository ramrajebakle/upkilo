using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

public interface ISchedulingService
{
    Task<IEnumerable<DateTime>> GetAvailableSlotsAsync(Guid tenantId, Guid serviceId, Guid? staffId, DateTime date);
    Task<bool> IsSlotAvailableAsync(Guid tenantId, Guid serviceId, Guid? staffId, DateTime startTime, int durationMinutes);
    Task<SlotHold> CreateSlotHoldAsync(Guid tenantId, Guid serviceId, Guid staffId, DateTime slotDateTime, string sessionToken);
    Task ReleaseSlotHoldAsync(Guid holdId);
    Task UpdateAvailabilityCacheAsync(Guid tenantId, Guid staffId, DateOnly date);
    Task InvalidateStaffCacheAsync(Guid tenantId, Guid staffId, DateOnly? date = null);
    Task<bool> CheckConcurrencyLimitAsync(Guid tenantId);
    Task<List<DateTime>> GenerateRecurrenceDatesAsync(Guid tenantId, string frequency, int interval, DateTime startDate, DateTime? endDate, int? occurrences, List<int>? daysOfWeek);
}
