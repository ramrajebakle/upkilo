using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Stripe;
using Microsoft.EntityFrameworkCore;

namespace Upkilo.Infrastructure.Services
{
    public class PayoutService : IPayoutService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PayoutService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISecretProvider _secretProvider;

        public PayoutService(
            AppDbContext context,
            ILogger<PayoutService> logger,
            IConfiguration configuration,
            ISecretProvider secretProvider)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _secretProvider = secretProvider;

            StripeConfiguration.ApiKey = _secretProvider.GetSecret("Stripe--SecretKey");
        }

        public async Task<string> GetStaffOnboardingUrlAsync(Guid tenantId, Guid staffId, string returnUrl)
        {
            var staff = await _context.Set<StaffMember>().FindAsync(staffId);
            if (staff == null) throw new Exception("Staff member not found");

            if (string.IsNullOrEmpty(staff.StripeConnectId))
            {
                var options = new AccountCreateOptions
                {
                    Type = "express",
                    Email = staff.Email,
                    Metadata = new Dictionary<string, string>
                    {
                        { "tenant_id", tenantId.ToString() },
                        { "staff_id", staffId.ToString() }
                    }
                };

                var service = new AccountService();
                var account = await service.CreateAsync(options);
                staff.StripeConnectId = account.Id;
                await _context.SaveChangesAsync();
            }

            var linkOptions = new AccountLinkCreateOptions
            {
                Account = staff.StripeConnectId,
                RefreshUrl = returnUrl,
                ReturnUrl = returnUrl,
                Type = "account_onboarding",
            };

            var linkService = new AccountLinkService();
            var accountLink = await linkService.CreateAsync(linkOptions);
            return accountLink.Url;
        }

        public async Task<PayoutResult> CreateStaffPayoutAsync(Guid tenantId, Guid staffId, decimal amount, string currency = "USD")
        {
            var staff = await _context.Set<StaffMember>().FindAsync(staffId);
            if (staff == null || string.IsNullOrEmpty(staff.StripeConnectId))
                return new PayoutResult { Success = false, Message = "Staff not setup for payouts" };

            try
            {
                // 1. Create Transfer to Connected Account
                var transferOptions = new TransferCreateOptions
                {
                    // Exponent-aware: a flat *100 over-pays staff 100x in a zero-decimal currency.
                    Amount = Upkilo.Core.Helpers.Currency.ToMinorUnits(amount, currency),
                    Currency = Upkilo.Core.Helpers.Currency.Normalize(currency).ToLowerInvariant(),
                    Destination = staff.StripeConnectId,
                    Description = $"Payout for earnings for {staff.FirstName} {staff.LastName}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "tenant_id", tenantId.ToString() },
                        { "staff_id", staffId.ToString() }
                    }
                };

                var transferService = new TransferService();
                var transfer = await transferService.CreateAsync(transferOptions);

                // 2. Create Payout from Connected Account to Bank (Automated in Express usually, but can be manual)
                // For this implementation, we assume a "Destination Charge" or "Separate Charges and Transfers" pattern.

                var payoutRecord = new StripePayout
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    StaffId = staffId,
                    Amount = amount,
                    Currency = currency,
                    StripeTransferId = transfer.Id,
                    Status = "paid",
                    GeneratedAt = DateTime.UtcNow // From BaseEntity
                };

                _context.Set<StripePayout>().Add(payoutRecord);
                await _context.SaveChangesAsync();

                return new PayoutResult { Success = true, Message = "Payout successful", StripePayoutId = transfer.Id };
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe payout failed for staff {StaffId}", staffId);
                return new PayoutResult { Success = false, Message = ex.Message };
            }
        }

        public async Task<IEnumerable<StripePayout>> GetStaffPayoutHistoryAsync(Guid staffId)
        {
            return await _context.Set<StripePayout>()
                .Where(p => p.StaffId == staffId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<PayoutResult> ProcessCommissionPayoutsAsync(Guid tenantId)
        {
            // Implementation for bulk processing pending commissions
            // Find all pending commissions for this tenant
            var pendingCommissions = await _context.Set<StaffCommission>()
                .Where(c => c.TenantId == tenantId && c.Status == CommissionStatus.Approved)
                .GroupBy(c => c.StaffId)
                .ToListAsync();

            int processed = 0;
            foreach (var group in pendingCommissions)
            {
                var staffId = group.Key;
                var total = group.Sum(c => c.TotalEarned + c.TipAmount);

                if (total > 0)
                {
                    var result = await CreateStaffPayoutAsync(tenantId, staffId, total);
                    if (result.Success)
                    {
                        foreach (var comm in group)
                        {
                            comm.Status = CommissionStatus.Paid;
                            comm.PaidAt = DateTime.UtcNow;
                        }
                        processed++;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return new PayoutResult { Success = true, Message = $"Processed {processed} staff payouts" };
        }

        public async Task SyncPayoutStatusAsync(string stripePayoutId)
        {
            // Implementation for webhook updates
            var payout = await _context.Set<StripePayout>()
                .FirstOrDefaultAsync(p => p.StripePayoutId == stripePayoutId || p.StripeTransferId == stripePayoutId);

            if (payout != null)
            {
                // Update based on Stripe API call or webhook payload
                // ...
            }
        }
    }
}
