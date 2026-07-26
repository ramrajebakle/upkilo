using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinalTextFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 4, 20, 2, 172, DateTimeKind.Utc).AddTicks(3680), new DateTime(2026, 4, 7, 4, 20, 2, 172, DateTimeKind.Utc).AddTicks(3684) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 4, 20, 2, 172, DateTimeKind.Utc).AddTicks(3729), new DateTime(2026, 4, 7, 4, 20, 2, 172, DateTimeKind.Utc).AddTicks(3729) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 7, 4, 20, 2, 172, DateTimeKind.Utc).AddTicks(3750), new DateTime(2026, 4, 7, 4, 20, 2, 172, DateTimeKind.Utc).AddTicks(3750) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 6, 23, 1, 49, 824, DateTimeKind.Utc).AddTicks(1770), new DateTime(2026, 4, 6, 23, 1, 49, 824, DateTimeKind.Utc).AddTicks(1775) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 6, 23, 1, 49, 824, DateTimeKind.Utc).AddTicks(1849), new DateTime(2026, 4, 6, 23, 1, 49, 824, DateTimeKind.Utc).AddTicks(1851) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 6, 23, 1, 49, 824, DateTimeKind.Utc).AddTicks(1866), new DateTime(2026, 4, 6, 23, 1, 49, 824, DateTimeKind.Utc).AddTicks(1867) });
        }
    }
}
