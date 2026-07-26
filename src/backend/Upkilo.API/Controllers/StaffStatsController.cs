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
    public class StaffStatsController : ControllerBase
    {
        private readonly ICommissionService _commissionService;
        private readonly ITenantProvider _tenantProvider;
        private readonly AppDbContext _context;

        public StaffStatsController(
            ICommissionService commissionService,
            ITenantProvider tenantProvider,
            AppDbContext context)
        {
            _commissionService = commissionService;
            _tenantProvider = tenantProvider;
            _context = context;
        }

        private Guid GetTenantId() => _tenantProvider.GetTenantId()
            ?? throw new UnauthorizedAccessException("Tenant context not available");

        /// <summary>
        /// Get earnings for the current staff member
        /// </summary>
        [HttpGet("my-earnings")]
        public async Task<IActionResult> GetMyEarnings([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var userId = _tenantProvider.GetUserId();
            var staff = await _context.Set<StaffMember>()
                .FirstOrDefaultAsync(s => s.TenantId == GetTenantId() && s.UserId == userId);

            if (staff == null) return NotFound("Staff profile not found");

            var earnings = await _commissionService.GetStaffEarningsAsync(staff.Id, from, to);
            return Ok(earnings);
        }

        /// <summary>
        /// [Owner Only] Get commission summary for the entire business
        /// </summary>
        [HttpGet("summary")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> GetSummary([FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            var summary = await _commissionService.GetCommissionStatsAsync(GetTenantId(), start, end);
            return Ok(summary);
        }

        /// <summary>
        /// [Owner Only] Get all pending commissions that need approval
        /// </summary>
        [HttpGet("pending")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> GetPendingCommissions()
        {
            var pending = await _context.Set<StaffCommission>()
                .Where(c => c.TenantId == GetTenantId() && c.Status == CommissionStatus.Pending)
                .OrderByDescending(c => c.CreatedAt)
                .Include(c => c.Staff)
                .Include(c => c.Booking)
                .ToListAsync();

            return Ok(pending);
        }

        /// <summary>
        /// [Owner Only] Approve a list of commissions for payout
        /// </summary>
        [HttpPost("approve")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> ApproveCommissions([FromBody] List<Guid> commissionIds)
        {
            await _commissionService.ApproveCommissionsAsync(commissionIds);
            return Ok(new { message = "Commissions approved" });
        }
    }
}
