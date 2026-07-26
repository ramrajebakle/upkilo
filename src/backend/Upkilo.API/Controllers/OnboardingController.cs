using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;


namespace Upkilo.API.Controllers;

/// <summary>
/// Onboarding wizard: guides new tenants through initial setup.
/// Persists progress in TenantOnboardingProgress table.
/// Auto-detects step completion from existing data.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/onboarding")]
[Authorize]
public class OnboardingController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<OnboardingController> _logger;

    public OnboardingController(AppDbContext context, ITenantProvider tenantProvider, ILogger<OnboardingController> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get onboarding checklist with current progress (auto-detected from real data)
    /// </summary>
    [HttpGet("checklist")]
    public async Task<IActionResult> GetChecklist()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Get or create progress record
        var progress = await _context.Set<TenantOnboardingProgress>()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId.Value);

        if (progress == null)
        {
            progress = new TenantOnboardingProgress
            {
                TenantId = tenantId.Value,
                UserId = Guid.Parse((User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value) ?? Guid.Empty.ToString())
            };
            _context.Set<TenantOnboardingProgress>().Add(progress);
        }

        // Auto-detect completion from actual data
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        var hasServices = await _context.Services.AnyAsync(s => s.TenantId == tenantId);
        var hasStaff = await _context.StaffMembers.AnyAsync(s => s.TenantId == tenantId);
        var hasBookings = await _context.Bookings.AnyAsync(b => b.TenantId == tenantId);
        var hasClients = await _context.Clients.AnyAsync(c => c.TenantId == tenantId);
        var hasUsedAi = await _context.AIUsageLogs.AnyAsync(a => a.TenantId == tenantId);
        DateTime? firstAiUsedAt = hasUsedAi
            ? await _context.AIUsageLogs
                .Where(a => a.TenantId == tenantId)
                .MinAsync(a => a.CreatedAt)
            : null;

        // Update auto-detected steps
        if (!progress.BusinessProfileCompleted && tenant?.BusinessType != null)
        {
            progress.BusinessProfileCompleted = true;
            progress.BusinessProfileCompletedAt = DateTime.UtcNow;
        }
        if (!progress.ServicesAdded && hasServices)
        {
            progress.ServicesAdded = true;
            progress.ServicesAddedAt = DateTime.UtcNow;
        }
        if (!progress.StaffAdded && hasStaff)
        {
            progress.StaffAdded = true;
            progress.StaffAddedAt = DateTime.UtcNow;
        }
        if (!progress.FirstBookingCreated && hasBookings)
        {
            progress.FirstBookingCreated = true;
            progress.FirstBookingCreatedAt = DateTime.UtcNow;
        }
        if (!progress.ClientsImported && hasClients)
        {
            progress.ClientsImported = true;
            progress.ClientsImportedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        var steps = new[]
        {
            new { id = "business_profile", title = "Set Up Business Profile",
                  description = "Configure your business name, address, timezone, and industry type.",
                  order = 1, completed = progress.BusinessProfileCompleted,
                  completedAt = progress.BusinessProfileCompletedAt, route = "/settings/business" },

            new { id = "working_hours", title = "Set Working Hours",
                  description = "Define your business hours and staff availability.",
                  order = 2, completed = progress.WorkingHoursCompleted,
                  completedAt = progress.WorkingHoursCompletedAt, route = "/settings/scheduling" },

            new { id = "add_services", title = "Add Your Services",
                  description = "Create the services you offer with pricing and duration.",
                  order = 3, completed = progress.ServicesAdded,
                  completedAt = progress.ServicesAddedAt, route = "/services" },

            new { id = "add_staff", title = "Add Team Members",
                  description = "Invite staff and assign them to services.",
                  order = 4, completed = progress.StaffAdded,
                  completedAt = progress.StaffAddedAt, route = "/staff" },

            new { id = "booking_page", title = "Customize Booking Page",
                  description = "Personalize your public booking page with your brand colors and logo.",
                  order = 5, completed = progress.BookingPageCustomized,
                  completedAt = progress.BookingPageCustomizedAt, route = "/settings/booking-page" },

            new { id = "payment_setup", title = "Connect Payment Method",
                  description = "Set up Stripe to accept online payments.",
                  order = 6, completed = progress.PaymentSetupCompleted,
                  completedAt = progress.PaymentSetupCompletedAt, route = "/settings/billing" },

            new { id = "first_booking", title = "Create Your First Booking",
                  description = "Test the system by creating a booking.",
                  order = 7, completed = progress.FirstBookingCreated,
                  completedAt = progress.FirstBookingCreatedAt, route = "/bookings/new" },

            new { id = "invite_client", title = "Import or Add Clients",
                  description = "Add your existing client base via CSV import or manually.",
                  order = 8, completed = progress.ClientsImported,
                  completedAt = progress.ClientsImportedAt, route = "/clients" },

            new { id = "ai_copilot_quickwin", title = "Try AI Copilot (Quick Win)",
                  description = "Ask the AI to write a client follow-up message or suggest a promotion. Most owners save 2+ hours on Day 1.",
                  order = 9, completed = hasUsedAi,
                  completedAt = firstAiUsedAt, route = "/ai" },
        };

        var completedCount = steps.Count(s => s.completed);

        return Ok(new
        {
            completionPercentage = (int)((completedCount / (double)steps.Length) * 100),
            completedSteps = completedCount,
            totalSteps = steps.Length,
            isDismissed = progress.IsDismissed,
            sampleDataTemplate = progress.SampleDataTemplate,
            steps
        });
    }

    /// <summary>
    /// Mark an onboarding step as completed
    /// </summary>
    [HttpPost("checklist/{stepId}/complete")]
    public async Task<IActionResult> CompleteStep(string stepId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var progress = await _context.Set<TenantOnboardingProgress>()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId.Value);

        if (progress == null) return NotFound("Onboarding progress not found");

        var now = DateTime.UtcNow;
        switch (stepId)
        {
            case "business_profile": progress.BusinessProfileCompleted = true; progress.BusinessProfileCompletedAt = now; break;
            case "working_hours": progress.WorkingHoursCompleted = true; progress.WorkingHoursCompletedAt = now; break;
            case "add_services": progress.ServicesAdded = true; progress.ServicesAddedAt = now; break;
            case "add_staff": progress.StaffAdded = true; progress.StaffAddedAt = now; break;
            case "booking_page": progress.BookingPageCustomized = true; progress.BookingPageCustomizedAt = now; break;
            case "payment_setup": progress.PaymentSetupCompleted = true; progress.PaymentSetupCompletedAt = now; break;
            case "first_booking": progress.FirstBookingCreated = true; progress.FirstBookingCreatedAt = now; break;
            case "invite_client": progress.ClientsImported = true; progress.ClientsImportedAt = now; break;
            // ai_copilot_quickwin is auto-detected from AIUsageLogs — no stored flag needed;
            // accepting the call here prevents frontend errors when manually marking it complete.
            case "ai_copilot_quickwin": break;
            default: return BadRequest($"Unknown step: {stepId}");
        }

        progress.UpdatedAt = now;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Onboarding step '{StepId}' completed for tenant {TenantId}", stepId, tenantId);

        return Ok(new { stepId, completedAt = now, message = $"Step '{stepId}' completed" });
    }

    /// <summary>
    /// Skip an onboarding step
    /// </summary>
    [HttpPost("checklist/{stepId}/skip")]
    public IActionResult SkipStep(string stepId)
    {
        return Ok(new { stepId, skippedAt = DateTime.UtcNow, message = $"Step '{stepId}' skipped" });
    }

    /// <summary>
    /// Get sample data templates
    /// </summary>
    [HttpGet("sample-data")]
    public IActionResult GetSampleDataOptions()
    {
        return Ok(new
        {
            options = new[]
            {
                new { id = "spa", name = "Spa & Wellness Demo", description = "5 services, 3 staff, 20 clients, 15 bookings" },
                new { id = "salon", name = "Hair Salon Demo", description = "8 services, 4 staff, 25 clients, 20 bookings" },
                new { id = "dental", name = "Dental Clinic Demo", description = "6 services, 2 staff, 15 clients, 10 bookings" },
                new { id = "fitness", name = "Fitness Studio Demo", description = "10 classes, 5 trainers, 30 clients, 25 bookings" },
                new { id = "consulting", name = "Consulting Firm Demo", description = "4 services, 3 consultants, 12 clients, 8 bookings" }
            }
        });
    }

    /// <summary>
    /// Seed sample data for the tenant
    /// </summary>
    [HttpPost("sample-data/{templateId}")]
    public async Task<IActionResult> SeedSampleData(string templateId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var progress = await _context.Set<TenantOnboardingProgress>()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId.Value);

        if (progress != null)
        {
            progress.SampleDataTemplate = templateId;
            await _context.SaveChangesAsync();
        }

        // Enqueue Hangfire job for demo data seeding
        Hangfire.BackgroundJob.Enqueue<Upkilo.API.Jobs.SampleDataSeedJob>(
            job => job.ExecuteAsync(tenantId.Value, templateId));

        return Ok(new { templateId, message = $"Sample data for '{templateId}' is being generated.", status = "processing" });
    }

    /// <summary>
    /// Dismiss onboarding for the current tenant
    /// </summary>
    [HttpPost("dismiss")]
    public async Task<IActionResult> DismissOnboarding()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var progress = await _context.Set<TenantOnboardingProgress>()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId.Value);

        if (progress != null)
        {
            progress.IsDismissed = true;
            progress.DismissedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "Onboarding dismissed.", dismissedAt = DateTime.UtcNow });
    }

    /// <summary>
    /// Returns upgrade nudge context for the current tenant.
    /// Used by the in-app upgrade CTA banner — fires when approaching Free plan limits.
    /// </summary>
    [HttpGet("upgrade-nudge")]
    public async Task<IActionResult> GetUpgradeNudge()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var clientCount = await _context.Clients
            .CountAsync(c => c.TenantId == tenantId.Value && !c.IsDeleted);
        var bookingCount = await _context.Bookings
            .CountAsync(b => b.TenantId == tenantId.Value && !b.IsDeleted);
        var staffCount = await _context.StaffMembers
            .CountAsync(s => s.TenantId == tenantId.Value && !s.IsDeleted);

        const int freeClientLimit = 100;
        const int freeStaffLimit = 1;
        const int freeBookingMonthlyLimit = 50;

        var nudges = new List<object>();

        if (clientCount >= (int)(freeClientLimit * 0.8))
        {
            nudges.Add(new
            {
                type = "client_limit",
                current = clientCount,
                limit = freeClientLimit,
                percent = Math.Min(100, (int)((double)clientCount / freeClientLimit * 100)),
                message = $"You have {clientCount}/{freeClientLimit} clients on the Free plan.",
                ctaText = "Upgrade to Starter for 500 clients",
                ctaUrl = "/settings/billing?upgrade=starter",
                urgency = clientCount >= freeClientLimit ? "blocking" : "warning"
            });
        }

        if (staffCount >= freeStaffLimit)
        {
            nudges.Add(new
            {
                type = "staff_limit",
                current = staffCount,
                limit = freeStaffLimit,
                percent = 100,
                message = "Free plan includes 1 staff member.",
                ctaText = "Upgrade to add more team members",
                ctaUrl = "/settings/billing?upgrade=starter",
                urgency = "warning"
            });
        }

        return Ok(new
        {
            showNudge = nudges.Count > 0,
            nudges,
            trialSuggestion = nudges.Count > 0
                ? "Upgrade to Starter for $39/mo — start 14-day free trial"
                : null
        });
    }
}
