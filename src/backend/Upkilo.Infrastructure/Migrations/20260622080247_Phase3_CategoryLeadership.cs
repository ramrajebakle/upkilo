using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_CategoryLeadership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 8, 2, 43, 887, DateTimeKind.Utc).AddTicks(7012), new DateTime(2026, 6, 22, 8, 2, 43, 887, DateTimeKind.Utc).AddTicks(7016) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 8, 2, 43, 887, DateTimeKind.Utc).AddTicks(7033), new DateTime(2026, 6, 22, 8, 2, 43, 887, DateTimeKind.Utc).AddTicks(7033) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 8, 2, 43, 887, DateTimeKind.Utc).AddTicks(7039), new DateTime(2026, 6, 22, 8, 2, 43, 887, DateTimeKind.Utc).AddTicks(7039) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 3, 31, 42, 715, DateTimeKind.Utc).AddTicks(1201), new DateTime(2026, 6, 22, 3, 31, 42, 715, DateTimeKind.Utc).AddTicks(1206) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 3, 31, 42, 715, DateTimeKind.Utc).AddTicks(1218), new DateTime(2026, 6, 22, 3, 31, 42, 715, DateTimeKind.Utc).AddTicks(1218) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 3, 31, 42, 715, DateTimeKind.Utc).AddTicks(1222), new DateTime(2026, 6, 22, 3, 31, 42, 715, DateTimeKind.Utc).AddTicks(1223) });
        }
    }
}
