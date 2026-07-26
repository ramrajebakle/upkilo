using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDripRebookBackup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DripCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    TriggerType = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StepsJson = table.Column<string>(type: "text", nullable: false),
                    EnrolledCount = table.Column<int>(type: "integer", nullable: false),
                    CompletedCount = table.Column<int>(type: "integer", nullable: false),
                    OpenRate = table.Column<double>(type: "double precision", nullable: false),
                    ClickRate = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DripCampaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RebookPrompts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Trigger = table.Column<string>(type: "text", nullable: false),
                    TriggerValue = table.Column<int>(type: "integer", nullable: true),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServiceName = table.Column<string>(type: "text", nullable: true),
                    Channel = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SendCount = table.Column<int>(type: "integer", nullable: false),
                    ConversionCount = table.Column<int>(type: "integer", nullable: false),
                    ConversionRate = table.Column<double>(type: "double precision", nullable: false),
                    LastSent = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RebookPrompts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantBackups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DownloadUrl = table.Column<string>(type: "text", nullable: true),
                    IncludedEntitiesJson = table.Column<string>(type: "text", nullable: false),
                    Restorable = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantBackups", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 17, 18, 27, 59, 948, DateTimeKind.Utc).AddTicks(7640), new DateTime(2026, 7, 17, 18, 27, 59, 948, DateTimeKind.Utc).AddTicks(7644) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 17, 18, 27, 59, 948, DateTimeKind.Utc).AddTicks(7657), new DateTime(2026, 7, 17, 18, 27, 59, 948, DateTimeKind.Utc).AddTicks(7658) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 17, 18, 27, 59, 948, DateTimeKind.Utc).AddTicks(7663), new DateTime(2026, 7, 17, 18, 27, 59, 948, DateTimeKind.Utc).AddTicks(7663) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DripCampaigns");

            migrationBuilder.DropTable(
                name: "RebookPrompts");

            migrationBuilder.DropTable(
                name: "TenantBackups");

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 17, 18, 23, 19, 536, DateTimeKind.Utc).AddTicks(5477), new DateTime(2026, 7, 17, 18, 23, 19, 536, DateTimeKind.Utc).AddTicks(5481) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 17, 18, 23, 19, 536, DateTimeKind.Utc).AddTicks(5497), new DateTime(2026, 7, 17, 18, 23, 19, 536, DateTimeKind.Utc).AddTicks(5498) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 7, 17, 18, 23, 19, 536, DateTimeKind.Utc).AddTicks(5503), new DateTime(2026, 7, 17, 18, 23, 19, 536, DateTimeKind.Utc).AddTicks(5503) });
        }
    }
}
