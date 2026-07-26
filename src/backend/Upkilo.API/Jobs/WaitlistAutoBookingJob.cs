using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Jobs;

/// <summary>
/// Background job that monitors waitlist entries and notifies clients when a slot becomes available.
/// </summary>
public class WaitlistAutoBookingJob
{
    private readonly AppDbContext _context;
    private readonly ISchedulingService _schedulingService;
    private readonly IEmailService _emailService;
    private readonly ILogger<WaitlistAutoBookingJob> _logger;
    private readonly IConfiguration _configuration;

    public WaitlistAutoBookingJob(
        AppDbContext context,
        ISchedulingService schedulingService,
        IEmailService emailService,
        ILogger<WaitlistAutoBookingJob> logger,
        IConfiguration configuration)
    {
        _context = context;
        _schedulingService = schedulingService;
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting WaitlistAutoBookingJob execution at {Time}", DateTime.UtcNow);

        // 1. Get all waiting entries for the future
        var waitingEntries = await _context.WaitlistEntries
            .Include(w => w.Service)
            .Include(w => w.Tenant)
            .Where(w => w.Status == WaitlistStatus.Waiting && w.PreferredDate >= DateTime.UtcNow.Date)
            .OrderBy(w => w.Priority)
            .ThenBy(w => w.RequestedDate)
            .ToListAsync();

        if (!waitingEntries.Any())
        {
            _logger.LogInformation("No waiting entries found to process.");
            return;
        }

        foreach (var entry in waitingEntries)
        {
            try
            {
                // 2. Check for availability on the preferred date
                var availableSlots = await _schedulingService.GetAvailableSlotsAsync(
                    entry.TenantId, 
                    entry.ServiceId, 
                    entry.StaffId, 
                    entry.PreferredDate);

                if (availableSlots.Any())
                {
                    _logger.LogInformation("Available slots found for WaitlistEntry {Id} on {Date}", entry.Id, entry.PreferredDate);

                    // 3. Notify the client
                    var businessName = entry.Tenant?.Name ?? "Upkilo Business";
                    var frontendUrl = _configuration["App:FrontendUrl"];
                    var bookingLink = $"{frontendUrl}/book?tenantId={entry.TenantId}&serviceId={entry.ServiceId}&date={entry.PreferredDate:yyyy-MM-dd}";

                    await _emailService.SendWaitlistNotificationAsync(new WaitlistEmailData(
                        entry.Email,
                        $"{entry.FirstName} {entry.LastName}",
                        entry.Service?.Name ?? "Service",
                        entry.PreferredDate,
                        bookingLink,
                        businessName
                    ));

                    // 4. Update status to Notified
                    entry.Status = WaitlistStatus.Notified;
                    entry.UpdatedAt = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing WaitlistEntry {Id}", entry.Id);
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Completed WaitlistAutoBookingJob execution.");
    }
}
