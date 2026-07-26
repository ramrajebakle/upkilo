using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase1Extensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.DropForeignKey(
                name: "FK_PromotionRedemptions_PromotionCodes_PromotionCodeId",
                table: "PromotionRedemptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_CustomRole_CustomRoleId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "PromotionCodes");

            migrationBuilder.DropTable(
                name: "TenantSubscriptions");

            migrationBuilder.DropTable(
                name: "WebhookEndpoints");

            migrationBuilder.DropIndex(
                name: "IX_StaffMembers_TenantId",
                table: "StaffMembers");

            migrationBuilder.DropIndex(
                name: "IX_Services_TenantId",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TenantId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Locations_TenantId",
                table: "Locations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CustomRole",
                table: "CustomRole");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "WaiverSignatures");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WaiverSignatures");

            migrationBuilder.DropColumn(
                name: "Columns",
                table: "ReportDefinitions");

            migrationBuilder.DropColumn(
                name: "DataSource",
                table: "ReportDefinitions");

            migrationBuilder.DropColumn(
                name: "Filters",
                table: "ReportDefinitions");

            migrationBuilder.DropColumn(
                name: "GroupBy",
                table: "ReportDefinitions");

            migrationBuilder.DropColumn(
                name: "AllowBookingHistory",
                table: "ClientPortalConfigs");

            migrationBuilder.DropColumn(
                name: "AllowDocumentAccess",
                table: "ClientPortalConfigs");

            migrationBuilder.DropColumn(
                name: "AllowForms",
                table: "ClientPortalConfigs");

            migrationBuilder.RenameTable(
                name: "CustomRole",
                newName: "CustomRoles");

            migrationBuilder.RenameColumn(
                name: "SortBy",
                table: "ReportDefinitions",
                newName: "Recipients");

            migrationBuilder.RenameColumn(
                name: "ScheduleRecipients",
                table: "ReportDefinitions",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "ScheduleCron",
                table: "ReportDefinitions",
                newName: "CronSchedule");

            migrationBuilder.RenameColumn(
                name: "IsTemplate",
                table: "ReportDefinitions",
                newName: "IsScheduled");

            migrationBuilder.RenameColumn(
                name: "ExportFormat",
                table: "ReportDefinitions",
                newName: "ConfigJson");

            migrationBuilder.RenameColumn(
                name: "PromotionCodeId",
                table: "PromotionRedemptions",
                newName: "PromoCodeId");

            migrationBuilder.RenameIndex(
                name: "IX_PromotionRedemptions_PromotionCodeId",
                table: "PromotionRedemptions",
                newName: "IX_PromotionRedemptions_PromoCodeId");

            migrationBuilder.RenameColumn(
                name: "ApplicableServices",
                table: "DigitalWaivers",
                newName: "ApplicableServiceIds");

            migrationBuilder.RenameIndex(
                name: "IX_Clients_TenantId_Email",
                table: "Clients",
                newName: "IX_Clients_Tenant_Email");

            migrationBuilder.RenameColumn(
                name: "IsEnabled",
                table: "ClientPortalConfigs",
                newName: "ShowStaffSelection");

            migrationBuilder.RenameColumn(
                name: "CustomBranding",
                table: "ClientPortalConfigs",
                newName: "SocialLinksJson");

            migrationBuilder.RenameColumn(
                name: "AllowProfileUpdate",
                table: "ClientPortalConfigs",
                newName: "ShowPrice");

            migrationBuilder.RenameColumn(
                name: "AllowMessaging",
                table: "ClientPortalConfigs",
                newName: "AllowSelfRegistration");

            migrationBuilder.RenameColumn(
                name: "AllowInvoiceDownload",
                table: "ClientPortalConfigs",
                newName: "AllowSelfCancellation");

            migrationBuilder.RenameColumn(
                name: "PerformedByName",
                table: "AuditEntries",
                newName: "UserName");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:hstore", ",,");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastTriggeredAt",
                table: "Webhooks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedFromIP",
                table: "WaiverSignatures",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "WaiverSignatures",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WaiverVersion",
                table: "WaiverSignatures",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailCode",
                table: "User2FAs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailCodeExpiresAt",
                table: "User2FAs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FailedAttempts",
                table: "User2FAs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntil",
                table: "User2FAs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmsCode",
                table: "User2FAs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SmsCodeExpiresAt",
                table: "User2FAs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessName",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessType",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Sector",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tier",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalAccountId",
                table: "TenantIntegrations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntegrationType",
                table: "TenantIntegrations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "TenantIntegrations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "TenantIntegrations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StripeExtraLocationPriceId",
                table: "SubscriptionPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeExtraStaffPriceId",
                table: "SubscriptionPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsConverted",
                table: "SlotHolds",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Duration",
                table: "Services",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "ReportDefinitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "ReportDefinitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "ReportDefinitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("ALTER TABLE \"PromoCodes\" ALTER COLUMN \"DiscountType\" TYPE integer USING \"DiscountType\"::integer;");

            migrationBuilder.AlterColumn<int>(
                name: "DiscountType",
                table: "PromoCodes",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "PromoCodes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "PromoCodes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxDiscountAmount",
                table: "PromoCodes",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxUsagePerCustomer",
                table: "PromoCodes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartsAt",
                table: "PromoCodes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "PipelineStages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "OutboxMessages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Error",
                table: "OutboxMessages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                table: "OutboxMessages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "InvoiceItems",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "InvoiceItems",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "InvoiceItems",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "DigitalWaivers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("ALTER TABLE \"Deals\" ALTER COLUMN \"Status\" TYPE integer USING \"Status\"::integer;");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Deals",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualCloseDate",
                table: "Deals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedToStaffId",
                table: "Deals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LostReason",
                table: "Deals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Deals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Deals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastVisitAt",
                table: "Clients",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancellationPolicyHours",
                table: "ClientPortalConfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CustomDomain",
                table: "ClientPortalConfigs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "ClientPortalConfigs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PortalName",
                table: "ClientPortalConfigs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrimaryColor",
                table: "ClientPortalConfigs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "EntityId",
                table: "AuditEntries",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "Details",
                table: "AuditEntries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Timestamp",
                table: "AuditEntries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "AuditEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("ALTER TABLE \"ApiKeys\" ALTER COLUMN \"Scopes\" TYPE jsonb USING to_jsonb(\"Scopes\");");

            migrationBuilder.AlterColumn<List<string>>(
                name: "Scopes",
                table: "ApiKeys",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(List<string>),
                oldType: "text[]");

            migrationBuilder.AddColumn<int>(
                name: "InputTokens",
                table: "AIDecisionLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "AIDecisionLogs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "OutputTokens",
                table: "AIDecisionLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("ALTER TABLE \"AIAgentConfigs\" ALTER COLUMN \"HandoffTriggers\" TYPE jsonb USING \"HandoffTriggers\"::jsonb;");

            migrationBuilder.AlterColumn<Dictionary<string, string>>(
                name: "HandoffTriggers",
                table: "AIAgentConfigs",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(Dictionary<string, string>),
                oldType: "hstore");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CustomRoles",
                table: "CustomRoles",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "AffiliatePayouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    PayoutMethod = table.Column<string>(type: "text", nullable: false),
                    TransactionReference = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffiliatePayouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AffiliatePayouts_PartnerAccounts_PartnerAccountId",
                        column: x => x.PartnerAccountId,
                        principalTable: "PartnerAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AvailabilitySnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AvailabilityJson = table.Column<string>(type: "jsonb", nullable: false),
                    Hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvailabilitySnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BookingReminders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Timing = table.Column<int>(type: "integer", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsSent = table.Column<bool>(type: "boolean", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingReminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingReminders_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingReminders_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommissionRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServiceCategory = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    MinAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    MaxAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommissionRules_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommissionRules_StaffMembers_StaffId",
                        column: x => x.StaffId,
                        principalTable: "StaffMembers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Consents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    HasConsented = table.Column<bool>(type: "boolean", nullable: false),
                    ConsentedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    ConsentSource = table.Column<string>(type: "text", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Consents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DataExports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DownloadUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Format = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataExports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeadLetterMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: false),
                    StackTrace = table.Column<string>(type: "text", nullable: true),
                    OriginalRetryCount = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<string>(type: "text", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeadLetterMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DunningCycles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StripeInvoiceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SubscriptionSuspended = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DunningCycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErrorLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    StackTrace = table.Column<string>(type: "text", nullable: true),
                    Level = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestPath = table.Column<string>(type: "text", nullable: true),
                    RequestMethod = table.Column<string>(type: "text", nullable: true),
                    QueryString = table.Column<string>(type: "text", nullable: true),
                    RequestBody = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GdprConsents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsentType = table.Column<string>(type: "text", nullable: false),
                    IsGranted = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GdprConsents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GdprConsents_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupBookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MasterBookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizerId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupName = table.Column<string>(type: "text", nullable: true),
                    MaxParticipants = table.Column<int>(type: "integer", nullable: false),
                    CurrentParticipants = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupBookings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IdempotencyRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "text", nullable: false),
                    ResponseStatusCode = table.Column<int>(type: "integer", nullable: false),
                    ResponseBody = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobQuotas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LimitPerMonth = table.Column<int>(type: "integer", nullable: false),
                    CurrentUsage = table.Column<int>(type: "integer", nullable: false),
                    ResetDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobQuotas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LegalAgreements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgreementType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DocumentUrl = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    AcceptedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalAgreements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoginHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: false),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    Browser = table.Column<string>(type: "text", nullable: true),
                    OperatingSystem = table.Column<string>(type: "text", nullable: true),
                    DeviceType = table.Column<string>(type: "text", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: true),
                    Result = table.Column<int>(type: "integer", nullable: false),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    AttemptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsSuspicious = table.Column<bool>(type: "boolean", nullable: false),
                    SuspiciousReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketingTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "text", nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MembershipContents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    RequiredPlanIds = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipContents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationFallbackChannels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationType = table.Column<string>(type: "text", nullable: false),
                    PrimaryChannel = table.Column<string>(type: "text", nullable: false),
                    FirstFallbackChannel = table.Column<string>(type: "text", nullable: false),
                    SecondFallbackChannel = table.Column<string>(type: "text", nullable: true),
                    TimeoutSecondsBeforeFallback = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationFallbackChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationFallbackChannels_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SmsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    PushEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    InAppEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    WhatsAppEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    BookingConfirmations = table.Column<bool>(type: "boolean", nullable: false),
                    BookingReminders = table.Column<bool>(type: "boolean", nullable: false),
                    BookingCancellations = table.Column<bool>(type: "boolean", nullable: false),
                    PaymentReceipts = table.Column<bool>(type: "boolean", nullable: false),
                    MarketingEmails = table.Column<bool>(type: "boolean", nullable: false),
                    PromotionalOffers = table.Column<bool>(type: "boolean", nullable: false),
                    LoyaltyUpdates = table.Column<bool>(type: "boolean", nullable: false),
                    ReviewRequests = table.Column<bool>(type: "boolean", nullable: false),
                    SecurityAlerts = table.Column<bool>(type: "boolean", nullable: false),
                    SystemUpdates = table.Column<bool>(type: "boolean", nullable: false),
                    QuietHoursStart = table.Column<string>(type: "text", nullable: true),
                    QuietHoursEnd = table.Column<string>(type: "text", nullable: true),
                    PreferredTimezone = table.Column<string>(type: "text", nullable: true),
                    ChannelPriority = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    TemplateKey = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    EmailSubject = table.Column<string>(type: "text", nullable: true),
                    EmailBody = table.Column<string>(type: "text", nullable: true),
                    SmsBody = table.Column<string>(type: "text", nullable: true),
                    PushTitle = table.Column<string>(type: "text", nullable: true),
                    PushBody = table.Column<string>(type: "text", nullable: true),
                    WhatsAppBody = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    Variables = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationTemplates_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingProgress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessProfileCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    WorkingHoursCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    ServicesAdded = table.Column<bool>(type: "boolean", nullable: false),
                    StaffAdded = table.Column<bool>(type: "boolean", nullable: false),
                    BookingPageCustomized = table.Column<bool>(type: "boolean", nullable: false),
                    PaymentSetupCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    FirstBookingCreated = table.Column<bool>(type: "boolean", nullable: false),
                    ClientsImported = table.Column<bool>(type: "boolean", nullable: false),
                    BusinessProfileCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WorkingHoursCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ServicesAddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StaffAddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BookingPageCustomizedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentSetupCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FirstBookingCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClientsImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDismissed = table.Column<bool>(type: "boolean", nullable: false),
                    DismissedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SampleDataTemplate = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingProgress_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentDisputes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StripeDisputeId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StripeChargeId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentDisputes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Referrals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferrerTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferredTenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferralCode = table.Column<string>(type: "text", nullable: false),
                    ReferredEmail = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReferrerCreditAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ReferredCreditAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ReferrerCredited = table.Column<bool>(type: "boolean", nullable: false),
                    ReferredCredited = table.Column<bool>(type: "boolean", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreditedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Referrals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Referrals_Tenants_ReferredTenantId",
                        column: x => x.ReferredTenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Referrals_Tenants_ReferrerTenantId",
                        column: x => x.ReferrerTenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "text", nullable: true),
                    ReasonRevoked = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SecurityEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Details = table.Column<string>(type: "text", nullable: true),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlanVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    AnnualPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    FeaturesJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlanVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    BillingInterval = table.Column<int>(type: "integer", nullable: false),
                    CurrentPeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentPeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PausedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResumeAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StripeSubscriptionId = table.Column<string>(type: "text", nullable: true),
                    StripeCustomerId = table.Column<string>(type: "text", nullable: true),
                    StripePaymentMethodId = table.Column<string>(type: "text", nullable: true),
                    BookingsUsed = table.Column<int>(type: "integer", nullable: false),
                    SmsUsed = table.Column<int>(type: "integer", nullable: false),
                    AiCreditsUsed = table.Column<int>(type: "integer", nullable: false),
                    StorageUsedBytes = table.Column<long>(type: "bigint", nullable: false),
                    ExtraStaffCount = table.Column<int>(type: "integer", nullable: false),
                    ExtraLocationCount = table.Column<int>(type: "integer", nullable: false),
                    LastAlertThreshold = table.Column<int>(type: "integer", nullable: false),
                    AiMonthlyBudget = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AiLastAlertThreshold = table.Column<int>(type: "integer", nullable: false),
                    AuditLogRetentionDays = table.Column<int>(type: "integer", nullable: false),
                    AllowedAiModels = table.Column<List<string>>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Subscriptions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedToAdminId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportTickets_Users_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantStatusHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NewStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantStatusHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Percentage = table.Column<decimal>(type: "numeric", nullable: true),
                    PaymentMethod = table.Column<string>(type: "text", nullable: false),
                    IsDistributed = table.Column<bool>(type: "boolean", nullable: false),
                    DistributedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StripePaymentIntentId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tips_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tips_StaffMembers_StaffId",
                        column: x => x.StaffId,
                        principalTable: "StaffMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TwoFaRecoveryRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdentityVerificationData = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AdminNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ResolvedByAdminId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TwoFaRecoveryRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    EndpointUrl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    NextRetryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AffiliateCommissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Source = table.Column<string>(type: "text", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    CommissionRate = table.Column<decimal>(type: "numeric", nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PayoutId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffiliateCommissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AffiliateCommissions_AffiliatePayouts_PayoutId",
                        column: x => x.PayoutId,
                        principalTable: "AffiliatePayouts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AffiliateCommissions_PartnerAccounts_PartnerAccountId",
                        column: x => x.PartnerAccountId,
                        principalTable: "PartnerAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupBookingParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupBookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    GuestName = table.Column<string>(type: "text", nullable: true),
                    GuestEmail = table.Column<string>(type: "text", nullable: true),
                    GuestPhone = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IndividualPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupBookingParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupBookingParticipants_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GroupBookingParticipants_GroupBookings_GroupBookingId",
                        column: x => x.GroupBookingId,
                        principalTable: "GroupBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MembershipModules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    DripDaysDelay = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipModules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MembershipModules_MembershipContents_MembershipContentId",
                        column: x => x.MembershipContentId,
                        principalTable: "MembershipContents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MembershipLessons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipModuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    BodyHtml = table.Column<string>(type: "text", nullable: true),
                    VideoUrl = table.Column<string>(type: "text", nullable: true),
                    AttachmentUrl = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipLessons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MembershipLessons_MembershipModules_MembershipModuleId",
                        column: x => x.MembershipModuleId,
                        principalTable: "MembershipModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientContentProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipLessonId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastPositionSeconds = table.Column<int>(type: "integer", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientContentProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientContentProgresses_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientContentProgresses_MembershipLessons_MembershipLessonId",
                        column: x => x.MembershipLessonId,
                        principalTable: "MembershipLessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WaiverSignatures_ClientId",
                table: "WaiverSignatures",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_IsDeleted",
                table: "Users",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_StaffMembers_TenantId_Email",
                table: "StaffMembers",
                columns: new[] { "TenantId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Services_TenantId_Name",
                table: "Services",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Metadata",
                table: "Payments",
                column: "Metadata")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Tenant_CreatedAt_Status",
                table: "Payments",
                columns: new[] { "TenantId", "CreatedAt", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Locations_TenantId_Name",
                table: "Locations",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DigitalWaivers_TenantId",
                table: "DigitalWaivers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationLogs_Metadata",
                table: "CommunicationLogs",
                column: "Metadata")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_FullText",
                table: "Clients",
                columns: new[] { "Email", "FirstName", "LastName" })
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops", "gin_trgm_ops", "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_TenantId_IsDeleted",
                table: "Clients",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Tenant_Staff_StartTime",
                table: "Bookings",
                columns: new[] { "TenantId", "StaffId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Tenant_StartTime_Status",
                table: "Bookings",
                columns: new[] { "TenantId", "StartTime", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TenantId_IsDeleted",
                table: "Bookings",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_Tenant_PerformedAt",
                table: "AuditEntries",
                columns: new[] { "TenantId", "PerformedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomRoles_TenantId_Name",
                table: "CustomRoles",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateCommissions_PartnerAccountId",
                table: "AffiliateCommissions",
                column: "PartnerAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AffiliateCommissions_PayoutId",
                table: "AffiliateCommissions",
                column: "PayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_AffiliatePayouts_PartnerAccountId",
                table: "AffiliatePayouts",
                column: "PartnerAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingReminders_BookingId",
                table: "BookingReminders",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingReminders_ClientId",
                table: "BookingReminders",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientContentProgresses_ClientId",
                table: "ClientContentProgresses",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientContentProgresses_MembershipLessonId",
                table: "ClientContentProgresses",
                column: "MembershipLessonId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRules_ServiceId",
                table: "CommissionRules",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRules_StaffId",
                table: "CommissionRules",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRules_Tenant_Staff_Service",
                table: "CommissionRules",
                columns: new[] { "TenantId", "StaffId", "ServiceId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Consents_UserId",
                table: "Consents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GdprConsents_ClientId",
                table: "GdprConsents",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_GdprConsents_TenantId_ClientId_ConsentType",
                table: "GdprConsents",
                columns: new[] { "TenantId", "ClientId", "ConsentType" });

            migrationBuilder.CreateIndex(
                name: "IX_GroupBookingParticipants_ClientId",
                table: "GroupBookingParticipants",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupBookingParticipants_GroupBookingId",
                table: "GroupBookingParticipants",
                column: "GroupBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_ExpiresAt",
                table: "IdempotencyRecords",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_Key",
                table: "IdempotencyRecords",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistories_UserId",
                table: "LoginHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipLessons_MembershipModuleId",
                table: "MembershipLessons",
                column: "MembershipModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipModules_MembershipContentId",
                table: "MembershipModules",
                column: "MembershipContentId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationFallbackChannels_UserId",
                table: "NotificationFallbackChannels",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_UserId",
                table: "NotificationPreferences",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplates_TenantId",
                table: "NotificationTemplates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingProgress_TenantId",
                table: "OnboardingProgress",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferredTenantId",
                table: "Referrals",
                column: "ReferredTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferrerTenantId",
                table: "Referrals",
                column: "ReferrerTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_Tenant_CreatedAt",
                table: "SecurityEvents",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PlanId",
                table: "Subscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TenantId",
                table: "Subscriptions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_SubmittedByUserId",
                table: "SupportTickets",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tips_BookingId",
                table: "Tips",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Tips_StaffId",
                table: "Tips",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_Status_NextRetryAt",
                table: "WebhookEvents",
                columns: new[] { "Status", "NextRetryAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_TenantId_Status",
                table: "WebhookEvents",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_DigitalWaivers_Tenants_TenantId",
                table: "DigitalWaivers",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PromotionRedemptions_PromoCodes_PromoCodeId",
                table: "PromotionRedemptions",
                column: "PromoCodeId",
                principalTable: "PromoCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_CustomRoles_CustomRoleId",
                table: "Users",
                column: "CustomRoleId",
                principalTable: "CustomRoles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WaiverSignatures_Clients_ClientId",
                table: "WaiverSignatures",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DigitalWaivers_Tenants_TenantId",
                table: "DigitalWaivers");

            migrationBuilder.DropForeignKey(
                name: "FK_PromotionRedemptions_PromoCodes_PromoCodeId",
                table: "PromotionRedemptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_CustomRoles_CustomRoleId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_WaiverSignatures_Clients_ClientId",
                table: "WaiverSignatures");

            migrationBuilder.DropTable(
                name: "AffiliateCommissions");

            migrationBuilder.DropTable(
                name: "AvailabilitySnapshots");

            migrationBuilder.DropTable(
                name: "BookingReminders");

            migrationBuilder.DropTable(
                name: "ClientContentProgresses");

            migrationBuilder.DropTable(
                name: "CommissionRules");

            migrationBuilder.DropTable(
                name: "Consents");

            migrationBuilder.DropTable(
                name: "DataExports");

            migrationBuilder.DropTable(
                name: "DeadLetterMessages");

            migrationBuilder.DropTable(
                name: "DunningCycles");

            migrationBuilder.DropTable(
                name: "ErrorLogs");

            migrationBuilder.DropTable(
                name: "GdprConsents");

            migrationBuilder.DropTable(
                name: "GroupBookingParticipants");

            migrationBuilder.DropTable(
                name: "IdempotencyRecords");

            migrationBuilder.DropTable(
                name: "JobQuotas");

            migrationBuilder.DropTable(
                name: "LegalAgreements");

            migrationBuilder.DropTable(
                name: "LoginHistories");

            migrationBuilder.DropTable(
                name: "MarketingTemplates");

            migrationBuilder.DropTable(
                name: "NotificationFallbackChannels");

            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.DropTable(
                name: "NotificationTemplates");

            migrationBuilder.DropTable(
                name: "OnboardingProgress");

            migrationBuilder.DropTable(
                name: "PaymentDisputes");

            migrationBuilder.DropTable(
                name: "Referrals");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "SecurityEvents");

            migrationBuilder.DropTable(
                name: "SubscriptionPlanVersions");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "SupportTickets");

            migrationBuilder.DropTable(
                name: "TenantStatusHistories");

            migrationBuilder.DropTable(
                name: "Tips");

            migrationBuilder.DropTable(
                name: "TwoFaRecoveryRequests");

            migrationBuilder.DropTable(
                name: "WebhookEvents");

            migrationBuilder.DropTable(
                name: "AffiliatePayouts");

            migrationBuilder.DropTable(
                name: "MembershipLessons");

            migrationBuilder.DropTable(
                name: "GroupBookings");

            migrationBuilder.DropTable(
                name: "MembershipModules");

            migrationBuilder.DropTable(
                name: "MembershipContents");

            migrationBuilder.DropIndex(
                name: "IX_WaiverSignatures_ClientId",
                table: "WaiverSignatures");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId_IsDeleted",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_StaffMembers_TenantId_Email",
                table: "StaffMembers");

            migrationBuilder.DropIndex(
                name: "IX_Services_TenantId_Name",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Metadata",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Tenant_CreatedAt_Status",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Locations_TenantId_Name",
                table: "Locations");

            migrationBuilder.DropIndex(
                name: "IX_DigitalWaivers_TenantId",
                table: "DigitalWaivers");

            migrationBuilder.DropIndex(
                name: "IX_CommunicationLogs_Metadata",
                table: "CommunicationLogs");

            migrationBuilder.DropIndex(
                name: "IX_Clients_FullText",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_Clients_TenantId_IsDeleted",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_Tenant_Staff_StartTime",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_Tenant_StartTime_Status",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_TenantId_IsDeleted",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_AuditEntries_Tenant_PerformedAt",
                table: "AuditEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CustomRoles",
                table: "CustomRoles");

            migrationBuilder.DropIndex(
                name: "IX_CustomRoles_TenantId_Name",
                table: "CustomRoles");

            migrationBuilder.DropColumn(
                name: "LastTriggeredAt",
                table: "Webhooks");

            migrationBuilder.DropColumn(
                name: "SignedFromIP",
                table: "WaiverSignatures");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "WaiverSignatures");

            migrationBuilder.DropColumn(
                name: "WaiverVersion",
                table: "WaiverSignatures");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailCode",
                table: "User2FAs");

            migrationBuilder.DropColumn(
                name: "EmailCodeExpiresAt",
                table: "User2FAs");

            migrationBuilder.DropColumn(
                name: "FailedAttempts",
                table: "User2FAs");

            migrationBuilder.DropColumn(
                name: "LockedUntil",
                table: "User2FAs");

            migrationBuilder.DropColumn(
                name: "SmsCode",
                table: "User2FAs");

            migrationBuilder.DropColumn(
                name: "SmsCodeExpiresAt",
                table: "User2FAs");

            migrationBuilder.DropColumn(
                name: "BusinessName",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "BusinessType",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Sector",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ExternalAccountId",
                table: "TenantIntegrations");

            migrationBuilder.DropColumn(
                name: "IntegrationType",
                table: "TenantIntegrations");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "TenantIntegrations");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "TenantIntegrations");

            migrationBuilder.DropColumn(
                name: "StripeExtraLocationPriceId",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "StripeExtraStaffPriceId",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "IsConverted",
                table: "SlotHolds");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "ReportDefinitions");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "ReportDefinitions");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "ReportDefinitions");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "PromoCodes");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "PromoCodes");

            migrationBuilder.DropColumn(
                name: "MaxDiscountAmount",
                table: "PromoCodes");

            migrationBuilder.DropColumn(
                name: "MaxUsagePerCustomer",
                table: "PromoCodes");

            migrationBuilder.DropColumn(
                name: "StartsAt",
                table: "PromoCodes");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "PipelineStages");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "DigitalWaivers");

            migrationBuilder.DropColumn(
                name: "ActualCloseDate",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "AssignedToStaffId",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "LostReason",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "LastVisitAt",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "CancellationPolicyHours",
                table: "ClientPortalConfigs");

            migrationBuilder.DropColumn(
                name: "CustomDomain",
                table: "ClientPortalConfigs");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "ClientPortalConfigs");

            migrationBuilder.DropColumn(
                name: "PortalName",
                table: "ClientPortalConfigs");

            migrationBuilder.DropColumn(
                name: "PrimaryColor",
                table: "ClientPortalConfigs");

            migrationBuilder.DropColumn(
                name: "Details",
                table: "AuditEntries");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "AuditEntries");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "AuditEntries");

            migrationBuilder.DropColumn(
                name: "InputTokens",
                table: "AIDecisionLogs");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "AIDecisionLogs");

            migrationBuilder.DropColumn(
                name: "OutputTokens",
                table: "AIDecisionLogs");

            migrationBuilder.RenameTable(
                name: "CustomRoles",
                newName: "CustomRole");

            migrationBuilder.RenameColumn(
                name: "Recipients",
                table: "ReportDefinitions",
                newName: "SortBy");

            migrationBuilder.RenameColumn(
                name: "IsScheduled",
                table: "ReportDefinitions",
                newName: "IsTemplate");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "ReportDefinitions",
                newName: "ScheduleRecipients");

            migrationBuilder.RenameColumn(
                name: "CronSchedule",
                table: "ReportDefinitions",
                newName: "ScheduleCron");

            migrationBuilder.RenameColumn(
                name: "ConfigJson",
                table: "ReportDefinitions",
                newName: "ExportFormat");

            migrationBuilder.RenameColumn(
                name: "PromoCodeId",
                table: "PromotionRedemptions",
                newName: "PromotionCodeId");

            migrationBuilder.RenameIndex(
                name: "IX_PromotionRedemptions_PromoCodeId",
                table: "PromotionRedemptions",
                newName: "IX_PromotionRedemptions_PromotionCodeId");

            migrationBuilder.RenameColumn(
                name: "ApplicableServiceIds",
                table: "DigitalWaivers",
                newName: "ApplicableServices");

            migrationBuilder.RenameIndex(
                name: "IX_Clients_Tenant_Email",
                table: "Clients",
                newName: "IX_Clients_TenantId_Email");

            migrationBuilder.RenameColumn(
                name: "SocialLinksJson",
                table: "ClientPortalConfigs",
                newName: "CustomBranding");

            migrationBuilder.RenameColumn(
                name: "ShowStaffSelection",
                table: "ClientPortalConfigs",
                newName: "IsEnabled");

            migrationBuilder.RenameColumn(
                name: "ShowPrice",
                table: "ClientPortalConfigs",
                newName: "AllowProfileUpdate");

            migrationBuilder.RenameColumn(
                name: "AllowSelfRegistration",
                table: "ClientPortalConfigs",
                newName: "AllowMessaging");

            migrationBuilder.RenameColumn(
                name: "AllowSelfCancellation",
                table: "ClientPortalConfigs",
                newName: "AllowInvoiceDownload");

            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "AuditEntries",
                newName: "PerformedByName");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:hstore", ",,");

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "WaiverSignatures",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "WaiverSignatures",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Columns",
                table: "ReportDefinitions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataSource",
                table: "ReportDefinitions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Filters",
                table: "ReportDefinitions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GroupBy",
                table: "ReportDefinitions",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DiscountType",
                table: "PromoCodes",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "OutboxMessages",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Error",
                table: "OutboxMessages",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                table: "OutboxMessages",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Deals",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<bool>(
                name: "AllowBookingHistory",
                table: "ClientPortalConfigs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowDocumentAccess",
                table: "ClientPortalConfigs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowForms",
                table: "ClientPortalConfigs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "EntityId",
                table: "AuditEntries",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<List<string>>(
                name: "Scopes",
                table: "ApiKeys",
                type: "text[]",
                nullable: false,
                oldClrType: typeof(List<string>),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<Dictionary<string, string>>(
                name: "HandoffTriggers",
                table: "AIAgentConfigs",
                type: "hstore",
                nullable: false,
                oldClrType: typeof(Dictionary<string, string>),
                oldType: "jsonb");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CustomRole",
                table: "CustomRole",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<string>(type: "text", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    NewValues = table.Column<string>(type: "text", nullable: true),
                    OldValues = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromotionCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentRedemptions = table.Column<int>(type: "integer", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DiscountType = table.Column<int>(type: "integer", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "numeric", nullable: false),
                    DurationMonths = table.Column<int>(type: "integer", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    MaxRedemptions = table.Column<int>(type: "integer", nullable: true),
                    MinTier = table.Column<int>(type: "integer", nullable: true),
                    StripePromotionId = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AiCreditsUsed = table.Column<int>(type: "integer", nullable: false),
                    AiLastAlertThreshold = table.Column<int>(type: "integer", nullable: false),
                    AiMonthlyBudget = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AllowedAiModels = table.Column<List<string>>(type: "jsonb", nullable: false),
                    AuditLogRetentionDays = table.Column<int>(type: "integer", nullable: false),
                    BillingInterval = table.Column<int>(type: "integer", nullable: false),
                    BookingsUsed = table.Column<int>(type: "integer", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentPeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentPeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    ExtraLocationCount = table.Column<int>(type: "integer", nullable: false),
                    ExtraStaffCount = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    LastAlertThreshold = table.Column<int>(type: "integer", nullable: false),
                    PausedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResumeAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SmsUsed = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StorageUsedBytes = table.Column<long>(type: "bigint", nullable: false),
                    StripeCustomerId = table.Column<string>(type: "text", nullable: true),
                    StripePaymentMethodId = table.Column<string>(type: "text", nullable: true),
                    StripeSubscriptionId = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantSubscriptions_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TenantSubscriptions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WebhookEndpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    Events = table.Column<string>(type: "text", nullable: false),
                    FailureCount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    LastSuccessAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastTriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Secret = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookEndpoints", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StaffMembers_TenantId",
                table: "StaffMembers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_TenantId",
                table: "Services",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId",
                table: "Payments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_TenantId",
                table: "Locations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantSubscriptions_PlanId",
                table: "TenantSubscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantSubscriptions_TenantId",
                table: "TenantSubscriptions",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_PromotionRedemptions_PromotionCodes_PromotionCodeId",
                table: "PromotionRedemptions",
                column: "PromotionCodeId",
                principalTable: "PromotionCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_CustomRole_CustomRoleId",
                table: "Users",
                column: "CustomRoleId",
                principalTable: "CustomRole",
                principalColumn: "Id");
        }
    }
}
