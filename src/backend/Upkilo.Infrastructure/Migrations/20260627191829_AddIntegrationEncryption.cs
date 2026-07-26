using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrationEncryption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EncryptedCredentials",
                table: "TenantIntegrations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "TenantIntegrations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastVerifiedAt",
                table: "TenantIntegrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationError",
                table: "TenantIntegrations",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TenantIntegrationAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    IntegrationId = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    ActorUserId = table.Column<string>(type: "text", nullable: true),
                    ActorIp = table.Column<string>(type: "text", nullable: true),
                    Details = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantIntegrationAudits", x => x.Id);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_WorkingHours_Staff_DayOfWeek",
                table: "StaffWorkingHours",
                columns: new[] { "StaffId", "DayOfWeek" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleExceptions_Staff_Date",
                table: "StaffExceptions",
                columns: new[] { "StaffId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_SlotHolds_Staff_Slot_Released",
                table: "SlotHolds",
                columns: new[] { "StaffId", "SlotDateTime", "IsReleased" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_Token",
                table: "PasswordResetTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Pending",
                table: "OutboxMessages",
                column: "CreatedAt",
                filter: "\"ProcessedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Tenant_User_IsRead",
                table: "Notifications",
                columns: new[] { "TenantId", "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_Email_AttemptedAt",
                table: "LoginAttempts",
                columns: new[] { "Email", "AttemptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationTokens_Token",
                table: "EmailVerificationTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConversionEvents_Tenant_CreatedAt",
                table: "ConversionEvents",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityCaches_Tenant_Staff_Date",
                table: "AvailabilityCaches",
                columns: new[] { "TenantId", "StaffId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantIntegrationAudits");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_WorkingHours_Staff_DayOfWeek",
                table: "StaffWorkingHours");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleExceptions_Staff_Date",
                table: "StaffExceptions");

            migrationBuilder.DropIndex(
                name: "IX_SlotHolds_Staff_Slot_Released",
                table: "SlotHolds");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_PasswordResetTokens_Token",
                table: "PasswordResetTokens");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_Pending",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_Tenant_User_IsRead",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_LoginAttempts_Email_AttemptedAt",
                table: "LoginAttempts");

            migrationBuilder.DropIndex(
                name: "IX_EmailVerificationTokens_Token",
                table: "EmailVerificationTokens");

            migrationBuilder.DropIndex(
                name: "IX_ConversionEvents_Tenant_CreatedAt",
                table: "ConversionEvents");

            migrationBuilder.DropIndex(
                name: "IX_AvailabilityCaches_Tenant_Staff_Date",
                table: "AvailabilityCaches");

            migrationBuilder.DropColumn(
                name: "EncryptedCredentials",
                table: "TenantIntegrations");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "TenantIntegrations");

            migrationBuilder.DropColumn(
                name: "LastVerifiedAt",
                table: "TenantIntegrations");

            migrationBuilder.DropColumn(
                name: "VerificationError",
                table: "TenantIntegrations");

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
    }
}
