namespace Upkilo.Core.Interfaces;

public interface ITenantProvider
{
    Guid? GetTenantId();
    Guid? GetUserId();
    string? GetTimezone();
}
