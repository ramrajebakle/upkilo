using Microsoft.EntityFrameworkCore;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Infrastructure.Services;

/// <summary>
/// Serializes a tenant's AI quota check-and-reserve across every process.
///
/// The quota sequence is read-then-write: sum this month's cost, compare against the budget, then
/// insert a reservation. That is only safe if nothing else runs the same sequence for the same
/// tenant concurrently. It was previously guarded by a static SemaphoreSlim, which serializes
/// within one process only — and the deployment runs multiple replicas (the outbox processor
/// explicitly coordinates "across all replicas"). Two replicas could therefore both read a
/// below-budget total before either wrote its reservation, and the tenant exceeded the budget by
/// up to one request per replica. The overage is real spend against the AI provider account.
///
/// A PostgreSQL transaction-scoped advisory lock replaces it. The lock is held by the database, so
/// it is honoured by every replica, and it releases automatically when the transaction ends —
/// including if the process dies mid-request, which a distributed lock built on expiry timers
/// would not guarantee.
///
/// Contention is unchanged in character: the in-process semaphore already serialized a tenant's AI
/// calls. This makes that serialization correct rather than adding it.
/// </summary>
public static class AiQuotaGate
{
    /// <summary>
    /// Runs <paramref name="reserve"/> holding an exclusive advisory lock for this tenant.
    ///
    /// On providers without advisory locks — SQLite, used by the test suite — the callback runs
    /// without one. That is acceptable there because those runs are single-process; it must not be
    /// relied on in production, where the provider is always Npgsql.
    /// </summary>
    public static async Task<T> WithTenantLockAsync<T>(
        AppDbContext context,
        Guid tenantId,
        Func<Task<T>> reserve,
        CancellationToken ct = default)
    {
        if (!context.Database.IsNpgsql())
            return await reserve();

        await using var transaction = await context.Database.BeginTransactionAsync(ct);

        // pg_advisory_xact_lock takes a 64-bit key. The tenant GUID is folded into one by XOR-ing
        // its two halves, which keeps distinct tenants apart in practice; a collision would only
        // cause two unrelated tenants to serialize against each other, never a missed limit.
        var key = AdvisoryKey(tenantId);

        await context.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock({0})", new object[] { key }, ct);

        var result = await reserve();

        // Committing releases the advisory lock and persists the reservation together, so a
        // reservation can never be visible to another replica while the lock is still held.
        await transaction.CommitAsync(ct);
        return result;
    }

    internal static long AdvisoryKey(Guid tenantId)
    {
        Span<byte> bytes = stackalloc byte[16];
        tenantId.TryWriteBytes(bytes);
        var high = BitConverter.ToInt64(bytes[..8]);
        var low = BitConverter.ToInt64(bytes[8..]);
        return high ^ low;
    }
}
