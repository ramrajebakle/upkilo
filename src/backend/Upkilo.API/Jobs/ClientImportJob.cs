using Hangfire;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Infrastructure.Data;
using System.Text.Json;

namespace Upkilo.API.Jobs;

/// <summary>
/// Hangfire background job for processing client CSV imports.
/// Replaces the unsafe Task.Run approach in DataOperationsController.
/// </summary>
public class ClientImportJob
{
    private readonly AppDbContext _context;
    private readonly ILogger<ClientImportJob> _logger;

    public ClientImportJob(AppDbContext context, ILogger<ClientImportJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Process a client CSV import in the background.
    /// </summary>
    [AutomaticRetry(Attempts = 2)]
    public async Task ExecuteAsync(Guid jobId, Guid tenantId, string filePath)
    {
        _logger.LogInformation("Starting client import job {JobId} for tenant {TenantId}", jobId, tenantId);

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
                _logger.LogError("Import file not found: {FilePath}", filePath);
                return;
            }

            var lines = await File.ReadAllLinesAsync(filePath);
            if (lines.Length <= 1)
            {
                job.Status = "Completed";
                job.TotalRows = 0;
                job.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Import job {JobId} completed — file was empty or header-only", jobId);
                return;
            }

            job.Status = "Processing";
            job.TotalRows = lines.Length - 1;
            await _context.SaveChangesAsync();

            var header = lines[0].Split(',').Select(h => h.Trim().ToLowerInvariant()).ToArray();
            int processed = 0, success = 0, errors = 0, duplicates = 0;
            var errorList = new List<string>();

            // Detect column indices by header name
            int firstNameIdx = Array.FindIndex(header, h => h is "first_name" or "firstname" or "first name");
            int lastNameIdx = Array.FindIndex(header, h => h is "last_name" or "lastname" or "last name");
            int emailIdx = Array.FindIndex(header, h => h is "email" or "email_address");
            int phoneIdx = Array.FindIndex(header, h => h is "phone" or "phone_number" or "mobile");

            // Fallback to positional mapping if headers not found
            if (firstNameIdx < 0) firstNameIdx = 0;
            if (lastNameIdx < 0) lastNameIdx = 1;
            if (emailIdx < 0) emailIdx = 2;
            if (phoneIdx < 0) phoneIdx = 3;

            // Pre-load existing emails for duplicate detection
            var existingEmailsList = await _context.Clients
                .Where(c => c.TenantId == tenantId)
                .Select(c => c.Email.ToLower())
                .ToListAsync();
            var existingEmails = existingEmailsList.ToHashSet();

            for (int i = 1; i < lines.Length; i++)
            {
                try
                {
                    var cols = lines[i].Split(',');
                    var email = cols.ElementAtOrDefault(emailIdx)?.Trim() ?? "";

                    // Skip duplicates
                    if (!string.IsNullOrEmpty(email) && existingEmails.Contains(email.ToLower()))
                    {
                        duplicates++;
                        processed++;
                        continue;
                    }

                    // Validate required fields
                    var firstName = cols.ElementAtOrDefault(firstNameIdx)?.Trim() ?? "";
                    if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(email))
                    {
                        errors++;
                        errorList.Add($"Row {i}: Missing both name and email — at least one is required");
                        processed++;
                        continue;
                    }

                    var client = new Client
                    {
                        TenantId = tenantId,
                        FirstName = firstName,
                        LastName = cols.ElementAtOrDefault(lastNameIdx)?.Trim() ?? "",
                        Email = email,
                        Phone = cols.ElementAtOrDefault(phoneIdx)?.Trim(),
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Clients.Add(client);
                    if (!string.IsNullOrEmpty(email))
                        existingEmails.Add(email.ToLower());
                    success++;
                }
                catch (Exception ex)
                {
                    errors++;
                    errorList.Add($"Row {i}: {ex.Message}");
                }
                processed++;

                // Batch save every 50 rows for progress visibility
                if (processed % 50 == 0)
                {
                    job.ProcessedRows = processed;
                    job.SuccessRows = success;
                    job.ErrorRows = errors;
                    job.DuplicatesFound = duplicates;
                    await _context.SaveChangesAsync();
                }
            }

            // Final save
            await _context.SaveChangesAsync();

            job.ProcessedRows = processed;
            job.SuccessRows = success;
            job.ErrorRows = errors;
            job.DuplicatesFound = duplicates;
            job.ValidationErrors = errorList.Any() ? JsonSerializer.Serialize(errorList) : null;
            job.Status = errors > 0 ? "CompletedWithErrors" : "Completed";
            job.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Client import {JobId} completed. Rows: {Total}, Success: {Success}, Errors: {Errors}, Duplicates: {Duplicates}",
                jobId, processed, success, errors, duplicates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Client import job {JobId} failed", jobId);
            job.Status = "Failed";
            job.ValidationErrors = JsonSerializer.Serialize(new[] { $"System error: {ex.Message}" });
            job.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            throw; // Let Hangfire retry
        }
        finally
        {
            // Clean up the import file
            try { if (File.Exists(filePath)) File.Delete(filePath); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to clean up import file: {FilePath}", filePath); }
        }
    }
}
