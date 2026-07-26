using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class P2_PerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_TenantId",
                table: "Subscriptions");

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 19, 3, 11, 305, DateTimeKind.Utc).AddTicks(2467), new DateTime(2026, 6, 22, 19, 3, 11, 305, DateTimeKind.Utc).AddTicks(2471) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 19, 3, 11, 305, DateTimeKind.Utc).AddTicks(2485), new DateTime(2026, 6, 22, 19, 3, 11, 305, DateTimeKind.Utc).AddTicks(2486) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 19, 3, 11, 305, DateTimeKind.Utc).AddTicks(2492), new DateTime(2026, 6, 22, 19, 3, 11, 305, DateTimeKind.Utc).AddTicks(2492) });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_Tenant_Status",
                table: "Subscriptions",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AIUsageLogs_Tenant_CreatedAt",
                table: "AIUsageLogs",
                columns: new[] { "TenantId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_Tenant_Status",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_AIUsageLogs_Tenant_CreatedAt",
                table: "AIUsageLogs");

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 14, 42, 16, 401, DateTimeKind.Utc).AddTicks(4178), new DateTime(2026, 6, 22, 14, 42, 16, 401, DateTimeKind.Utc).AddTicks(4182) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 14, 42, 16, 401, DateTimeKind.Utc).AddTicks(4217), new DateTime(2026, 6, 22, 14, 42, 16, 401, DateTimeKind.Utc).AddTicks(4217) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 14, 42, 16, 401, DateTimeKind.Utc).AddTicks(4222), new DateTime(2026, 6, 22, 14, 42, 16, 401, DateTimeKind.Utc).AddTicks(4223) });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TenantId",
                table: "Subscriptions",
                column: "TenantId");
        }
    }
}
