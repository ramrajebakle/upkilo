using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

/// <summary>
/// Manages GDPR consent tracking and DPA acceptance workflows
/// </summary>
public interface IConsentService
{
    Task<ConsentStatus> GetConsentStatusAsync(Guid tenantId, Guid clientId, string consentType);
    Task<bool> RecordConsentAsync(Guid tenantId, Guid clientId, string consentType, bool granted, string? ipAddress = null);
    Task<bool> RevokeConsentAsync(Guid tenantId, Guid clientId, string consentType);
    Task<IReadOnlyList<GdprConsent>> GetAllConsentsAsync(Guid tenantId, Guid clientId);
    Task<bool> AcceptDpaAsync(Guid tenantId, Guid userId, string dpaVersion);
    Task<bool> IsDpaAcceptedAsync(Guid tenantId);
    Task<LegalAgreement?> GetLatestDpaAsync();
}

public enum ConsentStatus
{
    NotRecorded,
    Granted,
    Revoked,
    Expired
}
