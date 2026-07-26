using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "experiments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "QueueName",
                table: "DeadLetterMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "DeadLetterMessages",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GroupBookingRecurrences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    Frequency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DaysOfWeek = table.Column<string[]>(type: "text[]", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartTime = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    MaxParticipants = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupBookingRecurrences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupBookingRecurrences_GroupBookings_ClassId",
                        column: x => x.ClassId,
                        principalTable: "GroupBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MagicLinkTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MagicLinkTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MagicLinkTokens_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 17, 17, 56, 6, 985, DateTimeKind.Utc).AddTicks(8744), new DateTime(2026, 4, 17, 17, 56, 6, 985, DateTimeKind.Utc).AddTicks(8748) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 17, 17, 56, 6, 985, DateTimeKind.Utc).AddTicks(8789), new DateTime(2026, 4, 17, 17, 56, 6, 985, DateTimeKind.Utc).AddTicks(8791) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 17, 17, 56, 6, 985, DateTimeKind.Utc).AddTicks(8820), new DateTime(2026, 4, 17, 17, 56, 6, 985, DateTimeKind.Utc).AddTicks(8822) });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_Tenant_LastVisit",
                table: "Clients",
                columns: new[] { "TenantId", "LastVisitAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Tenant_CreatedAt",
                table: "Bookings",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GroupBookingRecurrences_ClassId",
                table: "GroupBookingRecurrences",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_MagicLinkTokens_ClientId",
                table: "MagicLinkTokens",
                column: "ClientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupBookingRecurrences");

            migrationBuilder.DropTable(
                name: "MagicLinkTokens");

            migrationBuilder.DropIndex(
                name: "IX_Clients_Tenant_LastVisit",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_Tenant_CreatedAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "experiments");

            migrationBuilder.DropColumn(
                name: "QueueName",
                table: "DeadLetterMessages");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "DeadLetterMessages");

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
    }
}
