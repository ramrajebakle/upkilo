using Microsoft.Extensions.Logging;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;

namespace Upkilo.Infrastructure.Jobs
{
    public class DiscoveryAgentJob
    {
        private readonly AppDbContext _context;
        private readonly IAIService _aiService;
        private readonly ILogger<DiscoveryAgentJob> _logger;

        public DiscoveryAgentJob(
            AppDbContext context,
            IAIService aiService,
            ILogger<DiscoveryAgentJob> logger)
        {
            _context = context;
            _aiService = aiService;
            _logger = logger;
        }

        public async Task RunAsync()
        {
            _logger.LogInformation("DiscoveryAgentJob starting at {Time}", DateTime.UtcNow);

            var tenants = await _context.Tenants
                .Where(t => t.IsActive && t.Status == TenantStatus.Active)
                .ToListAsync();

            foreach (var tenant in tenants)
            {
                try
                {
                    await ProcessTenantDiscovery(tenant);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process discovery for tenant {TenantId}", tenant.Id);
                }
            }

            _logger.LogInformation("DiscoveryAgentJob completed at {Time}", DateTime.UtcNow);
        }

        public async Task ProcessTenantDiscovery(Tenant tenant)
        {
            var businessType = tenant.BusinessType ?? tenant.Industry ?? "Local Business";
            var niche = tenant.Industry ?? "Service Industry";

            _logger.LogInformation("Generating discovery report for tenant {TenantId} ({BusinessName})", tenant.Id, tenant.Name);

            var result = await _aiService.GenerateDiscoveryReportAsync(tenant.Id, businessType, niche);

            if (result.Success && !string.IsNullOrEmpty(result.Content))
            {
                var report = new AIDiscoveryReport
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    BusinessType = businessType,
                    Niche = niche,
                    Content = result.Content,
                    Keywords = ExtractKeywords(result.Content),
                    GeneratedAt = DateTime.UtcNow
                };

                _context.AIDiscoveryReports.Add(report);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Discovery report saved for tenant {TenantId}", tenant.Id);
            }
            else
            {
                _logger.LogWarning("Failed to generate discovery report for tenant {TenantId}: {Error}", tenant.Id, result.Error);
            }
        }

        private string ExtractKeywords(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return string.Empty;

            // Robust keyword scoring: length-weighted frequency + position bias
            var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "the", "and", "a", "an", "of", "to", "in", "is", "it", "that", "this", "for", "on", "with", "as", "are", "by", "be", "was", "were", "or", "from", "at", "but", "not", "have", "has", "had", "which", "can", "will", "your", "our", "their", "more", "into", "through", "about", "include", "suggested", "analysis", "market", "strategies"
            };

            var words = content.Split(new[] { ' ', '.', ',', '!', '?', '(', ')', '[', ']', '\"', '\'', '\n', '\r', ':', '-', '•' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.Trim().ToLowerInvariant())
                .Where(w => w.Length > 3 && !stopWords.Contains(w) && w.All(char.IsLetterOrDigit))
                .ToList();

            var scoring = new Dictionary<string, double>();
            for (int i = 0; i < words.Count; i++)
            {
                var word = words[i];
                // Base score: 1.0
                // Length bonus: longer words are often more specific
                var score = 1.0 + (word.Length * 0.1);
                
                // Position bias: words at the beginning/end are often more important
                if (i < words.Count * 0.2) score *= 1.2;
                if (i > words.Count * 0.8) score *= 1.1;

                if (scoring.ContainsKey(word))
                    scoring[word] += score;
                else
                    scoring[word] = score;
            }

            var topKeywords = scoring
                .OrderByDescending(kvp => kvp.Value)
                .Take(15)
                .Select(kvp => kvp.Key);

            return string.Join(", ", topKeywords);
        }
    }
}
