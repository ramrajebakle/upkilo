using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AttendanceService> _logger;

        public AttendanceService(AppDbContext context, ILogger<AttendanceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<StaffClockIn> ClockInAsync(Guid tenantId, Guid staffId, string? ipAddress = null, string? latLong = null, string? device = null)
        {
            _logger.LogInformation("Staff {StaffId} clocking in", staffId);

            // Check if already clocked in
            var activeSession = await _context.Set<StaffClockIn>()
                .FirstOrDefaultAsync(c => c.StaffId == staffId && c.ClockOutTime == null);

            if (activeSession != null)
                throw new Exception("Staff already clocked in");

            // Find current shift if any
            var now = DateTime.UtcNow;
            var currentShift = await _context.Set<StaffShift>()
                .FirstOrDefaultAsync(s => s.StaffId == staffId && s.StartTime <= now && s.EndTime >= now);

            var clockIn = new StaffClockIn
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                StaffId = staffId,
                ClockInTime = now,
                IpAddress = ipAddress,
                LatLong = latLong,
                Device = device,
                ShiftId = currentShift?.Id,
                CreatedAt = now
            };

            _context.Set<StaffClockIn>().Add(clockIn);
            await _context.SaveChangesAsync();

            return clockIn;
        }

        public async Task<StaffClockIn> ClockOutAsync(Guid staffId)
        {
            _logger.LogInformation("Staff {StaffId} clocking out", staffId);

            var activeSession = await _context.Set<StaffClockIn>()
                .OrderByDescending(c => c.ClockInTime)
                .FirstOrDefaultAsync(c => c.StaffId == staffId && c.ClockOutTime == null);

            if (activeSession == null)
                throw new Exception("No active clock-in session found");

            activeSession.ClockOutTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return activeSession;
        }

        public async Task<IEnumerable<StaffClockIn>> GetStaffTimesheetAsync(Guid staffId, DateTime start, DateTime end)
        {
            return await _context.Set<StaffClockIn>()
                .Where(c => c.StaffId == staffId && c.ClockInTime >= start && c.ClockInTime <= end)
                .OrderByDescending(c => c.ClockInTime)
                .ToListAsync();
        }

        public async Task<AttendanceStats> GetAttendanceStatsAsync(Guid tenantId, DateTime start, DateTime end)
        {
            var sessions = await _context.Set<StaffClockIn>()
                .Where(c => c.TenantId == tenantId && c.ClockInTime >= start && c.ClockInTime <= end)
                .ToListAsync();

            var stats = new AttendanceStats
            {
                TotalClockIns = sessions.Count,
                TotalHoursWorked = 0,
                HoursByStaff = new Dictionary<Guid, double>()
            };

            foreach (var s in sessions)
            {
                if (s.ClockOutTime.HasValue)
                {
                    var hours = (s.ClockOutTime.Value - s.ClockInTime).TotalHours;
                    stats.TotalHoursWorked += hours;

                    if (!stats.HoursByStaff.ContainsKey(s.StaffId))
                        stats.HoursByStaff[s.StaffId] = 0;
                    
                    stats.HoursByStaff[s.StaffId] += hours;
                }
            }

            return stats;
        }
    }
}
