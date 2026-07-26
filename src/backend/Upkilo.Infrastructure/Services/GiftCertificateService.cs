using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using System.Security.Cryptography;

namespace Upkilo.Infrastructure.Services;

public class GiftCertificateService : IGiftCertificateService
{
    private readonly AppDbContext _context;
    private readonly ILogger<GiftCertificateService> _logger;

    public GiftCertificateService(AppDbContext context, ILogger<GiftCertificateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<GiftCertificate> IssueGiftCertificateAsync(
        Guid tenantId,
        decimal amount,
        string? recipientEmail = null,
        string? senderName = null,
        string? message = null,
        DateTime? expiryDate = null,
        Guid? clientId = null)
    {
        var code = await GenerateUniqueCodeAsync(tenantId);

        var certificate = new GiftCertificate
        {
            TenantId = tenantId,
            Code = code,
            InitialAmount = amount,
            RemainingAmount = amount,
            Currency = "USD",
            ExpiryDate = expiryDate,
            Status = GiftCertificateStatus.Active,
            RecipientEmail = recipientEmail,
            SenderName = senderName,
            Message = message,
            ClientId = clientId
        };

        _context.GiftCertificates.Add(certificate);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Issued gift certificate {CertificateCode} for tenant {TenantId} with amount {Amount}", code, tenantId, amount);

        return certificate;
    }

    public async Task<GiftCertificate?> ValidateCodeAsync(Guid tenantId, string code)
    {
        var cert = await _context.GiftCertificates
            .Include(c => c.Redemptions)
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Code == code);

        if (cert == null) return null;

        // Check expiry
        if (cert.ExpiryDate.HasValue && cert.ExpiryDate < DateTime.UtcNow && cert.Status != GiftCertificateStatus.Expired)
        {
            cert.Status = GiftCertificateStatus.Expired;
            await _context.SaveChangesAsync();
        }

        return cert;
    }

    public async Task<bool> RedeemAmountAsync(Guid tenantId, string code, decimal amount, Guid? bookingId = null, string? notes = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var cert = await _context.GiftCertificates
                .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Code == code);

            if (cert == null || cert.Status == GiftCertificateStatus.FullyRedeemed || cert.Status == GiftCertificateStatus.Expired || cert.Status == GiftCertificateStatus.Void)
            {
                _logger.LogWarning("Attempted to redeem invalid or inactive gift certificate: {Code}", code);
                return false;
            }

            if (cert.RemainingAmount < amount)
            {
                _logger.LogWarning("Insufficient balance on gift certificate {Code}: {Remaining} < {Amount}", code, cert.RemainingAmount, amount);
                return false;
            }

            cert.RemainingAmount -= amount;
            
            if (cert.RemainingAmount == 0)
            {
                cert.Status = GiftCertificateStatus.FullyRedeemed;
            }
            else
            {
                cert.Status = GiftCertificateStatus.PartiallyRedeemed;
            }

            var redemption = new GiftCertificateRedemption
            {
                GiftCertificateId = cert.Id,
                BookingId = bookingId,
                AmountRedeemed = amount,
                RedeemedAt = DateTime.UtcNow,
                Notes = notes
            };

            _context.GiftCertificateRedemptions.Add(redemption);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Redeemed {Amount} from gift certificate {CertificateCode}", amount, code);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error redeeming gift certificate {CertificateCode}", code);
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task<IEnumerable<GiftCertificate>> GetTenantGiftCertificatesAsync(Guid tenantId)
    {
        return await _context.GiftCertificates
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<GiftCertificate?> GetByIdAsync(Guid id, Guid tenantId)
    {
        return await _context.GiftCertificates
            .Include(c => c.Redemptions)
            .Include(c => c.Client)
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId);
    }

    private async Task<string> GenerateUniqueCodeAsync(Guid tenantId)
    {
        string code;
        bool exists;
        do
        {
            code = GenerateRandomCode();
            exists = await _context.GiftCertificates.AnyAsync(c => c.Code == code);
        } while (exists);

        return code;
    }

    private string GenerateRandomCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new byte[8];
        RandomNumberGenerator.Fill(random);

        var result = new char[13];
        result[0] = 'U';
        result[1] = 'P';
        result[2] = 'K';
        result[3] = '-';
        
        for (int i = 0; i < 4; i++)
        {
            result[i + 4] = chars[random[i] % chars.Length];
        }
        result[8] = '-';
        for (int i = 0; i < 4; i++)
        {
            result[i + 9] = chars[random[i + 4] % chars.Length];
        }

        return new string(result);
    }
}
