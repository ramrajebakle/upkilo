using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationSoundBadgePreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AIConversations_StaffMembers_AssignedStaffId",
                table: "AIConversations");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_StaffMembers_StaffId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_calendar_sync_tokens_StaffMembers_StaffId",
                table: "calendar_sync_tokens");

            migrationBuilder.DropForeignKey(
                name: "FK_CommissionRules_StaffMembers_StaffId",
                table: "CommissionRules");

            migrationBuilder.DropForeignKey(
                name: "FK_CommunicationLogs_Clients_ClientId",
                table: "CommunicationLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_CommunicationLogs_Users_StaffId",
                table: "CommunicationLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Consents_Users_UserId",
                table: "Consents");

            migrationBuilder.DropForeignKey(
                name: "FK_PromotionRedemptions_PromoCodes_PromoCodeId",
                table: "PromotionRedemptions");

            migrationBuilder.DropForeignKey(
                name: "FK_PromotionRedemptions_Tenants_TenantId",
                table: "PromotionRedemptions");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleBlocks_StaffMembers_StaffId",
                table: "ScheduleBlocks");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffClockIns_StaffMembers_StaffId",
                table: "StaffClockIns");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffCommissions_StaffMembers_StaffId",
                table: "StaffCommissions");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffMembers_Tenants_TenantId",
                table: "StaffMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffMembers_Users_UserId",
                table: "StaffMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffServices_StaffMembers_StaffId",
                table: "StaffServices");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffShifts_StaffMembers_StaffId",
                table: "StaffShifts");

            migrationBuilder.DropForeignKey(
                name: "FK_Tips_StaffMembers_StaffId",
                table: "Tips");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WebhookDeliveryLogs",
                table: "WebhookDeliveryLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StaffMembers",
                table: "StaffMembers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PromotionRedemptions",
                table: "PromotionRedemptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Consents",
                table: "Consents");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Bookings");

            migrationBuilder.RenameTable(
                name: "WebhookDeliveryLogs",
                newName: "WebhookDeliveryLog");

            migrationBuilder.RenameTable(
                name: "StaffMembers",
                newName: "StaffMember");

            migrationBuilder.RenameTable(
                name: "PromotionRedemptions",
                newName: "PromoRedemption");

            migrationBuilder.RenameTable(
                name: "Consents",
                newName: "UserConsent");

            migrationBuilder.RenameColumn(
                name: "SortOrder",
                table: "PipelineStages",
                newName: "OrderIndex");

            migrationBuilder.RenameColumn(
                name: "StaffId",
                table: "CommunicationLogs",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_CommunicationLogs_StaffId",
                table: "CommunicationLogs",
                newName: "IX_CommunicationLogs_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_StaffMembers_UserId",
                table: "StaffMember",
                newName: "IX_StaffMember_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_StaffMembers_TenantId_Email",
                table: "StaffMember",
                newName: "IX_StaffMember_TenantId_Email");

            migrationBuilder.RenameIndex(
                name: "IX_PromotionRedemptions_TenantId",
                table: "PromoRedemption",
                newName: "IX_PromoRedemption_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_PromotionRedemptions_PromoCodeId",
                table: "PromoRedemption",
                newName: "IX_PromoRedemption_PromoCodeId");

            migrationBuilder.RenameIndex(
                name: "IX_Consents_UserId",
                table: "UserConsent",
                newName: "IX_UserConsent_UserId");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "WebhookDeliveries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "WaitlistEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestedDate",
                table: "WaitlistEntries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "StaffId",
                table: "WaitlistEntries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "WaitlistEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "WaitlistEntries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "EnforceTwoFactorForClients",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnforceTwoFactorForStaff",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "StripeAiUsagePriceId",
                table: "SubscriptionPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentText",
                table: "SocialPosts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MediaUrlsJson",
                table: "SocialPosts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledFor",
                table: "SocialPosts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Timestamp",
                table: "SecurityEvents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "FilterJson",
                table: "SavedSearchFilters",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TargetEntity",
                table: "SavedSearchFilters",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRunAt",
                table: "ReportDefinitions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScheduledEmailRecipients",
                table: "ReportDefinitions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentUses",
                table: "PromoCodes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxUses",
                table: "PromoCodes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidFrom",
                table: "PromoCodes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidUntil",
                table: "PromoCodes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresShipping",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ProbabilityPercentage",
                table: "PipelineStages",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "Timestamp",
                table: "PageAnalyticsRecords",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "PlaySound",
                table: "NotificationPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowBadge",
                table: "NotificationPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SoundFileName",
                table: "NotificationPreferences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClientId",
                table: "Invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IssuedAt",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionId",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "InvoiceItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "Total",
                table: "InvoiceItems",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAlertSentAt",
                table: "InventoryItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRestockedAt",
                table: "InventoryItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LocationId",
                table: "InventoryItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LowStockThreshold",
                table: "InventoryItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "InventoryItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "InventoryItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ResponseDataJson",
                table: "FormSubmissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubmittedByClientId",
                table: "FormSubmissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OptionsJson",
                table: "FormFields",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrderIndex",
                table: "FormFields",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "FormDefinitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "FormDefinitions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "DataExports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "DataExports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "Fields",
                table: "DataExports",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileUrl",
                table: "DataExports",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FiltersJson",
                table: "DataExports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestedAt",
                table: "DataExports",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "RequestedById",
                table: "DataExports",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "TargetEntity",
                table: "DataExports",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Timestamp",
                table: "ConversionEvents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "Value",
                table: "ConversionEvents",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnalysisData",
                table: "ConversionAnalyses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApplied",
                table: "ConversionAnalyses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "ClientId",
                table: "CommunicationLogs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "CommunicationLogs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "CommunicationLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAt",
                table: "CommunicationLogs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceId",
                table: "CommunicationLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Clients",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Clients",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Clients",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MessageBody",
                table: "campaigns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SentCount",
                table: "campaigns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TargetSegment",
                table: "campaigns",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalAccountId",
                table: "calendar_sync_tokens",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "calendar_sync_tokens",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SyncDirection",
                table: "calendar_sync_tokens",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "PremiumScore",
                table: "BusinessListings",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<DateTime>(
                name: "GracePeriodExpiresAt",
                table: "ApiKeys",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PayoutsEnabled",
                table: "StaffMember",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StripeConnectId",
                table: "StaffMember",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripePayoutStatus",
                table: "StaffMember",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BookingId",
                table: "PromoRedemption",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClientId",
                table: "PromoRedemption",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConsentType",
                table: "UserConsent",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "GrantedAt",
                table: "UserConsent",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsGranted",
                table: "UserConsent",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_WebhookDeliveryLog",
                table: "WebhookDeliveryLog",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StaffMember",
                table: "StaffMember",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PromoRedemption",
                table: "PromoRedemption",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserConsent",
                table: "UserConsent",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "AdvancedFeatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EnableApiAccess = table.Column<bool>(type: "boolean", nullable: false),
                    EnableCustomWebhooks = table.Column<bool>(type: "boolean", nullable: false),
                    EnableWhiteLabel = table.Column<bool>(type: "boolean", nullable: false),
                    EnablePrioritySupport = table.Column<bool>(type: "boolean", nullable: false),
                    EnableCustomSmsSenderId = table.Column<bool>(type: "boolean", nullable: false),
                    EnableAdvancedReporting = table.Column<bool>(type: "boolean", nullable: false),
                    EnableIpAllowlisting = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvancedFeatures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIDiscoveryReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessType = table.Column<string>(type: "text", nullable: false),
                    Niche = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Keywords = table.Column<string>(type: "text", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUserReviewed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIDiscoveryReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIDiscoveryReports_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    OldValues = table.Column<string>(type: "text", nullable: true),
                    NewValues = table.Column<string>(type: "text", nullable: true),
                    Details = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClientReferrals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferrerClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferredClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RewardPoints = table.Column<int>(type: "integer", nullable: false),
                    RewardIssued = table.Column<bool>(type: "boolean", nullable: false),
                    ReferrerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferredId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientReferrals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientReferrals_Clients_ReferredId",
                        column: x => x.ReferredId,
                        principalTable: "Clients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClientReferrals_Clients_ReferrerId",
                        column: x => x.ReferrerId,
                        principalTable: "Clients",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CrmTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AssignedTo = table.Column<Guid>(type: "uuid", nullable: true),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RelatedId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedType = table.Column<string>(type: "text", nullable: true),
                    AssigneeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmTasks_StaffMember_AssigneeId",
                        column: x => x.AssigneeId,
                        principalTable: "StaffMember",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyRewards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LoyaltyProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    PointsCost = table.Column<int>(type: "integer", nullable: false),
                    RewardType = table.Column<string>(type: "text", nullable: false),
                    RewardValue = table.Column<decimal>(type: "numeric", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MaxRedemptions = table.Column<int>(type: "integer", nullable: true),
                    TimesRedeemed = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyRewards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    TransactionType = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoyaltyTransactions_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "marketing_auto_responders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TriggerEvent = table.Column<string>(type: "text", nullable: false),
                    EmailTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    Subject = table.Column<string>(type: "text", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    DelayMinutes = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_marketing_auto_responders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SamlConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    EntityId = table.Column<string>(type: "text", nullable: false),
                    IdpMetadataUrl = table.Column<string>(type: "text", nullable: false),
                    IdpCertificate = table.Column<string>(type: "text", nullable: true),
                    SignOnUrl = table.Column<string>(type: "text", nullable: true),
                    LogoutUrl = table.Column<string>(type: "text", nullable: true),
                    AttributeMapping = table.Column<string>(type: "text", nullable: false),
                    AllowPasswordLogin = table.Column<bool>(type: "boolean", nullable: false),
                    AutoCreateUsers = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultRoleId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SamlConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceBundleItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BundleServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComponentServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceBundleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceBundleItems_Services_BundleServiceId",
                        column: x => x.BundleServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceBundleItems_Services_ComponentServiceId",
                        column: x => x.ComponentServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceUpsells",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MainServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpsellServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Pitch = table.Column<string>(type: "text", nullable: true),
                    DiscountedPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceUpsells", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceUpsells_Services_MainServiceId",
                        column: x => x.MainServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceUpsells_Services_UpsellServiceId",
                        column: x => x.UpsellServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StaffCertifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IssuingOrganization = table.Column<string>(type: "text", nullable: true),
                    IssuingAuthority = table.Column<string>(type: "text", nullable: true),
                    CertificateNumber = table.Column<string>(type: "text", nullable: true),
                    IssueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerificationUrl = table.Column<string>(type: "text", nullable: true),
                    DocumentUrl = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffCertifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffCertifications_StaffMember_StaffId",
                        column: x => x.StaffId,
                        principalTable: "StaffMember",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StaffShiftSwaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestingStaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestingShiftId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetStaffId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetShiftId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    ActionedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AdminNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffShiftSwaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffShiftSwaps_StaffMember_RequestingStaffId",
                        column: x => x.RequestingStaffId,
                        principalTable: "StaffMember",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StaffShiftSwaps_StaffMember_TargetStaffId",
                        column: x => x.TargetStaffId,
                        principalTable: "StaffMember",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StaffShiftSwaps_StaffShifts_RequestingShiftId",
                        column: x => x.RequestingShiftId,
                        principalTable: "StaffShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StaffShiftSwaps_StaffShifts_TargetShiftId",
                        column: x => x.TargetShiftId,
                        principalTable: "StaffShifts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StaffTimesheets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClockInTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClockOutTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TotalHours = table.Column<decimal>(type: "numeric", nullable: true),
                    IsOvertime = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffTimesheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffTimesheets_StaffMember_StaffId",
                        column: x => x.StaffId,
                        principalTable: "StaffMember",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StripePayouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    StripeTransferId = table.Column<string>(type: "text", nullable: false),
                    StripePayoutId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ArrivalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureCode = table.Column<string>(type: "text", nullable: true),
                    FailureMessage = table.Column<string>(type: "text", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StripePayouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StripePayouts_StaffMember_StaffId",
                        column: x => x.StaffId,
                        principalTable: "StaffMember",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PromoRedemption_ClientId",
                table: "PromoRedemption",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_AIDiscoveryReports_TenantId",
                table: "AIDiscoveryReports",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientReferrals_ReferredId",
                table: "ClientReferrals",
                column: "ReferredId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientReferrals_ReferrerId",
                table: "ClientReferrals",
                column: "ReferrerId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmTasks_AssigneeId",
                table: "CrmTasks",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_ClientId",
                table: "LoyaltyTransactions",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceBundleItems_BundleServiceId",
                table: "ServiceBundleItems",
                column: "BundleServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceBundleItems_ComponentServiceId",
                table: "ServiceBundleItems",
                column: "ComponentServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceUpsells_MainServiceId",
                table: "ServiceUpsells",
                column: "MainServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceUpsells_UpsellServiceId",
                table: "ServiceUpsells",
                column: "UpsellServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffCertifications_StaffId",
                table: "StaffCertifications",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffShiftSwaps_RequestingShiftId",
                table: "StaffShiftSwaps",
                column: "RequestingShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffShiftSwaps_RequestingStaffId",
                table: "StaffShiftSwaps",
                column: "RequestingStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffShiftSwaps_TargetShiftId",
                table: "StaffShiftSwaps",
                column: "TargetShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffShiftSwaps_TargetStaffId",
                table: "StaffShiftSwaps",
                column: "TargetStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffTimesheets_StaffId",
                table: "StaffTimesheets",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_StripePayouts_StaffId",
                table: "StripePayouts",
                column: "StaffId");

            migrationBuilder.AddForeignKey(
                name: "FK_AIConversations_StaffMember_AssignedStaffId",
                table: "AIConversations",
                column: "AssignedStaffId",
                principalTable: "StaffMember",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_StaffMember_StaffId",
                table: "Bookings",
                column: "StaffId",
                principalTable: "StaffMember",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_calendar_sync_tokens_StaffMember_StaffId",
                table: "calendar_sync_tokens",
                column: "StaffId",
                principalTable: "StaffMember",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CommissionRules_StaffMember_StaffId",
                table: "CommissionRules",
                column: "StaffId",
                principalTable: "StaffMember",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CommunicationLogs_Clients_ClientId",
                table: "CommunicationLogs",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CommunicationLogs_Users_UserId",
                table: "CommunicationLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PromoRedemption_Clients_ClientId",
                table: "PromoRedemption",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PromoRedemption_PromoCodes_PromoCodeId",
                table: "PromoRedemption",
                column: "PromoCodeId",
                principalTable: "PromoCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PromoRedemption_Tenants_TenantId",
                table: "PromoRedemption",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleBlocks_StaffMember_StaffId",
                table: "ScheduleBlocks",
                column: "StaffId",
                principalTable: "StaffMember",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffClockIns_StaffMember_StaffId",
                table: "StaffClockIns",
                column: "StaffId",
                principalTable: "StaffMember",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffCommissions_StaffMember_StaffId",
                table: "StaffCommissions",
                column: "StaffId",
                principalTable: "StaffMember",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffMember_Tenants_TenantId",
                table: "StaffMember",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffMember_Users_UserId",
                table: "StaffMember",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StaffServices_StaffMember_StaffId",
                table: "StaffServices",
                column: "StaffId",
                principalTable: "StaffMember",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffShifts_StaffMember_StaffId",
                table: "StaffShifts",
                column: "StaffId",
                principalTable: "StaffMember",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tips_StaffMember_StaffId",
                table: "Tips",
                column: "StaffId",
                principalTable: "StaffMember",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserConsent_Users_UserId",
                table: "UserConsent",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AIConversations_StaffMember_AssignedStaffId",
                table: "AIConversations");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_StaffMember_StaffId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_calendar_sync_tokens_StaffMember_StaffId",
                table: "calendar_sync_tokens");

            migrationBuilder.DropForeignKey(
                name: "FK_CommissionRules_StaffMember_StaffId",
                table: "CommissionRules");

            migrationBuilder.DropForeignKey(
                name: "FK_CommunicationLogs_Clients_ClientId",
                table: "CommunicationLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_CommunicationLogs_Users_UserId",
                table: "CommunicationLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_PromoRedemption_Clients_ClientId",
                table: "PromoRedemption");

            migrationBuilder.DropForeignKey(
                name: "FK_PromoRedemption_PromoCodes_PromoCodeId",
                table: "PromoRedemption");

            migrationBuilder.DropForeignKey(
                name: "FK_PromoRedemption_Tenants_TenantId",
                table: "PromoRedemption");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleBlocks_StaffMember_StaffId",
                table: "ScheduleBlocks");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffClockIns_StaffMember_StaffId",
                table: "StaffClockIns");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffCommissions_StaffMember_StaffId",
                table: "StaffCommissions");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffMember_Tenants_TenantId",
                table: "StaffMember");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffMember_Users_UserId",
                table: "StaffMember");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffServices_StaffMember_StaffId",
                table: "StaffServices");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffShifts_StaffMember_StaffId",
                table: "StaffShifts");

            migrationBuilder.DropForeignKey(
                name: "FK_Tips_StaffMember_StaffId",
                table: "Tips");

            migrationBuilder.DropForeignKey(
                name: "FK_UserConsent_Users_UserId",
                table: "UserConsent");

            migrationBuilder.DropTable(
                name: "AdvancedFeatures");

            migrationBuilder.DropTable(
                name: "AIDiscoveryReports");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "ClientReferrals");

            migrationBuilder.DropTable(
                name: "CrmTasks");

            migrationBuilder.DropTable(
                name: "LoyaltyRewards");

            migrationBuilder.DropTable(
                name: "LoyaltyTransactions");

            migrationBuilder.DropTable(
                name: "marketing_auto_responders");

            migrationBuilder.DropTable(
                name: "SamlConfigurations");

            migrationBuilder.DropTable(
                name: "ServiceBundleItems");

            migrationBuilder.DropTable(
                name: "ServiceUpsells");

            migrationBuilder.DropTable(
                name: "StaffCertifications");

            migrationBuilder.DropTable(
                name: "StaffShiftSwaps");

            migrationBuilder.DropTable(
                name: "StaffTimesheets");

            migrationBuilder.DropTable(
                name: "StripePayouts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WebhookDeliveryLog",
                table: "WebhookDeliveryLog");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserConsent",
                table: "UserConsent");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StaffMember",
                table: "StaffMember");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PromoRedemption",
                table: "PromoRedemption");

            migrationBuilder.DropIndex(
                name: "IX_PromoRedemption_ClientId",
                table: "PromoRedemption");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WebhookDeliveries");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "WaitlistEntries");

            migrationBuilder.DropColumn(
                name: "RequestedDate",
                table: "WaitlistEntries");

            migrationBuilder.DropColumn(
                name: "StaffId",
                table: "WaitlistEntries");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "WaitlistEntries");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "WaitlistEntries");

            migrationBuilder.DropColumn(
                name: "EnforceTwoFactorForClients",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "EnforceTwoFactorForStaff",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "StripeAiUsagePriceId",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "ContentText",
                table: "SocialPosts");

            migrationBuilder.DropColumn(
                name: "MediaUrlsJson",
                table: "SocialPosts");

            migrationBuilder.DropColumn(
                name: "ScheduledFor",
                table: "SocialPosts");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "SecurityEvents");

            migrationBuilder.DropColumn(
                name: "FilterJson",
                table: "SavedSearchFilters");

            migrationBuilder.DropColumn(
                name: "TargetEntity",
                table: "SavedSearchFilters");

            migrationBuilder.DropColumn(
                name: "LastRunAt",
                table: "ReportDefinitions");

            migrationBuilder.DropColumn(
                name: "ScheduledEmailRecipients",
                table: "ReportDefinitions");

            migrationBuilder.DropColumn(
                name: "CurrentUses",
                table: "PromoCodes");

            migrationBuilder.DropColumn(
                name: "MaxUses",
                table: "PromoCodes");

            migrationBuilder.DropColumn(
                name: "ValidFrom",
                table: "PromoCodes");

            migrationBuilder.DropColumn(
                name: "ValidUntil",
                table: "PromoCodes");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RequiresShipping",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ProbabilityPercentage",
                table: "PipelineStages");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "PageAnalyticsRecords");

            migrationBuilder.DropColumn(
                name: "PlaySound",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "ShowBadge",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "SoundFileName",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "IssuedAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SubscriptionId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "Total",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "LastAlertSentAt",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "LastRestockedAt",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "LowStockThreshold",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "ResponseDataJson",
                table: "FormSubmissions");

            migrationBuilder.DropColumn(
                name: "SubmittedByClientId",
                table: "FormSubmissions");

            migrationBuilder.DropColumn(
                name: "OptionsJson",
                table: "FormFields");

            migrationBuilder.DropColumn(
                name: "OrderIndex",
                table: "FormFields");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "FormDefinitions");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "FormDefinitions");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "DataExports");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "DataExports");

            migrationBuilder.DropColumn(
                name: "Fields",
                table: "DataExports");

            migrationBuilder.DropColumn(
                name: "FileUrl",
                table: "DataExports");

            migrationBuilder.DropColumn(
                name: "FiltersJson",
                table: "DataExports");

            migrationBuilder.DropColumn(
                name: "RequestedAt",
                table: "DataExports");

            migrationBuilder.DropColumn(
                name: "RequestedById",
                table: "DataExports");

            migrationBuilder.DropColumn(
                name: "TargetEntity",
                table: "DataExports");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "ConversionEvents");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "ConversionEvents");

            migrationBuilder.DropColumn(
                name: "AnalysisData",
                table: "ConversionAnalyses");

            migrationBuilder.DropColumn(
                name: "IsApplied",
                table: "ConversionAnalyses");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "CommunicationLogs");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "CommunicationLogs");

            migrationBuilder.DropColumn(
                name: "ReadAt",
                table: "CommunicationLogs");

            migrationBuilder.DropColumn(
                name: "ReferenceId",
                table: "CommunicationLogs");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "MessageBody",
                table: "campaigns");

            migrationBuilder.DropColumn(
                name: "SentCount",
                table: "campaigns");

            migrationBuilder.DropColumn(
                name: "TargetSegment",
                table: "campaigns");

            migrationBuilder.DropColumn(
                name: "ExternalAccountId",
                table: "calendar_sync_tokens");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "calendar_sync_tokens");

            migrationBuilder.DropColumn(
                name: "SyncDirection",
                table: "calendar_sync_tokens");

            migrationBuilder.DropColumn(
                name: "PremiumScore",
                table: "BusinessListings");

            migrationBuilder.DropColumn(
                name: "GracePeriodExpiresAt",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "ConsentType",
                table: "UserConsent");

            migrationBuilder.DropColumn(
                name: "GrantedAt",
                table: "UserConsent");

            migrationBuilder.DropColumn(
                name: "IsGranted",
                table: "UserConsent");

            migrationBuilder.DropColumn(
                name: "PayoutsEnabled",
                table: "StaffMember");

            migrationBuilder.DropColumn(
                name: "StripeConnectId",
                table: "StaffMember");

            migrationBuilder.DropColumn(
                name: "StripePayoutStatus",
                table: "StaffMember");

            migrationBuilder.DropColumn(
                name: "BookingId",
                table: "PromoRedemption");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "PromoRedemption");

            migrationBuilder.RenameTable(
                name: "WebhookDeliveryLog",
                newName: "WebhookDeliveryLogs");

            migrationBuilder.RenameTable(
                name: "UserConsent",
                newName: "Consents");

            migrationBuilder.RenameTable(
                name: "StaffMember",
                newName: "StaffMembers");

            migrationBuilder.RenameTable(
                name: "PromoRedemption",
                newName: "PromotionRedemptions");

            migrationBuilder.RenameColumn(
                name: "OrderIndex",
                table: "PipelineStages",
                newName: "SortOrder");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "CommunicationLogs",
                newName: "StaffId");

            migrationBuilder.RenameIndex(
                name: "IX_CommunicationLogs_UserId",
                table: "CommunicationLogs",
                newName: "IX_CommunicationLogs_StaffId");

            migrationBuilder.RenameIndex(
                name: "IX_UserConsent_UserId",
                table: "Consents",
                newName: "IX_Consents_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_StaffMember_UserId",
                table: "StaffMembers",
                newName: "IX_StaffMembers_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_StaffMember_TenantId_Email",
                table: "StaffMembers",
                newName: "IX_StaffMembers_TenantId_Email");

            migrationBuilder.RenameIndex(
                name: "IX_PromoRedemption_TenantId",
                table: "PromotionRedemptions",
                newName: "IX_PromotionRedemptions_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_PromoRedemption_PromoCodeId",
                table: "PromotionRedemptions",
                newName: "IX_PromotionRedemptions_PromoCodeId");

            migrationBuilder.AlterColumn<Guid>(
                name: "ClientId",
                table: "CommunicationLogs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Bookings",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddPrimaryKey(
                name: "PK_WebhookDeliveryLogs",
                table: "WebhookDeliveryLogs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Consents",
                table: "Consents",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StaffMembers",
                table: "StaffMembers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PromotionRedemptions",
                table: "PromotionRedemptions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AIConversations_StaffMembers_AssignedStaffId",
                table: "AIConversations",
                column: "AssignedStaffId",
                principalTable: "StaffMembers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_StaffMembers_StaffId",
                table: "Bookings",
                column: "StaffId",
                principalTable: "StaffMembers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_calendar_sync_tokens_StaffMembers_StaffId",
                table: "calendar_sync_tokens",
                column: "StaffId",
                principalTable: "StaffMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CommissionRules_StaffMembers_StaffId",
                table: "CommissionRules",
                column: "StaffId",
                principalTable: "StaffMembers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CommunicationLogs_Clients_ClientId",
                table: "CommunicationLogs",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CommunicationLogs_Users_StaffId",
                table: "CommunicationLogs",
                column: "StaffId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Consents_Users_UserId",
                table: "Consents",
                column: "UserId",
                principalTable: "Users",
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
                name: "FK_PromotionRedemptions_Tenants_TenantId",
                table: "PromotionRedemptions",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleBlocks_StaffMembers_StaffId",
                table: "ScheduleBlocks",
                column: "StaffId",
                principalTable: "StaffMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffClockIns_StaffMembers_StaffId",
                table: "StaffClockIns",
                column: "StaffId",
                principalTable: "StaffMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffCommissions_StaffMembers_StaffId",
                table: "StaffCommissions",
                column: "StaffId",
                principalTable: "StaffMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffMembers_Tenants_TenantId",
                table: "StaffMembers",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffMembers_Users_UserId",
                table: "StaffMembers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StaffServices_StaffMembers_StaffId",
                table: "StaffServices",
                column: "StaffId",
                principalTable: "StaffMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffShifts_StaffMembers_StaffId",
                table: "StaffShifts",
                column: "StaffId",
                principalTable: "StaffMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tips_StaffMembers_StaffId",
                table: "Tips",
                column: "StaffId",
                principalTable: "StaffMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
