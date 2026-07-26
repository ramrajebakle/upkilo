using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;

namespace Upkilo.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class StaffPayoutController : ControllerBase
    {
        private readonly IPayoutService _payoutService;
        private readonly ITenantProvider _tenantProvider;
        private readonly AppDbContext _context;

        public StaffPayoutController(
            IPayoutService payoutService,
            ITenantProvider tenantProvider,
            AppDbContext context)
        {
            _payoutService = payoutService;
            _tenantProvider = tenantProvider;
            _context = context;
        }

        private Guid GetTenantId() => _tenantProvider.GetTenantId()
            ?? throw new UnauthorizedAccessException("Tenant context not available");

        /// <summary>
        /// Get payout onboarding URL for the current user's staff profile
        /// </summary>
        [HttpPost("onboarding-url")]
        public async Task<IActionResult> GetOnboardingUrl([FromBody] OnboardingUrlRequest request)
        {
            var userId = _tenantProvider.GetUserId();
            var staff = await _context.Set<StaffMember>()
                .FirstOrDefaultAsync(s => s.TenantId == GetTenantId() && s.UserId == userId);

            if (staff == null) return NotFound("Staff profile not found");

            var url = await _payoutService.GetStaffOnboardingUrlAsync(GetTenantId(), staff.Id, request.ReturnUrl);
            return Ok(new { url });
        }

        /// <summary>
        /// Get payout history for the current staff member
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetPayoutHistory()
        {
            var userId = _tenantProvider.GetUserId();
            var staff = await _context.Set<StaffMember>()
                .FirstOrDefaultAsync(s => s.TenantId == GetTenantId() && s.UserId == userId);

            if (staff == null) return NotFound("Staff profile not found");

            var history = await _payoutService.GetStaffPayoutHistoryAsync(staff.Id);
            return Ok(history);
        }

        /// <summary>
        /// [Owner Only] Process all approved commissions for the tenant
        /// </summary>
        [HttpPost("process-commissions")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> ProcessCommissions()
        {
            var result = await _payoutService.ProcessCommissionPayoutsAsync(GetTenantId());
            if (!result.Success) return BadRequest(result.Message);
            return Ok(result);
        }

        /// <summary>
        /// [Owner Only] Get all payouts for the tenant
        /// </summary>
        [HttpGet("all-payouts")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> GetAllPayouts()
        {
            var payouts = await _context.Set<StripePayout>()
                .Where(p => p.TenantId == GetTenantId())
                .OrderByDescending(p => p.CreatedAt)
                .Include(p => p.Staff)
                .ToListAsync();

            return Ok(payouts);
        }
    }

    public class OnboardingUrlRequest
    {
        public string ReturnUrl { get; set; } = string.Empty;
    }
}
