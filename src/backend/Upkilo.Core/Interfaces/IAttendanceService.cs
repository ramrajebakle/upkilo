using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces
{
    public interface IAttendanceService
    {
        Task<StaffClockIn> ClockInAsync(Guid tenantId, Guid staffId, string? ipAddress = null, string? latLong = null, string? device = null);
        Task<StaffClockIn> ClockOutAsync(Guid staffId);
        Task<IEnumerable<StaffClockIn>> GetStaffTimesheetAsync(Guid staffId, DateTime start, DateTime end);
        Task<AttendanceStats> GetAttendanceStatsAsync(Guid tenantId, DateTime start, DateTime end);
    }

    public class AttendanceStats
    {
        public int TotalClockIns { get; set; }
        public double TotalHoursWorked { get; set; }
        public Dictionary<Guid, double> HoursByStaff { get; set; } = new();
    }
}
