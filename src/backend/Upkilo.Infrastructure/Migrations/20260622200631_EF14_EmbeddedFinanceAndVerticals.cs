using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EF14_EmbeddedFinanceAndVerticals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 20, 6, 28, 733, DateTimeKind.Utc).AddTicks(1828), new DateTime(2026, 6, 22, 20, 6, 28, 733, DateTimeKind.Utc).AddTicks(1831) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 20, 6, 28, 733, DateTimeKind.Utc).AddTicks(1869), new DateTime(2026, 6, 22, 20, 6, 28, 733, DateTimeKind.Utc).AddTicks(1869) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 20, 6, 28, 733, DateTimeKind.Utc).AddTicks(1874), new DateTime(2026, 6, 22, 20, 6, 28, 733, DateTimeKind.Utc).AddTicks(1874) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 20, 0, 48, 268, DateTimeKind.Utc).AddTicks(6351), new DateTime(2026, 6, 22, 20, 0, 48, 268, DateTimeKind.Utc).AddTicks(6354) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 20, 0, 48, 268, DateTimeKind.Utc).AddTicks(6400), new DateTime(2026, 6, 22, 20, 0, 48, 268, DateTimeKind.Utc).AddTicks(6401) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 20, 0, 48, 268, DateTimeKind.Utc).AddTicks(6405), new DateTime(2026, 6, 22, 20, 0, 48, 268, DateTimeKind.Utc).AddTicks(6406) });
        }
    }
}
