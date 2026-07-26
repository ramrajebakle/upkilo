namespace Upkilo.Core.Entities;

/// <summary>
/// Tenant lifecycle state machine.
/// Defines all valid states and transitions for a tenant's lifecycle.
/// Used by controllers and jobs to enforce valid state transitions.
/// 
/// State diagram:
///   Provisioning → Active → PastDue → Suspended → Cancelled → Deleted
///                   ↑         ↓          ↓
///                   └── Active ←── Active (payment recovered)
///                   
///   Active ↔ Paused (voluntary pause)
///   Any state → Cancelled (by admin or for policy violation)
/// </summary>
public enum TenantLifecycleState
{
    Provisioning,   // Account created, setup in progress
    Active,         // Normal operating state
    Trial,          // Free trial period
    PastDue,        // Payment failed, grace period
    Suspended,      // Auto-suspended (Day 14) — read-only
    Paused,         // Voluntarily paused by owner
    Cancelled,      // Subscription cancelled — 90-day data hold
    Deleted         // Data permanently purged
}

/// <summary>
/// Static validator for tenant state transitions.
/// Ensures only valid transitions are allowed.
/// </summary>
public static class TenantLifecycleTransitions
{
    private static readonly Dictionary<TenantLifecycleState, HashSet<TenantLifecycleState>> ValidTransitions = new()
    {
        { TenantLifecycleState.Provisioning, new() { TenantLifecycleState.Active, TenantLifecycleState.Trial, TenantLifecycleState.Cancelled } },
        { TenantLifecycleState.Trial, new() { TenantLifecycleState.Active, TenantLifecycleState.Cancelled } },
        { TenantLifecycleState.Active, new() { TenantLifecycleState.PastDue, TenantLifecycleState.Paused, TenantLifecycleState.Cancelled } },
        { TenantLifecycleState.PastDue, new() { TenantLifecycleState.Active, TenantLifecycleState.Suspended, TenantLifecycleState.Cancelled } },
        { TenantLifecycleState.Suspended, new() { TenantLifecycleState.Active, TenantLifecycleState.Cancelled } },
        { TenantLifecycleState.Paused, new() { TenantLifecycleState.Active, TenantLifecycleState.Cancelled } },
        { TenantLifecycleState.Cancelled, new() { TenantLifecycleState.Deleted } },
        { TenantLifecycleState.Deleted, new() { } }  // Terminal state
    };

    /// <summary>
    /// Check if a state transition is valid.
    /// </summary>
    public static bool IsValidTransition(TenantLifecycleState from, TenantLifecycleState to)
    {
        return ValidTransitions.TryGetValue(from, out var valid) && valid.Contains(to);
    }

    /// <summary>
    /// Get all valid next states from the current state.
    /// </summary>
    public static IReadOnlySet<TenantLifecycleState> GetValidNextStates(TenantLifecycleState current)
    {
        return ValidTransitions.TryGetValue(current, out var valid) ? valid : new HashSet<TenantLifecycleState>();
    }

    /// <summary>
    /// Determine if an action is allowed in a given state.
    /// </summary>
    public static TenantCapabilities GetCapabilities(TenantLifecycleState state) => state switch
    {
        TenantLifecycleState.Active or TenantLifecycleState.Trial => new(CanBook: true, CanModify: true, CanRead: true, CanBill: true),
        TenantLifecycleState.PastDue => new(CanBook: true, CanModify: true, CanRead: true, CanBill: false),
        TenantLifecycleState.Paused => new(CanBook: false, CanModify: false, CanRead: true, CanBill: false),
        TenantLifecycleState.Suspended => new(CanBook: false, CanModify: false, CanRead: true, CanBill: false),
        TenantLifecycleState.Cancelled => new(CanBook: false, CanModify: false, CanRead: true, CanBill: false),
        _ => new(CanBook: false, CanModify: false, CanRead: false, CanBill: false)
    };
}

/// <summary>
/// Capabilities available in a given tenant lifecycle state.
/// </summary>
public record TenantCapabilities(bool CanBook, bool CanModify, bool CanRead, bool CanBill);
