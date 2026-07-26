using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformDiscounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PricingPlanId",
                table: "Tenants",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlatformDiscounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MaxRedemptions = table.Column<int>(type: "integer", nullable: true),
                    CurrentRedemptions = table.Column<int>(type: "integer", nullable: false),
                    StripeCouponId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformDiscounts", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 21, 18, 10, 49, 495, DateTimeKind.Utc).AddTicks(2620), new DateTime(2026, 6, 21, 18, 10, 49, 495, DateTimeKind.Utc).AddTicks(2622) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 21, 18, 10, 49, 495, DateTimeKind.Utc).AddTicks(2636), new DateTime(2026, 6, 21, 18, 10, 49, 495, DateTimeKind.Utc).AddTicks(2636) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 21, 18, 10, 49, 495, DateTimeKind.Utc).AddTicks(2642), new DateTime(2026, 6, 21, 18, 10, 49, 495, DateTimeKind.Utc).AddTicks(2642) });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_PricingPlanId",
                table: "Tenants",
                column: "PricingPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tenants_PricingPlans_PricingPlanId",
                table: "Tenants",
                column: "PricingPlanId",
                principalTable: "PricingPlans",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tenants_PricingPlans_PricingPlanId",
                table: "Tenants");

            migrationBuilder.DropTable(
                name: "PlatformDiscounts");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_PricingPlanId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PricingPlanId",
                table: "Tenants");

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 21, 7, 17, 54, 16, DateTimeKind.Utc).AddTicks(2203), new DateTime(2026, 6, 21, 7, 17, 54, 16, DateTimeKind.Utc).AddTicks(2206) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 21, 7, 17, 54, 16, DateTimeKind.Utc).AddTicks(2223), new DateTime(2026, 6, 21, 7, 17, 54, 16, DateTimeKind.Utc).AddTicks(2223) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 21, 7, 17, 54, 16, DateTimeKind.Utc).AddTicks(2230), new DateTime(2026, 6, 21, 7, 17, 54, 16, DateTimeKind.Utc).AddTicks(2230) });
        }
    }
}
