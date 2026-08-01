using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class SchedulingService : ISchedulingService
{
    private readonly AppDbContext _context;
    private readonly IDistributedLockProvider _lockProvider;
    private readonly IRequestCoalescer _coalescer;
    private readonly ITimezoneService _timezoneService;
    private readonly IEventService _eventService;
    private readonly ICacheService _cache;
    private readonly ILogger<SchedulingService> _logger;

    public SchedulingService(
        AppDbContext context,
        IDistributedLockProvider lockProvider,
        IRequestCoalescer coalescer,
        ITimezoneService timezoneService,
        IEventService eventService,
        ICacheService cache,
        ILogger<SchedulingService> logger)
    {
        _context = context;
        _lockProvider = lockProvider;
        _coalescer = coalescer;
        _timezoneService = timezoneService;
        _eventService = eventService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IEnumerable<DateTime>> GetAvailableSlotsAsync(Guid tenantId, Guid serviceId, Guid? staffId, DateTime date)
    {
        var cacheKey = $"slots:{tenantId}:{serviceId}:{staffId?.ToString() ?? "any"}:{date:yyyyMMdd}";

        return await _coalescer.ExecuteAsync(cacheKey, async () =>
        {
            if (!await CheckConcurrencyLimitAsync(tenantId))
            {
                return Enumerable.Empty<DateTime>();
            }

            var service = await _context.Services.FindAsync(serviceId);
            if (service == null) return Enumerable.Empty<DateTime>();

            var staffIds = staffId.HasValue
                ? new List<Guid> { staffId.Value }
                : await _context.StaffServices
                    .Where(ss => ss.ServiceId == serviceId)
                    .Select(ss => ss.StaffId)
                    .ToListAsync();

            var availableFullSlots = new List<DateTime>();

            var dateOnly = DateOnly.FromDateTime(date);

            // Batch-load all availability caches for every staff member in one query
            // instead of 1 query per staff member (N+1 → 1).
            var cacheMap = await _context.AvailabilityCaches
                .Where(c => c.TenantId == tenantId && staffIds.Contains(c.StaffId) && c.Date == dateOnly)
                .ToDictionaryAsync(c => c.StaffId);

            foreach (var id in staffIds)
            {
                cacheMap.TryGetValue(id, out var avCache);

                if (avCache == null || avCache.LastUpdatedAt < DateTime.UtcNow.AddMinutes(-15))
                {
                    await UpdateAvailabilityCacheAsync(tenantId, id, dateOnly);
                    avCache = await _context.AvailabilityCaches
                        .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.StaffId == id && c.Date == dateOnly);
                }

                if (avCache != null)
                {
                    var mask = avCache.AvailableSlotsMask;
                    for (int i = 0; i < mask.Length; i++)
                    {
                        if (mask[i] == '1' && DoesServiceFitInCache(mask, i, service))
                            availableFullSlots.Add(date.Date.AddMinutes(i * 15));
                    }
                }
            }

            return availableFullSlots.Distinct().OrderBy(s => s);
        });
    }

    // Synchronous — no awaits. Was async Task<bool> which allocated a Task on every call
    // through the 96-slot mask loop. Static so the compiler cannot accidentally add awaits.
    private static bool DoesServiceFitInCache(string mask, int startIndex, Service service)
    {
        int slotsNeeded = (int)Math.Ceiling(service.DurationMinutes / 15.0);
        int bufferBeforeSlots = (int)Math.Ceiling(service.BufferBeforeMinutes / 15.0);
        int bufferAfterSlots = (int)Math.Ceiling(service.BufferAfterMinutes / 15.0);

        int checkStart = startIndex - bufferBeforeSlots;
        int checkEnd = startIndex + slotsNeeded + bufferAfterSlots;

        if (checkStart < 0 || checkEnd > 96) return false;

        for (int i = checkStart; i < checkEnd; i++)
            if (mask[i] == '0') return false;

        return true;
    }

    private async Task<IEnumerable<DateTime>> CalculateStaffAvailabilityAsync(Guid tenantId, Guid staffId, Service service, DateTime date)
    {
        var slots = new List<DateTime>();

        // 0. Resolve Timezone
        var staff = await _context.StaffMembers
            .Include(s => s.Tenant)
            .FirstOrDefaultAsync(s => s.Id == staffId);

        // We use a dummy booking to resolve the hierarchy (Staff > Tenant)
        var dummyBooking = new Booking { Staff = staff, TenantId = tenantId, Tenant = staff?.Tenant };
        var timezoneId = _timezoneService.GetBookingTimezone(dummyBooking);

        // Calculate UTC window for this local "date"
        var localDayStart = date.Date; // e.g. 2026-04-01 00:00 (assumed local relative to the request)
        var utcDayStart = _timezoneService.ConvertToUtc(localDayStart, timezoneId);
        var utcDayEnd = utcDayStart.AddDays(1);

        // 1. Get Working Hours
        var dayOfWeek = (int)localDayStart.DayOfWeek;
        var workingHours = await _context.StaffWorkingHours
            .FirstOrDefaultAsync(wh => wh.StaffId == staffId && wh.DayOfWeek == dayOfWeek && wh.IsWorkingDay);

        if (workingHours == null) return slots;

        // 2. Check Exceptions
        var dateOnly = DateOnly.FromDateTime(localDayStart);
        var exception = await _context.StaffExceptions
            .FirstOrDefaultAsync(se => se.StaffId == staffId && se.Date == dateOnly);

        if (exception != null && exception.IsAllDay) return slots;

        // Effective working hours in LOCAL time
        var workStart = localDayStart.Add(workingHours.StartTime);
        var workEnd = localDayStart.Add(workingHours.EndTime);

        // 2.5 Check Location Holidays — coalesced so parallel staff-loop iterations share one query.
        var location = await _coalescer.ExecuteAsync($"primary_location:{tenantId}",
            () => _context.Locations.FirstOrDefaultAsync(l => l.TenantId == tenantId && l.IsPrimary));
        if (location != null && !string.IsNullOrEmpty(location.Holidays))
        {
            if (location.Holidays.Contains(localDayStart.ToString("yyyy-MM-dd")))
            {
                return slots;
            }
        }

        // 3. Get Existing Bookings in UTC window
        var bookings = await _context.Bookings
            .Include(b => b.Service)
            .Where(b => b.StaffId == staffId &&
                        b.StartTime < utcDayEnd &&
                        b.EndTime > utcDayStart &&
                        b.Status != BookingStatus.Cancelled)
            .ToListAsync();

        // 4. Get Active Slot Holds in local day
        var holds = await _context.SlotHolds
            .Where(h => h.StaffId == staffId &&
                        h.SlotDateTime >= utcDayStart &&
                        h.SlotDateTime < utcDayEnd &&
                        h.ExpiresAt > DateTime.UtcNow &&
                        !h.IsReleased)
            .ToListAsync();

        // Convert bookings/holds to local day range for easier slot generation logic
        // ... slots logic below remains local-time based ...
        // Wait, the slot generation logic uses currentTime which is localDayStart + workingHours.StartTime.
        // So we need to compare local slots with local versions of bookings.

        var localizedBookings = bookings.Select(b => new
        {
            LocalStart = _timezoneService.ConvertToUserTimezone(b.StartTime, timezoneId),
            LocalEnd = _timezoneService.ConvertToUserTimezone(b.EndTime, timezoneId),
            b.GroupSize,
            b.ServiceId,
            b.Service
        }).ToList();

        var localizedHolds = holds.Select(h => new
        {
            LocalStart = _timezoneService.ConvertToUserTimezone(h.SlotDateTime, timezoneId)
        }).ToList();

        // 5. Generate Slots
        // We define the "Service Window" as: [Start - BufferBefore, Start + Duration + BufferAfter]
        // This entire window must be free.
        // It must also fall within Working Hours (Start - BufferBefore >= WorkStart ? Maybe not, usually prep can trigger early arrival? 
        // Let's assume strict: The actual SERVICE time must be within working hours. But the staff must be Free for the buffer.)

        // Policy: 
        // - Service Start/End must be within WorkStart/WorkEnd.
        // - BufferBefore/BufferAfter must NOT overlap with other bookings.
        // - BufferBefore/BufferAfter CAN overlap with outside working hours? (Usually no, staff shouldn't work unpaid).
        // -> Strict Policy: Entire [Start-Before, End+After] must be within [WorkStart, WorkEnd].

        var currentTime = workStart;
        var duration = service.DurationMinutes;
        var bufferBefore = service.BufferBeforeMinutes;
        var bufferAfter = service.BufferAfterMinutes;
        var totalServiceMinutes = duration; // The client facing duration

        while (currentTime.AddMinutes(totalServiceMinutes) <= workEnd)
        {
            var slotStart = currentTime;
            var slotEnd = currentTime.AddMinutes(totalServiceMinutes);

            // Effective Busy Window for this proposed slot
            var myBusyStart = slotStart.AddMinutes(-bufferBefore);
            var myBusyEnd = slotEnd.AddMinutes(bufferAfter);

            // Bounds Check: Must fit within working hours
            if (myBusyStart < workStart || myBusyEnd > workEnd)
            {
                currentTime = currentTime.AddMinutes(15);
                continue;
            }

            // Check Booking Overlaps (with group capacity support)
            bool isBlocked = false;

            if (service.MaxAttendees > 1)
            {
                // Group/class-style service: count total participants in overlapping bookings
                int totalAttendees = 0;
                foreach (var b in localizedBookings)
                {
                    var bBufferBefore = b.Service?.BufferBeforeMinutes ?? 0;
                    var bBufferAfter = b.Service?.BufferAfterMinutes ?? 0;
                    var bBusyStart = b.LocalStart.AddMinutes(-bBufferBefore);
                    var bBusyEnd = b.LocalEnd.AddMinutes(bBufferAfter);

                    // Only count bookings for the SAME service at the SAME time slot
                    if (b.ServiceId == service.Id && b.LocalStart == slotStart)
                    {
                        totalAttendees += b.GroupSize;
                    }
                    // Still block on overlapping bookings for different services
                    else if (myBusyStart < bBusyEnd && myBusyEnd > bBusyStart)
                    {
                        isBlocked = true;
                        break;
                    }
                }

                // Block if adding 1 more participant would exceed capacity
                if (totalAttendees >= service.MaxAttendees)
                    isBlocked = true;
            }
            else
            {
                // Standard 1:1 service — any overlap blocks
                foreach (var b in localizedBookings)
                {
                    var bBufferBefore = b.Service?.BufferBeforeMinutes ?? 0;
                    var bBufferAfter = b.Service?.BufferAfterMinutes ?? 0;
                    var bBusyStart = b.LocalStart.AddMinutes(-bBufferBefore);
                    var bBusyEnd = b.LocalEnd.AddMinutes(bBufferAfter);

                    if (myBusyStart < bBusyEnd && myBusyEnd > bBusyStart)
                    {
                        isBlocked = true;
                        break;
                    }
                }
            }

            if (isBlocked)
            {
                currentTime = currentTime.AddMinutes(15);
                continue;
            }

            // Check Staff Holds
            foreach (var h in localizedHolds)
            {
                // Holds are usually for 15 min slots, but must check overlap with myBusy [Start-Before, End+After]
                if (slotStart == h.LocalStart)
                {
                    isBlocked = true;
                    break;
                }
            }

            if (isBlocked)
            {
                currentTime = currentTime.AddMinutes(15);
                continue;
            }

            // Check Staff Exceptions (partial day)
            if (exception != null && !exception.IsAllDay && exception.StartTime.HasValue && exception.EndTime.HasValue)
            {
                var exStart = date.Date.Add(exception.StartTime.Value);
                var exEnd = date.Date.Add(exception.EndTime.Value);

                if (myBusyStart < exEnd && myBusyEnd > exStart)
                {
                    isBlocked = true;
                }
            }

            // Check Staff Breaks
            if (!isBlocked && workingHours.BreakStartTime.HasValue && workingHours.BreakEndTime.HasValue)
            {
                var breakStart = date.Date.Add(workingHours.BreakStartTime.Value);
                var breakEnd = date.Date.Add(workingHours.BreakEndTime.Value);

                if (myBusyStart < breakEnd && myBusyEnd > breakStart)
                {
                    isBlocked = true;
                }
            }

            if (isBlocked)
            {
                currentTime = currentTime.AddMinutes(15);
                continue;
            }

            // Check Hold Overlaps
            // Holds don't usually have "Buffer" info stored unless we looked up the service.
            // For now, assume Holds occupy the exact slot duration + maybe standard buffer?
            // Let's assume strict overlap on the hold time itself.
            foreach (var h in holds)
            {
                var hStart = h.SlotDateTime;
                var hEnd = h.SlotDateTime.AddMinutes(h.DurationMinutes);

                // We simplify holds to exact times for now as we don't store buffers on holds easily without lookup
                if (myBusyStart < hEnd && myBusyEnd > hStart)
                {
                    isBlocked = true;
                    break;
                }
            }

            if (isBlocked)
            {
                currentTime = currentTime.AddMinutes(15);
                continue;
            }

            // Check Breaks
            if (workingHours.BreakStartTime.HasValue && workingHours.BreakEndTime.HasValue)
            {
                var breakStart = date.Date.Add(workingHours.BreakStartTime.Value);
                var breakEnd = date.Date.Add(workingHours.BreakEndTime.Value);

                // Service (including buffers) cannot overlap break
                if (myBusyStart < breakEnd && myBusyEnd > breakStart)
                {
                    isBlocked = true;
                }
            }

            if (!isBlocked)
            {
                slots.Add(slotStart);
            }

            currentTime = currentTime.AddMinutes(15); // Interval step
        }

        return slots;
    }

    public async Task<bool> IsSlotAvailableAsync(Guid tenantId, Guid serviceId, Guid? staffId, DateTime startTime, int durationMinutes)
    {
        // Fast path: read the 96-char mask directly instead of computing all available slots
        // for the day and scanning — avoids running GetAvailableSlotsAsync (Bookings + Holds queries).
        if (staffId.HasValue)
        {
            var dateOnly = DateOnly.FromDateTime(startTime);
            var avCache = await _context.AvailabilityCaches
                .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.StaffId == staffId.Value && c.Date == dateOnly);

            if (avCache != null && avCache.LastUpdatedAt >= DateTime.UtcNow.AddMinutes(-15))
            {
                var slotIndex = (int)(startTime.TimeOfDay.TotalMinutes / 15);
                var service = await _context.Services.FindAsync(serviceId);
                if (service != null && slotIndex >= 0 && slotIndex < 96)
                    return DoesServiceFitInCache(avCache.AvailableSlotsMask, slotIndex, service);
            }
        }

        // Fallback when cache is stale or no staffId: compute from live data.
        var slots = await GetAvailableSlotsAsync(tenantId, serviceId, staffId, startTime.Date);
        return slots.Any(s => s == startTime);
    }

    public async Task<SlotHold> CreateSlotHoldAsync(Guid tenantId, Guid serviceId, Guid staffId, DateTime slotDateTime, string sessionToken)
    {
        // Distributed Lock to prevent race condition on the same slot
        var resource = $"slot:{staffId}:{slotDateTime:yyyyMMddHHmm}";
        using var @lock = await _lockProvider.AcquireLockAsync(resource, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5));

        if (@lock == null)
        {
            throw new InvalidOperationException("This time slot is currently being reserved by another user. Please try again in a few seconds.");
        }

        // Add additional DB-level pessimistic lock on the cache to prevent any race condition between IsSlotAvailable and SaveChanges
        var dateOnly = DateOnly.FromDateTime(slotDateTime);
        AvailabilityCache? dbLock;
        if (_context.Database.IsNpgsql())
        {
            dbLock = await _context.AvailabilityCaches
                .FromSqlRaw("SELECT * FROM \"AvailabilityCaches\" WHERE \"StaffId\" = {0} AND \"Date\" = {1} FOR UPDATE", staffId, dateOnly)
                .FirstOrDefaultAsync();
        }
        else
        {
            dbLock = await _context.AvailabilityCaches
                .FirstOrDefaultAsync(c => c.StaffId == staffId && c.Date == dateOnly);
        }

        if (!await IsSlotAvailableAsync(tenantId, serviceId, staffId, slotDateTime, 0))
        {
            throw new InvalidOperationException("The requested time slot is no longer available.");
        }

        if (!await CheckConcurrencyLimitAsync(tenantId))
        {
            throw new InvalidOperationException("Tenant has reached the maximum number of concurrent bookings allowed by their subscription tier.");
        }

        var service = await _context.Services.FindAsync(serviceId);
        if (service == null) throw new Exception("Service not found");

        var hold = new SlotHold
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StaffId = staffId,
            ServiceId = serviceId,
            SlotDateTime = slotDateTime,
            DurationMinutes = service.DurationMinutes,
            SessionToken = sessionToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            IsReleased = false
        };

        _context.SlotHolds.Add(hold);
        await _context.SaveChangesAsync();

        // Update Cache
        await UpdateAvailabilityCacheAsync(tenantId, staffId, DateOnly.FromDateTime(slotDateTime));

        return hold;
    }

    public async Task ReleaseSlotHoldAsync(Guid holdId)
    {
        var hold = await _context.SlotHolds.FindAsync(holdId);
        if (hold != null)
        {
            hold.IsReleased = true;
            await _context.SaveChangesAsync();

            // Update Cache
            await UpdateAvailabilityCacheAsync(hold.TenantId, hold.StaffId, DateOnly.FromDateTime(hold.SlotDateTime));
        }
    }

    public async Task<bool> CheckConcurrencyLimitAsync(Guid tenantId)
    {
        // Cache the subscription limit with a 5-min TTL — the plan limit changes rarely
        // but CheckConcurrencyLimitAsync is called on every CreateSlotHold path.
        var limit = await _cache.GetOrSetAsync(tenantId, "concurrency_limit", async () =>
        {
            var mapping = await _context.Subscriptions
                .Where(s => s.TenantId == tenantId &&
                            (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing))
                .SelectMany(s => s.PricingPlan!.FeatureMappings)
                .Where(m => m.PricingFeature.Key == "max_concurrent_bookings")
                .Select(m => (int?)m.NumericLimit)
                .FirstOrDefaultAsync();

            return mapping ?? -1; // -1 = unlimited
        }, TimeSpan.FromMinutes(5));

        if (limit == -1) return true;

        var activeBookingCount = await _context.Bookings
            .CountAsync(b => b.TenantId == tenantId &&
                             b.Status != BookingStatus.Cancelled &&
                             b.Status != BookingStatus.Completed &&
                             b.EndTime > DateTime.UtcNow);

        return activeBookingCount < limit;
    }

    public async Task UpdateAvailabilityCacheAsync(Guid tenantId, Guid staffId, DateOnly date)
    {
        var dateTime = date.ToDateTime(TimeOnly.MinValue);

        // 1. Get raw slots (this logic is similar to CalculateStaffAvailabilityAsync but for all possible slots)
        // We reuse CalculateStaffAvailabilityAsync for a one-minute "mock" service to find free windows
        var mockService = new Service
        {
            DurationMinutes = 15,
            BufferBeforeMinutes = 0,
            BufferAfterMinutes = 0,
            MaxAttendees = 1
        };

        var availableStartTimes = await CalculateStaffAvailabilityAsync(tenantId, staffId, mockService, dateTime);
        var availableHashSet = availableStartTimes.ToHashSet();

        // 2. Build 96-bit mask (15 min intervals)
        var mask = new char[96];
        for (int i = 0; i < 96; i++)
        {
            var slotTime = dateTime.AddMinutes(i * 15);
            mask[i] = availableHashSet.Contains(slotTime) ? '1' : '0';
        }

        var maskString = new string(mask);

        // 3. Upsert
        var cache = await _context.AvailabilityCaches
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.StaffId == staffId && c.Date == date);

        if (cache == null)
        {
            cache = new AvailabilityCache
            {
                TenantId = tenantId,
                StaffId = staffId,
                Date = date
            };
            _context.AvailabilityCaches.Add(cache);
        }

        cache.AvailableSlotsMask = maskString;
        cache.LastUpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Broadcast change event
        await _eventService.PublishAsync("scheduling.availability_changed", new
        {
            StaffId = staffId,
            Date = cache.Date,
            Timestamp = DateTime.UtcNow
        }, tenantId);
    }

    public async Task InvalidateStaffCacheAsync(Guid tenantId, Guid staffId, DateOnly? date = null)
    {
        var query = _context.AvailabilityCaches
            .Where(c => c.TenantId == tenantId && c.StaffId == staffId);

        if (date.HasValue)
        {
            query = query.Where(c => c.Date == date.Value);
        }

        var caches = await query.ToListAsync();

        if (caches.Any())
        {
            _context.AvailabilityCaches.RemoveRange(caches);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Invalidated availability cache for staff {StaffId} in tenant {TenantId} (Date: {Date})",
                staffId, tenantId, date?.ToString() ?? "All");

            // Broadcast change event
            foreach (var cache in caches)
            {
                await _eventService.PublishAsync("scheduling.availability_changed", new
                {
                    StaffId = staffId,
                    Date = cache.Date,
                    Timestamp = DateTime.UtcNow
                }, tenantId);
            }
        }
    }
    public async Task<List<DateTime>> GenerateRecurrenceDatesAsync(Guid tenantId, string frequency, int interval, DateTime startDate, DateTime? endDate, int? occurrences, List<int>? daysOfWeek)
    {
        var candidateDates = new List<DateTime>();
        var currentDate = startDate.Date;
        var count = 0;
        var maxToGenerate = Math.Min(occurrences ?? 100, 100); // Safety limit

        if (interval <= 0) interval = 1;

        if (frequency.Equals("Daily", StringComparison.OrdinalIgnoreCase))
        {
            while (count < maxToGenerate && (!endDate.HasValue || currentDate <= endDate.Value.Date))
            {
                candidateDates.Add(currentDate);
                currentDate = currentDate.AddDays(interval);
                count++;
            }
        }
        else if (frequency.Equals("Weekly", StringComparison.OrdinalIgnoreCase))
        {
            if (daysOfWeek != null && daysOfWeek.Any())
            {
                // Align to the start of the week of the start date (assuming Sunday is 0)
                var weekStartDate = currentDate.AddDays(-(int)currentDate.DayOfWeek);

                while (count < maxToGenerate)
                {
                    foreach (var day in daysOfWeek.OrderBy(d => d))
                    {
                        var targetDate = weekStartDate.AddDays(day);
                        if (targetDate < startDate.Date) continue;
                        if (endDate.HasValue && targetDate > endDate.Value.Date) break;
                        if (count >= maxToGenerate) break;

                        candidateDates.Add(targetDate);
                        count++;
                    }

                    weekStartDate = weekStartDate.AddDays(7 * interval);

                    // Termination conditions
                    if (endDate.HasValue && weekStartDate > endDate.Value.Date) break;
                    if (count >= maxToGenerate) break;
                }
            }
            else
            {
                while (count < maxToGenerate && (!endDate.HasValue || currentDate <= endDate.Value.Date))
                {
                    candidateDates.Add(currentDate);
                    currentDate = currentDate.AddDays(7 * interval);
                    count++;
                }
            }
        }
        else if (frequency.Equals("Monthly", StringComparison.OrdinalIgnoreCase))
        {
            while (count < maxToGenerate && (!endDate.HasValue || currentDate <= endDate.Value.Date))
            {
                candidateDates.Add(currentDate);
                currentDate = currentDate.AddMonths(interval);
                count++;
            }
        }

        return await Task.FromResult(candidateDates);
    }
}
