using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.Tests.Helpers;

/// <summary>
/// Creates an AppDbContext backed by SQLite in-memory for testing.
/// SQLite silently ignores PostgreSQL-specific column types (jsonb)
/// but supports most EF Core features needed for unit tests.
/// </summary>
public class TestDbContextFactory : IDisposable
{
    // xUnit constructs a new instance of the test class — and therefore a new
    // TestDbContextFactory — for every single [Fact]/[Theory], not once per class. 90 test
    // files call CreateContext(), so EnsureCreated() was re-deriving full DDL (every table,
    // index and constraint across the entire EF model) from scratch via reflection on every
    // individual test, not once per file.
    //
    // The DDL text itself does not depend on any per-test state — it is a pure function of
    // the AppDbContext model, which is fixed for the lifetime of the process — so it is
    // computed once and reused. Per-test isolation is UNCHANGED: every test still gets its
    // own brand-new, uniquely-named SQLite in-memory connection, exactly as before. Only the
    // cost of generating that connection's schema is cut, by executing a cached CREATE
    // script directly instead of re-running EF's model-to-DDL translation each time.
    private static readonly Lazy<string> CachedCreateScript = new(() =>
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var scriptContext = new AppDbContext(options);
        return scriptContext.Database.GenerateCreateScript();
    });

    private readonly SqliteConnection _connection;

    public TestDbContextFactory()
    {
        _connection = new SqliteConnection($"Data Source=InMemory_{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        _connection.Open();
        using var pragma = _connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = OFF;";
        pragma.ExecuteNonQuery();

        // Same DDL EnsureCreated() would have executed on this connection — see
        // CachedCreateScript above for why it is computed once rather than per-instance.
        using var schema = _connection.CreateCommand();
        schema.CommandText = CachedCreateScript.Value;
        schema.ExecuteNonQuery();
    }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        // No EnsureCreated() here: the constructor above already applied the cached schema
        // to this connection. Calling it again would be a same-schema no-op at best, but it
        // still pays for EF re-checking the database against the model on every CreateContext()
        // call — the exact per-call cost this factory exists to remove.
        return new AppDbContext(options);
    }

    /// <summary>
    /// Creates an AppDbContext whose global query filter is scoped to <paramref name="tenantId"/>.
    /// Use this to verify that EF Core's HasQueryFilter prevents cross-tenant data leakage.
    /// </summary>
    public AppDbContext CreateContextForTenant(Guid tenantId)
    {
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.Setup(p => p.GetTenantId()).Returns(tenantId);
        tenantProvider.Setup(p => p.GetUserId()).Returns((Guid?)null);
        tenantProvider.Setup(p => p.GetTimezone()).Returns("UTC");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new AppDbContext(options, tenantProvider.Object);
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}
