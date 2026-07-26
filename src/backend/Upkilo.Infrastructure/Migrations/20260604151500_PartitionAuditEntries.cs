using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PartitionAuditEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                // 1. Rename existing table and its PK index (PG index names are schema-scoped)
                migrationBuilder.Sql("ALTER TABLE \"AuditEntries\" RENAME TO \"AuditEntries_temp\";");
                migrationBuilder.Sql("ALTER INDEX \"PK_AuditEntries\" RENAME TO \"PK_AuditEntries_temp\";");

                // 2. Create partitioned table (PK must include partition key Timestamp)
                migrationBuilder.Sql(@"
                    CREATE TABLE ""AuditEntries"" (
                        ""Id"" uuid NOT NULL,
                        ""TenantId"" uuid NOT NULL,
                        ""EntityType"" text NOT NULL,
                        ""EntityId"" text NOT NULL,
                        ""Action"" text NOT NULL,
                        ""UserId"" uuid NULL,
                        ""UserName"" text NULL,
                        ""OldValues"" text NULL,
                        ""NewValues"" text NULL,
                        ""ChangedFields"" text NULL,
                        ""IpAddress"" text NULL,
                        ""UserAgent"" text NULL,
                        ""Details"" text NULL,
                        ""Timestamp"" timestamp with time zone NOT NULL,
                        ""PerformedAt"" timestamp with time zone NOT NULL DEFAULT NOW(),
                        ""PerformedById"" uuid NULL,
                        ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT NOW(),
                        ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT NOW(),
                        ""RowVersion"" bytea NULL,
                        ""Version"" integer NOT NULL DEFAULT 1,
                        ""IsDeleted"" boolean NOT NULL DEFAULT false,
                        ""DeletedAt"" timestamp with time zone NULL,
                        ""DeletedBy"" text NULL,
                        CONSTRAINT ""PK_AuditEntries"" PRIMARY KEY (""Id"", ""Timestamp"")
                    ) PARTITION BY RANGE (""Timestamp"");
                ");

                // 3. Create initial partitions for 2026
                migrationBuilder.Sql(@"
                    CREATE TABLE ""AuditEntries_2026_06"" PARTITION OF ""AuditEntries""
                        FOR VALUES FROM ('2026-06-01 00:00:00+00') TO ('2026-07-01 00:00:00+00');
                    CREATE TABLE ""AuditEntries_2026_07"" PARTITION OF ""AuditEntries""
                        FOR VALUES FROM ('2026-07-01 00:00:00+00') TO ('2026-08-01 00:00:00+00');
                    CREATE TABLE ""AuditEntries_default"" PARTITION OF ""AuditEntries"" DEFAULT;
                ");

                // 4. Migrate old data
                migrationBuilder.Sql(@"
                    INSERT INTO ""AuditEntries"" (""Id"", ""TenantId"", ""EntityType"", ""EntityId"", ""Action"", ""UserId"", ""UserName"", ""OldValues"", ""NewValues"", ""ChangedFields"", ""IpAddress"", ""UserAgent"", ""Details"", ""Timestamp"")
                    SELECT ""Id"", ""TenantId"", ""EntityType"", ""EntityId"", ""Action"", ""UserId"", ""UserName"", ""OldValues"", ""NewValues"", ""ChangedFields"", ""IpAddress"", ""UserAgent"", ""Details"", ""Timestamp""
                    FROM ""AuditEntries_temp"";
                ");

                // 5. Drop temp table
                migrationBuilder.Sql("DROP TABLE \"AuditEntries_temp\";");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                // Reverse is to make unpartitioned table again
                migrationBuilder.Sql("ALTER TABLE \"AuditEntries\" RENAME TO \"AuditEntries_temp\";");
                migrationBuilder.Sql("ALTER INDEX \"PK_AuditEntries\" RENAME TO \"PK_AuditEntries_temp\";");

                migrationBuilder.Sql(@"
                    CREATE TABLE ""AuditEntries"" (
                        ""Id"" uuid NOT NULL,
                        ""TenantId"" uuid NOT NULL,
                        ""EntityType"" text NOT NULL,
                        ""EntityId"" text NOT NULL,
                        ""Action"" text NOT NULL,
                        ""UserId"" uuid NULL,
                        ""UserName"" text NULL,
                        ""OldValues"" text NULL,
                        ""NewValues"" text NULL,
                        ""ChangedFields"" text NULL,
                        ""IpAddress"" text NULL,
                        ""UserAgent"" text NULL,
                        ""Details"" text NULL,
                        ""Timestamp"" timestamp with time zone NOT NULL,
                        CONSTRAINT ""PK_AuditEntries"" PRIMARY KEY (""Id"")
                    );
                ");

                migrationBuilder.Sql(@"
                    INSERT INTO ""AuditEntries"" (""Id"", ""TenantId"", ""EntityType"", ""EntityId"", ""Action"", ""UserId"", ""UserName"", ""OldValues"", ""NewValues"", ""ChangedFields"", ""IpAddress"", ""UserAgent"", ""Details"", ""Timestamp"")
                    SELECT ""Id"", ""TenantId"", ""EntityType"", ""EntityId"", ""Action"", ""UserId"", ""UserName"", ""OldValues"", ""NewValues"", ""ChangedFields"", ""IpAddress"", ""UserAgent"", ""Details"", ""Timestamp""
                    FROM ""AuditEntries_temp"";
                ");

                migrationBuilder.Sql("DROP TABLE \"AuditEntries_temp\";");
            }
        }
    }
}
