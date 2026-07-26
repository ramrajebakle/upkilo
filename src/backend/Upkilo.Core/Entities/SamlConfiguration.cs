using System;

namespace Upkilo.Core.Entities
{
    /// <summary>
    /// Configuration for SAML2/SSO for enterprise tenants.
    /// </summary>
    public class SamlConfiguration : TenantEntity
    {
        public bool IsEnabled { get; set; }
        public string EntityId { get; set; } = string.Empty;
        public string IdpMetadataUrl { get; set; } = string.Empty;
        public string? IdpCertificate { get; set; }
        public string? SignOnUrl { get; set; }
        public string? LogoutUrl { get; set; }
        public string AttributeMapping { get; set; } = "{}"; // JSON mapping: { "email": "http://...", "firstName": "..." }
        public bool AllowPasswordLogin { get; set; } = true;
        public bool AutoCreateUsers { get; set; } = false;
        public string? DefaultRoleId { get; set; }
    }
}
