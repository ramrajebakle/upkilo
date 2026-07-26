using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class ConsentService : IConsentService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ConsentService> _logger;

    public ConsentService(AppDbContext context, ILogger<ConsentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ConsentStatus> GetConsentStatusAsync(Guid tenantId, Guid clientId, string consentType)
    {
        var consent = await _context.GdprConsents
            .Where(c => c.TenantId == tenantId && c.ClientId == clientId && c.ConsentType == consentType)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        if (consent == null) return ConsentStatus.NotRecorded;
        return consent.IsGranted ? ConsentStatus.Granted : ConsentStatus.Revoked;
    }

    public async Task<bool> RecordConsentAsync(Guid tenantId, Guid clientId, string consentType, bool granted, string? ipAddress = null)
    {
        var consent = new GdprConsent
        {
            TenantId = tenantId,
            ClientId = clientId,
            ConsentType = consentType,
            IsGranted = granted,
            ProcessedAt = DateTime.UtcNow,
            IpAddress = ipAddress
        };

        _context.GdprConsents.Add(consent);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Consent {Type} {Action} for client {ClientId} in tenant {TenantId}",
            consentType, granted ? "granted" : "revoked", clientId, tenantId);
        return true;
    }

    public async Task<bool> RevokeConsentAsync(Guid tenantId, Guid clientId, string consentType)
    {
        // Record a new revocation entry
        var consent = new GdprConsent
        {
            TenantId = tenantId,
            ClientId = clientId,
            ConsentType = consentType,
            IsGranted = false,
            ProcessedAt = DateTime.UtcNow
        };

        _context.GdprConsents.Add(consent);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<GdprConsent>> GetAllConsentsAsync(Guid tenantId, Guid clientId)
    {
        return await _context.GdprConsents
            .Where(c => c.TenantId == tenantId && c.ClientId == clientId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> AcceptDpaAsync(Guid tenantId, Guid userId, string dpaVersion)
    {
        var existing = await _context.LegalAgreements
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.AgreementType == "DPA" && a.Version == dpaVersion);

        if (existing != null) return true;

        _context.LegalAgreements.Add(new LegalAgreement
        {
            TenantId = tenantId,
            AgreementType = "DPA",
            Version = dpaVersion,
            AcceptedByUserId = userId,
            AcceptedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        _logger.LogInformation("DPA v{Version} accepted by user {UserId} for tenant {TenantId}", dpaVersion, userId, tenantId);
        return true;
    }

    public async Task<bool> IsDpaAcceptedAsync(Guid tenantId)
    {
        var latestDpa = await GetLatestDpaAsync();
        if (latestDpa == null) return true;

        return await _context.LegalAgreements
            .AnyAsync(a => a.TenantId == tenantId && a.AgreementType == "DPA" && a.Version == latestDpa.Version);
    }

    public async Task<LegalAgreement?> GetLatestDpaAsync()
    {
        return await _context.LegalAgreements
            .Where(a => a.AgreementType == "DPA_TEMPLATE")
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();
    }
}
