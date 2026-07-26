using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingPlanToSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PricingPlanId",
                table: "Subscriptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 21, 18, 16, 5, 227, DateTimeKind.Utc).AddTicks(988), new DateTime(2026, 6, 21, 18, 16, 5, 227, DateTimeKind.Utc).AddTicks(991) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 21, 18, 16, 5, 227, DateTimeKind.Utc).AddTicks(1069), new DateTime(2026, 6, 21, 18, 16, 5, 227, DateTimeKind.Utc).AddTicks(1069) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 21, 18, 16, 5, 227, DateTimeKind.Utc).AddTicks(1075), new DateTime(2026, 6, 21, 18, 16, 5, 227, DateTimeKind.Utc).AddTicks(1075) });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PricingPlanId",
                table: "Subscriptions",
                column: "PricingPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_PricingPlans_PricingPlanId",
                table: "Subscriptions",
                column: "PricingPlanId",
                principalTable: "PricingPlans",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_PricingPlans_PricingPlanId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_PricingPlanId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "PricingPlanId",
                table: "Subscriptions");

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
        }
    }
}
