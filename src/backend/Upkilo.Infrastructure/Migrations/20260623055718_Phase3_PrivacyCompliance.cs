using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_PrivacyCompliance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DoNotSell",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DoNotSellUpdatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CcpaDeletionRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestNumber = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueBy = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    FulfilledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_CcpaDeletionRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CookieConsentRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Essential = table.Column<bool>(type: "boolean", nullable: false),
                    Analytics = table.Column<bool>(type: "boolean", nullable: false),
                    Marketing = table.Column<bool>(type: "boolean", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    ConsentVersion = table.Column<string>(type: "text", nullable: false),
                    ConsentedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_CookieConsentRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LegalDisclosureRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "text", nullable: false),
                    RequestType = table.Column<string>(type: "text", nullable: false),
                    IssuingAuthority = table.Column<string>(type: "text", nullable: false),
                    IssuingJurisdiction = table.Column<string>(type: "text", nullable: false),
                    StatutoryCitation = table.Column<string>(type: "text", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResponseDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DataCategoriesRequested = table.Column<string>(type: "text", nullable: true),
                    DataCategoriesProvided = table.Column<string>(type: "text", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    UserNotified = table.Column<bool>(type: "boolean", nullable: false),
                    UserNotifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NotificationLegallyProhibited = table.Column<bool>(type: "boolean", nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LegalCounselNotes = table.Column<string>(type: "text", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FulfilledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_LegalDisclosureRequests", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 23, 5, 57, 14, 981, DateTimeKind.Utc).AddTicks(2736), new DateTime(2026, 6, 23, 5, 57, 14, 981, DateTimeKind.Utc).AddTicks(2739) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 23, 5, 57, 14, 981, DateTimeKind.Utc).AddTicks(2752), new DateTime(2026, 6, 23, 5, 57, 14, 981, DateTimeKind.Utc).AddTicks(2753) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 23, 5, 57, 14, 981, DateTimeKind.Utc).AddTicks(2757), new DateTime(2026, 6, 23, 5, 57, 14, 981, DateTimeKind.Utc).AddTicks(2758) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CcpaDeletionRequests");

            migrationBuilder.DropTable(
                name: "CookieConsentRecords");

            migrationBuilder.DropTable(
                name: "LegalDisclosureRequests");

            migrationBuilder.DropColumn(
                name: "DoNotSell",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DoNotSellUpdatedAt",
                table: "Users");

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
    }
}
