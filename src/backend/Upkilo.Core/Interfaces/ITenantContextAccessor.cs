namespace Upkilo.Core.Interfaces;

/// <summary>
/// AsyncLocal-based tenant context propagation for background jobs and async operations
/// </summary>
public interface ITenantContextAccessor
{
    Guid? TenantId { get; set; }
    Guid? UserId { get; set; }
    void SetContext(Guid tenantId, Guid? userId = null);
    void Clear();
}

public class TenantContextAccessor : ITenantContextAccessor
{
    private static readonly AsyncLocal<TenantContextHolder> _holder = new();

    public Guid? TenantId
    {
        get => _holder.Value?.TenantId;
        set
        {
            var holder = _holder.Value ??= new TenantContextHolder();
            holder.TenantId = value;
        }
    }

    public Guid? UserId
    {
        get => _holder.Value?.UserId;
        set
        {
            var holder = _holder.Value ??= new TenantContextHolder();
            holder.UserId = value;
        }
    }

    public void SetContext(Guid tenantId, Guid? userId = null)
    {
        var holder = _holder.Value ??= new TenantContextHolder();
        holder.TenantId = tenantId;
        holder.UserId = userId;
    }

    public void Clear()
    {
        _holder.Value = null;
    }

    private class TenantContextHolder
    {
        public Guid? TenantId { get; set; }
        public Guid? UserId { get; set; }
    }
}
