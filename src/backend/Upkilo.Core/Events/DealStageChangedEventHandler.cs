using MediatR;

namespace Upkilo.Core.Events;

public class DealStageChangedEvent : INotification
{
    public Guid DealId { get; set; }
    public Guid TenantId { get; set; }
    public Guid OldStageId { get; set; }
    public Guid NewStageId { get; set; }
    public string DealTitle { get; set; } = string.Empty;
}
