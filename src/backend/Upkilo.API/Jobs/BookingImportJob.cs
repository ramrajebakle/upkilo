using Hangfire;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using System.Text.Json;

namespace Upkilo.API.Jobs;

/// <summary>
/// Hangfire background job for processing booking CSV imports.
/// </summary>
public class BookingImportJob
{
    private readonly AppDbContext _context;
    private readonly ILogger<BookingImportJob> _logger;

    public BookingImportJob(AppDbContext context, ILogger<BookingImportJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Process a booking CSV import in the background.
    /// Expected CSV columns: client_email, service_name, date, time, staff_name, notes
    /// </summary>
    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync(Guid jobId, Guid tenantId, string filePath)
    {
        _logger.LogInformation("Starting booking import job {JobId} for tenant {TenantId}", jobId, tenantId);

        var job = await _context.DataImportJobs.FirstOrDefaultAsync(x => x.Id == jobId && x.TenantId == tenantId);
        if (job == null)
        {
            _logger.LogWarning("Import job {JobId} not found — aborting", jobId);
            return;
        }

        try
        {
            if (!File.Exists(filePath))
            {
                job.Status = "Failed";
                job.ValidationErrors = JsonSerializer.Serialize(new[] { "Import file not found on disk" });
                job.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return;
            }

            var lines = await File.ReadAllLinesAsync(filePath);
            if (lines.Length <= 1)
            {
                job.Status = "Completed";
                job.TotalRows = 0;
                job.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return;
            }

            job.Status = "Processing";
            job.TotalRows = lines.Length - 1;
            await _context.SaveChangesAsync();

            var header = lines[0].Split(',').Select(h => h.Trim().ToLowerInvariant()).ToArray();
            int processed = 0, success = 0, errors = 0;
            var errorList = new List<string>();

            // Detect column indices
            int emailIdx = Array.FindIndex(header, h => h is "client_email" or "email");
            int serviceIdx = Array.FindIndex(header, h => h is "service_name" or "service");
            int dateIdx = Array.FindIndex(header, h => h is "date" or "booking_date");
            int timeIdx = Array.FindIndex(header, h => h is "time" or "booking_time" or "start_time");
            int staffIdx = Array.FindIndex(header, h => h is "staff_name" or "staff" or "provider");
            int notesIdx = Array.FindIndex(header, h => h is "notes" or "note" or "comments");

            if (emailIdx < 0) emailIdx = 0;
            if (serviceIdx < 0) serviceIdx = 1;
            if (dateIdx < 0) dateIdx = 2;
            if (timeIdx < 0) timeIdx = 3;

            // Pre-load lookups for matching
            var clients = await _context.Clients
                .Where(c => c.TenantId == tenantId)
                .Select(c => new { c.Id, Email = c.Email.ToLower() })
                .ToDictionaryAsync(c => c.Email, c => c.Id);

            var services = await _context.Services
                .Where(s => s.TenantId == tenantId)
                .Select(s => new { s.Id, Name = s.Name.ToLower(), s.Duration })
                .ToListAsync();

            for (int i = 1; i < lines.Length; i++)
            {
                try
                {
                    var cols = lines[i].Split(',');
                    var clientEmail = cols.ElementAtOrDefault(emailIdx)?.Trim() ?? "";
                    var serviceName = cols.ElementAtOrDefault(serviceIdx)?.Trim() ?? "";
                    var dateStr = cols.ElementAtOrDefault(dateIdx)?.Trim() ?? "";
                    var timeStr = cols.ElementAtOrDefault(timeIdx)?.Trim() ?? "";

                    // Validate required fields
                    if (string.IsNullOrWhiteSpace(dateStr))
                    {
                        errors++;
                        errorList.Add($"Row {i}: Missing date");
                        processed++;
                        continue;
                    }

                    // Parse date/time
                    if (!DateTime.TryParse($"{dateStr} {timeStr}".Trim(), out var startTime))
                    {
                        errors++;
                        errorList.Add($"Row {i}: Invalid date/time format: {dateStr} {timeStr}");
                        processed++;
                        continue;
                    }

                    // Match client by email
                    Guid? clientId = null;
                    if (!string.IsNullOrEmpty(clientEmail) && clients.TryGetValue(clientEmail.ToLower(), out var matchedClientId))
                    {
                        clientId = matchedClientId;
                    }

                    // Match service by name
                    var matchedService = services.FirstOrDefault(s => s.Name.Equals(serviceName.ToLower()));
                    int duration = matchedService?.Duration ?? 60;

                    var booking = new Booking
                    {
                        TenantId = tenantId,
                        ClientId = clientId,
                        ServiceId = matchedService?.Id,
                        StartTime = startTime,
                        EndTime = startTime.AddMinutes(duration),
                        Status = BookingStatus.Confirmed,
                        Notes = notesIdx >= 0 ? cols.ElementAtOrDefault(notesIdx)?.Trim() : null,
                        Source = BookingSource.Website,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Bookings.Add(booking);
                    success++;
                }
                catch (Exception ex)
                {
                    errors++;
                    errorList.Add($"Row {i}: {ex.Message}");
                }
                processed++;

                // Batch save every 50 rows
                if (processed % 50 == 0)
                {
                    job.ProcessedRows = processed;
                    job.SuccessRows = success;
                    job.ErrorRows = errors;
                    await _context.SaveChangesAsync();
                }
            }

            await _context.SaveChangesAsync();

            job.ProcessedRows = processed;
            job.SuccessRows = success;
            job.ErrorRows = errors;
            job.ValidationErrors = errorList.Any() ? JsonSerializer.Serialize(errorList) : null;
            job.Status = errors > 0 ? "CompletedWithErrors" : "Completed";
            job.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Booking import {JobId} completed. Rows: {Total}, Success: {Success}, Errors: {Errors}",
                jobId, processed, success, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Booking import job {JobId} failed", jobId);
            job.Status = "Failed";
            job.ValidationErrors = JsonSerializer.Serialize(new[] { $"System error: {ex.Message}" });
            job.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            throw;
        }
        finally
        {
            try { if (File.Exists(filePath)) File.Delete(filePath); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to clean up import file: {FilePath}", filePath); }
        }
    }
}
