using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations;

/// <summary>
/// SC2: Add PostgreSQL HASH partitioning for high-volume log tables.
///
/// Strategy: non-destructive — creates new partitioned tables (AIUsageLogs_p, etc.)
/// alongside the originals. A subsequent data migration or scheduled job can copy rows
/// in batches and rename tables atomically during a low-traffic window.
///
/// The partition key is TenantId (8 partitions, modulus 8) — this gives ~12.5% of rows
/// per partition and good cardinality for 10k+ tenants.
///
/// We add BRIN indexes on CreatedAt for fast time-range scans on the originals while
/// the partitioned tables are being populated.
/// </summary>
public partial class SC2_HashPartitioning : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── 1. BRIN indexes on existing tables for time-range query optimisation ──
        // Each CONCURRENTLY statement runs in its own Sql() call with suppressTransaction: true.
        // PostgreSQL forbids CREATE/DROP INDEX CONCURRENTLY inside any transaction block;
        // passing suppressTransaction prevents EF Core from wrapping the call in BEGIN/COMMIT.
        migrationBuilder.Sql(
            @"CREATE INDEX CONCURRENTLY IF NOT EXISTS ""IX_AIUsageLogs_TenantId_CreatedAt_brin""
                ON ""AIUsageLogs"" USING BRIN (""TenantId"", ""CreatedAt"");",
            suppressTransaction: true);

        migrationBuilder.Sql(
            @"CREATE INDEX CONCURRENTLY IF NOT EXISTS ""IX_AuditLogs_TenantId_CreatedAt_brin""
                ON ""AuditLogs"" USING BRIN (""TenantId"", ""Timestamp"");",
            suppressTransaction: true);

        migrationBuilder.Sql(
            @"CREATE INDEX CONCURRENTLY IF NOT EXISTS ""IX_CommunicationLogs_TenantId_CreatedAt_brin""
                ON ""CommunicationLogs"" USING BRIN (""TenantId"", ""CreatedAt"");",
            suppressTransaction: true);

        // ── 2. Create HASH-partitioned shadow tables ───────────────────────────
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS ""AIUsageLogs_p"" (
                ""Id""            uuid        NOT NULL,
                ""TenantId""      uuid        NOT NULL,
                ""UserId""        uuid,
                ""Model""         text        NOT NULL DEFAULT 'gpt-4',
                ""Feature""       text        NOT NULL DEFAULT '',
                ""InputTokens""   integer     NOT NULL DEFAULT 0,
                ""OutputTokens""  integer     NOT NULL DEFAULT 0,
                ""Cost""          numeric     NOT NULL DEFAULT 0,
                ""LatencyMs""     integer,
                ""Success""       boolean     NOT NULL DEFAULT true,
                ""ErrorMessage""  text,
                ""CreatedAt""     timestamptz NOT NULL DEFAULT now(),
                PRIMARY KEY (""Id"", ""TenantId"")
            ) PARTITION BY HASH (""TenantId"");
        ");

        // Create 8 partitions for AIUsageLogs_p
        for (int i = 0; i < 8; i++)
        {
            migrationBuilder.Sql($@"
                CREATE TABLE IF NOT EXISTS ""AIUsageLogs_p_{i}""
                    PARTITION OF ""AIUsageLogs_p""
                    FOR VALUES WITH (MODULUS 8, REMAINDER {i});
            ");
        }

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS ""AuditLogs_p"" (
                ""Id""          uuid        NOT NULL,
                ""TenantId""    uuid        NOT NULL,
                ""UserId""      uuid,
                ""Action""      text        NOT NULL DEFAULT '',
                ""EntityType""  text        NOT NULL DEFAULT '',
                ""EntityId""    text,
                ""OldValues""   text,
                ""NewValues""   text,
                ""IpAddress""   text,
                ""UserAgent""   text,
                ""Timestamp""   timestamptz NOT NULL DEFAULT now(),
                PRIMARY KEY (""Id"", ""TenantId"")
            ) PARTITION BY HASH (""TenantId"");
        ");

        for (int i = 0; i < 8; i++)
        {
            migrationBuilder.Sql($@"
                CREATE TABLE IF NOT EXISTS ""AuditLogs_p_{i}""
                    PARTITION OF ""AuditLogs_p""
                    FOR VALUES WITH (MODULUS 8, REMAINDER {i});
            ");
        }

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS ""CommunicationLogs_p"" (
                ""Id""          uuid        NOT NULL,
                ""TenantId""    uuid        NOT NULL,
                ""ClientId""    uuid,
                ""UserId""      uuid,
                ""Channel""     text        NOT NULL DEFAULT 'email',
                ""Subject""     text        NOT NULL DEFAULT '',
                ""Body""        text        NOT NULL DEFAULT '',
                ""Status""      text        NOT NULL DEFAULT 'sent',
                ""CreatedAt""   timestamptz NOT NULL DEFAULT now(),
                PRIMARY KEY (""Id"", ""TenantId"")
            ) PARTITION BY HASH (""TenantId"");
        ");

        for (int i = 0; i < 8; i++)
        {
            migrationBuilder.Sql($@"
                CREATE TABLE IF NOT EXISTS ""CommunicationLogs_p_{i}""
                    PARTITION OF ""CommunicationLogs_p""
                    FOR VALUES WITH (MODULUS 8, REMAINDER {i});
            ");
        }

        // ── 3. Covering indexes on partitioned tables ──────────────────────────
        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS ""IX_AIUsageLogs_p_TenantId_CreatedAt""
                ON ""AIUsageLogs_p"" (""TenantId"", ""CreatedAt"" DESC);

            CREATE INDEX IF NOT EXISTS ""IX_AuditLogs_p_TenantId_Timestamp""
                ON ""AuditLogs_p"" (""TenantId"", ""Timestamp"" DESC);

            CREATE INDEX IF NOT EXISTS ""IX_CommunicationLogs_p_TenantId_CreatedAt""
                ON ""CommunicationLogs_p"" (""TenantId"", ""CreatedAt"" DESC);
        ");

        // ── 4. Comments documenting the migration path ─────────────────────────
        migrationBuilder.Sql(@"
            COMMENT ON TABLE ""AIUsageLogs_p""
                IS 'SC2: Hash-partitioned shadow of AIUsageLogs (8 shards on TenantId). Run the batch migration script to populate and then swap table names.';
            COMMENT ON TABLE ""AuditLogs_p""
                IS 'SC2: Hash-partitioned shadow of AuditLogs (8 shards on TenantId).';
            COMMENT ON TABLE ""CommunicationLogs_p""
                IS 'SC2: Hash-partitioned shadow of CommunicationLogs (8 shards on TenantId).';
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop partitioned tables and their partitions
        for (int i = 7; i >= 0; i--)
        {
            migrationBuilder.Sql($@"DROP TABLE IF EXISTS ""AIUsageLogs_p_{i}"" CASCADE;");
            migrationBuilder.Sql($@"DROP TABLE IF EXISTS ""AuditLogs_p_{i}"" CASCADE;");
            migrationBuilder.Sql($@"DROP TABLE IF EXISTS ""CommunicationLogs_p_{i}"" CASCADE;");
        }
        migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""AIUsageLogs_p"" CASCADE;");
        migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""AuditLogs_p"" CASCADE;");
        migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""CommunicationLogs_p"" CASCADE;");

        migrationBuilder.Sql(
            @"DROP INDEX CONCURRENTLY IF EXISTS ""IX_AIUsageLogs_TenantId_CreatedAt_brin"";",
            suppressTransaction: true);
        migrationBuilder.Sql(
            @"DROP INDEX CONCURRENTLY IF EXISTS ""IX_AuditLogs_TenantId_CreatedAt_brin"";",
            suppressTransaction: true);
        migrationBuilder.Sql(
            @"DROP INDEX CONCURRENTLY IF EXISTS ""IX_CommunicationLogs_TenantId_CreatedAt_brin"";",
            suppressTransaction: true);
    }
}
