using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicPricingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PricingFeatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_PricingFeatures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PricingPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    StripeProductId = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TrialDays = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_PricingPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanFeatureMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PricingPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    PricingFeatureId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    NumericLimit = table.Column<int>(type: "integer", nullable: true),
                    TextValue = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_PlanFeatureMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanFeatureMappings_PricingFeatures_PricingFeatureId",
                        column: x => x.PricingFeatureId,
                        principalTable: "PricingFeatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlanFeatureMappings_PricingPlans_PricingPlanId",
                        column: x => x.PricingPlanId,
                        principalTable: "PricingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanPrices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PricingPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    StripePriceId = table.Column<string>(type: "text", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    CurrencyCode = table.Column<string>(type: "text", nullable: false),
                    Cycle = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_PlanPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanPrices_PricingPlans_PricingPlanId",
                        column: x => x.PricingPlanId,
                        principalTable: "PricingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_PlanFeatureMappings_PricingFeatureId",
                table: "PlanFeatureMappings",
                column: "PricingFeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanFeatureMappings_PricingPlanId",
                table: "PlanFeatureMappings",
                column: "PricingPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanPrices_PricingPlanId",
                table: "PlanPrices",
                column: "PricingPlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanFeatureMappings");

            migrationBuilder.DropTable(
                name: "PlanPrices");

            migrationBuilder.DropTable(
                name: "PricingFeatures");

            migrationBuilder.DropTable(
                name: "PricingPlans");

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 20, 9, 33, 6, 337, DateTimeKind.Utc).AddTicks(8319), new DateTime(2026, 6, 20, 9, 33, 6, 337, DateTimeKind.Utc).AddTicks(8328) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 20, 9, 33, 6, 337, DateTimeKind.Utc).AddTicks(9199), new DateTime(2026, 6, 20, 9, 33, 6, 337, DateTimeKind.Utc).AddTicks(9206) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 20, 9, 33, 6, 337, DateTimeKind.Utc).AddTicks(9226), new DateTime(2026, 6, 20, 9, 33, 6, 337, DateTimeKind.Utc).AddTicks(9227) });
        }
    }
}
