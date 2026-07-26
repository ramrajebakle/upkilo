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
    private readonly SqliteConnection _connection;

    public TestDbContextFactory()
    {
        _connection = new SqliteConnection($"Data Source=InMemory_{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        _connection.Open();
        using var pragma = _connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = OFF;";
        pragma.ExecuteNonQuery();
    }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new AppDbContext(options);

        // Ensure the schema is created (ignores unsupported column types silently)
        context.Database.EnsureCreated();

        return context;
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

        var context = new AppDbContext(options, tenantProvider.Object);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}
