using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase1_MonetizationFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StripeSmsOveragePriceId",
                table: "SubscriptionPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCustom",
                table: "PricingPlans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StripeAiUsagePriceId",
                table: "PricingPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeExtraLocationPriceId",
                table: "PricingPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeExtraStaffPriceId",
                table: "PricingPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeSmsOveragePriceId",
                table: "PricingPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DripEmailSentAt",
                table: "OnboardingProgress",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewRequestSentAt",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 21, 20, 22, 36, 323, DateTimeKind.Utc).AddTicks(639), new DateTime(2026, 6, 21, 20, 22, 36, 323, DateTimeKind.Utc).AddTicks(642) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 21, 20, 22, 36, 323, DateTimeKind.Utc).AddTicks(654), new DateTime(2026, 6, 21, 20, 22, 36, 323, DateTimeKind.Utc).AddTicks(655) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 21, 20, 22, 36, 323, DateTimeKind.Utc).AddTicks(660), new DateTime(2026, 6, 21, 20, 22, 36, 323, DateTimeKind.Utc).AddTicks(660) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StripeSmsOveragePriceId",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "IsCustom",
                table: "PricingPlans");

            migrationBuilder.DropColumn(
                name: "StripeAiUsagePriceId",
                table: "PricingPlans");

            migrationBuilder.DropColumn(
                name: "StripeExtraLocationPriceId",
                table: "PricingPlans");

            migrationBuilder.DropColumn(
                name: "StripeExtraStaffPriceId",
                table: "PricingPlans");

            migrationBuilder.DropColumn(
                name: "StripeSmsOveragePriceId",
                table: "PricingPlans");

            migrationBuilder.DropColumn(
                name: "DripEmailSentAt",
                table: "OnboardingProgress");

            migrationBuilder.DropColumn(
                name: "ReviewRequestSentAt",
                table: "Bookings");

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
        }
    }
}
