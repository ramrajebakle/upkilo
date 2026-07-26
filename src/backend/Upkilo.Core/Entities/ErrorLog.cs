namespace Upkilo.Core.Entities;

/// <summary>
/// Log of system errors and exceptions for auditing and debugging.
/// </summary>
public class ErrorLog : TenantEntity
{
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string Level { get; set; } = "Error"; // Info, Warning, Error, Critical
    public string? Source { get; set; } // Component, Service, or Controller name
    public string? IpAddress { get; set; }
    public Guid? UserId { get; set; }
    public string? RequestPath { get; set; }
    public string? RequestMethod { get; set; }
    public string? QueryString { get; set; }
    public string? RequestBody { get; set; } // Care should be taken for PII/Secrets
    public string? UserAgent { get; set; }
}
