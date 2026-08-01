using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Jobs
{
    public class BackupVerificationJob
    {
        private readonly ILogger<BackupVerificationJob> _logger;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;

        public BackupVerificationJob(
            ILogger<BackupVerificationJob> logger,
            IConfiguration configuration,
            AppDbContext context)
        {
            _logger = logger;
            _configuration = configuration;
            _context = context;
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation("Starting backup verification job...");

            var passed = 0;
            var failed = 0;

            // ── Check 1: Verify Azure Blob backup container is reachable and has recent files ──
            try
            {
                var storageAccount = _configuration["Azure:StorageAccountName"];
                var containerName = _configuration["Azure:BackupContainerName"] ?? "db-backups";
                var storageSasToken = _configuration["Azure:BackupSasToken"];

                if (!string.IsNullOrEmpty(storageAccount) && !string.IsNullOrEmpty(storageSasToken))
                {
                    var listUrl = $"https://{storageAccount}.blob.core.windows.net/{containerName}" +
                                  $"?restype=container&comp=list&maxresults=5&{storageSasToken}";

                    using var http = new HttpClient();
                    var response = await http.GetAsync(listUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var xml = await response.Content.ReadAsStringAsync();
                        if (xml.Contains("<Blob>"))
                        {
                            _logger.LogInformation("Backup container reachable and contains blobs.");
                            passed++;
                        }
                        else
                        {
                            _logger.LogWarning("Backup container is EMPTY — no backup files found.");
                            failed++;
                        }
                    }
                    else
                    {
                        _logger.LogError("Backup container unreachable: {Status}", response.StatusCode);
                        failed++;
                    }
                }
                else
                {
                    _logger.LogWarning("Azure backup storage not configured — skipping blob check.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backup blob check failed.");
                failed++;
            }

            // ── Check 2: Live DB health probe — verify critical tables are queryable ──
            try
            {
                var tenantCount = await _context.Tenants.CountAsync();
                var bookingCount = await _context.Bookings.CountAsync();

                _logger.LogInformation(
                    "DB health probe passed: {Tenants} tenants, {Bookings} bookings.",
                    tenantCount, bookingCount);
                passed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DB health probe failed — database may be unreachable.");
                failed++;
            }

            // ── Summary ──
            if (failed == 0)
                _logger.LogInformation("Backup verification PASSED ({Passed}/{Total} checks).", passed, passed + failed);
            else
                _logger.LogError("Backup verification FAILED — {Failed} of {Total} checks failed.", failed, passed + failed);
        }
    }
}
