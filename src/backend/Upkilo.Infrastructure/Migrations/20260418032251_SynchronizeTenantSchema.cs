using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SynchronizeTenantSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Safely add columns using raw SQL to handle cases where snapshot is out of sync with DB
            migrationBuilder.Sql("ALTER TABLE \"Tenants\" ADD COLUMN IF NOT EXISTS \"Domain\" text;");
            migrationBuilder.Sql("ALTER TABLE \"Tenants\" ADD COLUMN IF NOT EXISTS \"Email\" text;");
            migrationBuilder.Sql("ALTER TABLE \"Tenants\" ADD COLUMN IF NOT EXISTS \"Phone\" text;");
            migrationBuilder.Sql("ALTER TABLE \"Tenants\" ADD COLUMN IF NOT EXISTS \"Industry\" text;");
            migrationBuilder.Sql("ALTER TABLE \"Tenants\" ADD COLUMN IF NOT EXISTS \"EnforceTwoFactor\" boolean DEFAULT false;");
            migrationBuilder.Sql("ALTER TABLE \"Tenants\" ADD COLUMN IF NOT EXISTS \"EnforceTwoFactorForStaff\" boolean DEFAULT false;");
            migrationBuilder.Sql("ALTER TABLE \"Tenants\" ADD COLUMN IF NOT EXISTS \"EnforceTwoFactorForClients\" boolean DEFAULT false;");

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 18, 3, 22, 46, 891, DateTimeKind.Utc).AddTicks(7956), new DateTime(2026, 4, 18, 3, 22, 46, 891, DateTimeKind.Utc).AddTicks(7960) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 18, 3, 22, 46, 891, DateTimeKind.Utc).AddTicks(8001), new DateTime(2026, 4, 18, 3, 22, 46, 891, DateTimeKind.Utc).AddTicks(8001) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 18, 3, 22, 46, 891, DateTimeKind.Utc).AddTicks(8008), new DateTime(2026, 4, 18, 3, 22, 46, 891, DateTimeKind.Utc).AddTicks(8008) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Standard drop if we want to undo
            migrationBuilder.DropColumn(
                name: "Domain",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Industry",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "EnforceTwoFactor",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "EnforceTwoFactorForStaff",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "EnforceTwoFactorForClients",
                table: "Tenants");
            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 17, 17, 56, 6, 985, DateTimeKind.Utc).AddTicks(8744), new DateTime(2026, 4, 17, 17, 56, 6, 985, DateTimeKind.Utc).AddTicks(8748) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 17, 17, 56, 6, 985, DateTimeKind.Utc).AddTicks(8789), new DateTime(2026, 4, 17, 17, 56, 6, 985, DateTimeKind.Utc).AddTicks(8791) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 17, 17, 56, 6, 985, DateTimeKind.Utc).AddTicks(8820), new DateTime(2026, 4, 17, 17, 56, 6, 985, DateTimeKind.Utc).AddTicks(8822) });
        }
    }
}
