using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase2_DifferentiationFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AverageRating",
                table: "Tenants",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewCount",
                table: "Tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Tagline",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferralCode",
                table: "PartnerAccounts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeConnectAccountId",
                table: "PartnerAccounts",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EnterpriseLeads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyName = table.Column<string>(type: "text", nullable: false),
                    ContactName = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    TeamSize = table.Column<string>(type: "text", nullable: true),
                    CurrentPlatform = table.Column<string>(type: "text", nullable: true),
                    UseCase = table.Column<string>(type: "text", nullable: true),
                    Message = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_EnterpriseLeads", x => x.Id);
                });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnterpriseLeads");

            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ReviewCount",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Tagline",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ReferralCode",
                table: "PartnerAccounts");

            migrationBuilder.DropColumn(
                name: "StripeConnectAccountId",
                table: "PartnerAccounts");

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
    }
}
