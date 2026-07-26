namespace Upkilo.Core.Entities
{
    public class WorkflowEvent
    {
        public string EventName { get; set; } = string.Empty;
        public object Data { get; set; } = new();
        public Guid TenantId { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }
}
