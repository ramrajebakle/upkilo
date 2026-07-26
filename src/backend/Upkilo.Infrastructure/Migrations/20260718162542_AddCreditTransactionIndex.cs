using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditTransactionIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_Tenant_Client",
                table: "CreditTransactions",
                columns: new[] { "TenantId", "ClientId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CreditTransactions_Tenant_Client",
                table: "CreditTransactions");

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
    }
}
