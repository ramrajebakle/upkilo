using Upkilo.Core.Entities;

namespace Upkilo.Core.Interfaces;

public interface IGiftCertificateService
{
    Task<GiftCertificate> IssueGiftCertificateAsync(
        Guid tenantId, 
        decimal amount, 
        string? recipientEmail = null, 
        string? senderName = null, 
        string? message = null,
        DateTime? expiryDate = null,
        Guid? clientId = null);

    Task<GiftCertificate?> ValidateCodeAsync(Guid tenantId, string code);

    Task<bool> RedeemAmountAsync(Guid tenantId, string code, decimal amount, Guid? bookingId = null, string? notes = null);

    Task<IEnumerable<GiftCertificate>> GetTenantGiftCertificatesAsync(Guid tenantId);
    
    Task<GiftCertificate?> GetByIdAsync(Guid id, Guid tenantId);
}
