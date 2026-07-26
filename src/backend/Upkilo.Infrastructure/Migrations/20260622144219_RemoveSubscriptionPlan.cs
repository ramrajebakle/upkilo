using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Upkilo.Core.Entities;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSubscriptionPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_SubscriptionPlans_PlanId",
                table: "Subscriptions");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropTable(
                name: "SubscriptionPlanVersions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_PlanId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "PlanId",
                table: "Subscriptions");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PlanId",
                table: "Subscriptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnnualPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Features = table.Column<PlanFeatures>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    StripeAiUsagePriceId = table.Column<string>(type: "text", nullable: true),
                    StripeExtraLocationPriceId = table.Column<string>(type: "text", nullable: true),
                    StripeExtraStaffPriceId = table.Column<string>(type: "text", nullable: true),
                    StripePriceIdAnnual = table.Column<string>(type: "text", nullable: false),
                    StripePriceIdMonthly = table.Column<string>(type: "text", nullable: false),
                    StripeSmsOveragePriceId = table.Column<string>(type: "text", nullable: true),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    TrialDays = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlanVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnnualPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FeaturesJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    SubscriptionPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlanVersions", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 14, 21, 51, 545, DateTimeKind.Utc).AddTicks(9430), new DateTime(2026, 6, 22, 14, 21, 51, 545, DateTimeKind.Utc).AddTicks(9434) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 14, 21, 51, 545, DateTimeKind.Utc).AddTicks(9445), new DateTime(2026, 6, 22, 14, 21, 51, 545, DateTimeKind.Utc).AddTicks(9446) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 14, 21, 51, 545, DateTimeKind.Utc).AddTicks(9451), new DateTime(2026, 6, 22, 14, 21, 51, 545, DateTimeKind.Utc).AddTicks(9452) });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PlanId",
                table: "Subscriptions",
                column: "PlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_SubscriptionPlans_PlanId",
                table: "Subscriptions",
                column: "PlanId",
                principalTable: "SubscriptionPlans",
                principalColumn: "Id");
        }
    }
}
