using System;

namespace Upkilo.Core.Entities
{
    /// <summary>
    /// Stores AI-generated SEO and Market Discovery reports for tenants.
    /// </summary>
    public class AIDiscoveryReport : BaseEntity
    {
        public Guid TenantId { get; set; }
        public string BusinessType { get; set; } = string.Empty;
        public string Niche { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty; // Markdown report
        public string Keywords { get; set; } = string.Empty; // Semicolon separated
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public bool IsUserReviewed { get; set; }

        // Navigation
        public virtual Tenant? Tenant { get; set; }
    }
}
