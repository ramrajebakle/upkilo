using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWhiteLabelEmailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerifiedAt",
                table: "WhiteLabelConfigs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "WhiteLabelConfigs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 28, 5, 24, 39, 422, DateTimeKind.Utc).AddTicks(4493), new DateTime(2026, 6, 28, 5, 24, 39, 422, DateTimeKind.Utc).AddTicks(4498) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 28, 5, 24, 39, 422, DateTimeKind.Utc).AddTicks(4529), new DateTime(2026, 6, 28, 5, 24, 39, 422, DateTimeKind.Utc).AddTicks(4529) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 28, 5, 24, 39, 422, DateTimeKind.Utc).AddTicks(4535), new DateTime(2026, 6, 28, 5, 24, 39, 422, DateTimeKind.Utc).AddTicks(4536) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailVerifiedAt",
                table: "WhiteLabelConfigs");

            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "WhiteLabelConfigs");

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 27, 19, 18, 24, 606, DateTimeKind.Utc).AddTicks(4897), new DateTime(2026, 6, 27, 19, 18, 24, 606, DateTimeKind.Utc).AddTicks(4903) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 27, 19, 18, 24, 606, DateTimeKind.Utc).AddTicks(4918), new DateTime(2026, 6, 27, 19, 18, 24, 606, DateTimeKind.Utc).AddTicks(4919) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 27, 19, 18, 24, 606, DateTimeKind.Utc).AddTicks(4928), new DateTime(2026, 6, 27, 19, 18, 24, 606, DateTimeKind.Utc).AddTicks(4928) });
        }
    }
}
