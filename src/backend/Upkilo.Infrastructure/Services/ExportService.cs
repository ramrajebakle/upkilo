using System.Text;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

public class ExportService : IExportService
{
    private readonly AppDbContext _context;

    public ExportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<byte[]> ExportClientsToCsvAsync(Guid tenantId)
    {
        var clients = await _context.Clients
            .Where(c => c.TenantId == tenantId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var sb = new StringBuilder();
        // Header
        sb.AppendLine("Id,FirstName,LastName,Email,Phone,TotalBookings,LifetimeValue,LoyaltyPoints,LoyaltyTier,CreatedAt,LastBookingAt");

        foreach (var client in clients)
        {
            sb.AppendLine($"{client.Id},{Escape(client.FirstName)},{Escape(client.LastName)},{Escape(client.Email)},{Escape(client.Phone)},{client.TotalBookings},{client.LifetimeValue},{client.LoyaltyPoints},{client.LoyaltyTier},{client.CreatedAt:O},{client.LastBookingAt:O}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportBookingsToCsvAsync(Guid tenantId)
    {
        var bookings = await _context.Bookings
            .Include(b => b.Client)
            .Include(b => b.Staff)
            .Include(b => b.Service)
            .Where(b => b.TenantId == tenantId)
            .OrderByDescending(b => b.StartTime)
            .Take(10000) // Safety limit
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Id,ClientName,ClientEmail,ServiceName,StaffName,StartTime,EndTime,Status,Price,PaymentStatus");

        foreach (var b in bookings)
        {
            var clientName = b.Client != null ? $"{b.Client.FirstName} {b.Client.LastName}" : "Unknown";
            var clientEmail = b.Client?.Email ?? "";
            var staffName = b.Staff?.FirstName ?? "Unknown";
            var serviceName = b.Service?.Name ?? "Unknown";

            sb.AppendLine($"{b.Id},{Escape(clientName)},{Escape(clientEmail)},{Escape(serviceName)},{Escape(staffName)},{b.StartTime:O},{b.EndTime:O},{b.Status},{b.Price},{b.PaymentStatus}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
