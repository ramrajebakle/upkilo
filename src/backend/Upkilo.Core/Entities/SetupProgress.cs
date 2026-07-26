namespace Upkilo.Core.Entities;

public class SetupProgress
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    // Step completion flags
    public bool ProfileCompleted { get; set; }
    public bool ServicesCompleted { get; set; }
    public bool StaffCompleted { get; set; }
    public bool AvailabilityCompleted { get; set; }
    public bool IntegrationsCompleted { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public int CompletionPercentage => CalculatePercentage();
    
    private int CalculatePercentage()
    {
        int completed = 0;
        if (ProfileCompleted) completed++;
        if (ServicesCompleted) completed++;
        if (StaffCompleted) completed++;
        if (AvailabilityCompleted) completed++;
        if (IntegrationsCompleted) completed++;
        return (completed * 100) / 5;
    }
}
