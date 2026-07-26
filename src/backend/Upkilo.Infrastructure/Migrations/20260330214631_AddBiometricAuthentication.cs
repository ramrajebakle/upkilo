using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBiometricAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "workflows",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "workflows",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "workflow_templates",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "workflow_templates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "workflow_executions",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "workflow_executions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_compensated",
                table: "workflow_executions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "retry_count",
                table: "workflow_executions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "workflow_execution_logs",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "workflow_execution_logs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "WhiteLabelConfigs",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "WhiteLabelConfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "WhatsAppTemplates",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "WhatsAppTemplates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "WhatsAppConfigs",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "WhatsAppConfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Webhooks",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Webhooks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "WebhookEvents",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "WebhookEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "WebhookDeliveryLog",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "WebhookDeliveryLog",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "WebhookDeliveries",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "WebhookDeliveries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "WaiverSignatures",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "WaiverSignatures",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "WaitlistConfigs",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "WaitlistConfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "VoiceCalls",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "VoiceCalls",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "VisualRegressionTests",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "VisualRegressionTests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "UserUiPreferences",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "UserUiPreferences",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Users",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "SocialProvider",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "UserConsent",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "UserConsent",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "UserActivityLogs",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "UserActivityLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TwoFaRecoveryRequests",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "TwoFaRecoveryRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TwoFactorConfigs",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "TwoFactorConfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TranslationEntries",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "TranslationEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Tips",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Tips",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TenantStatusHistories",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "TenantStatusHistories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Tenants",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TenantQuotas",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "TenantQuotas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TenantManagements",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "TenantManagements",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TenantIntegrations",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "TenantIntegrations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TaxRates",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "TaxRates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SupportTickets",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "SupportTickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SupportTicketComments",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "SupportTicketComments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Suppliers",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Suppliers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Subscriptions",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SubscriptionPlanVersions",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SubscriptionPlans",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "SubscriptionPlans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StripePayouts",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "StripePayouts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StaffWorkingHours",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "StaffWorkingHours",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StaffTips",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "StaffTips",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StaffTimesheets",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "StaffTimesheets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StaffShiftSwaps",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "StaffShiftSwaps",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StaffShifts",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "StaffShifts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StaffServices",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "StaffServices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRate",
                table: "StaffMember",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StaffMember",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "Timezone",
                table: "StaffMember",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "StaffMember",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StaffExceptions",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "StaffExceptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StaffCommissions",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "StaffCommissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StaffClockIns",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "StaffClockIns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StaffCertifications",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "StaffCertifications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SsoConfigs",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "SsoConfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SplitPayments",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "SplitPayments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SocialPosts",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "SocialPosts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Soc2Evidences",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Soc2Evidences",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SmsConsents",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "SmsConsents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SmsCampaignRegistrations",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "SmsCampaignRegistrations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SmsA2PBrands",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "SmsA2PBrands",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SlotHolds",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "SlotHolds",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ShippingProviders",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ShippingProviders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ServiceUpsells",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ServiceUpsells",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Services",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Services",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ServicePackages",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ServicePackages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ServiceBundleItems",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ServiceBundleItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SeoAnalyses",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "SeoAnalyses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SecurityEvents",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "SecurityEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SectionTemplates",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "SectionTemplates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SdkReleases",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ScheduleBlocks",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ScheduleBlocks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ScalingPolicies",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ScalingPolicies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SavedSearchFilters",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "SavedSearchFilters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SandboxEnvironments",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "SandboxEnvironments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SamlConfigurations",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "SamlConfigurations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SalesPipelines",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "SalesPipelines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ReviewRequests",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ReviewRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Resources",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Resources",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ReportDefinitions",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ReportDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "RegionConfigs",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "RegionConfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "RefreshTokens",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "RefreshTokens",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Referrals",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Referrals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ReferralRecords",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ReferralRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "RecurringPatterns",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "RecurringPatterns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "RecentSearches",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "RecentSearches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PushNotificationTokens",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "PushNotificationTokens",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PurchaseOrders",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "PurchaseOrders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PromoRedemption",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "PromoRedemption",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PromoCodes",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "PromoCodes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Products",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ProcessedWebhooks",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ProcessedWebhooks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PredictiveScores",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "PredictiveScores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PluginInstallations",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "PluginInstallations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PipelineStages",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "PipelineStages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Payments",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Payments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PaymentDisputes",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "PaymentDisputes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PartnerAccounts",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "PartnerAccounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "PageAnalyticsRecords",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "PageAnalyticsRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OutboxMessages",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "OutboxMessages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Orders",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OnboardingProgress",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "OnboardingProgress",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OfflineSyncQueues",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "OfflineSyncQueues",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OAuthTokens",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "OAuthTokens",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OAuthApps",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "OAuthApps",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "NotificationTemplates",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "NotificationTemplates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Notifications",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Notifications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "NotificationPreferences",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "NotificationPreferences",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "NotificationFallbackChannels",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "NotificationFallbackChannels",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "MlModelTrainings",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "MlModelTrainings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "MigrationRecords",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "MembershipPlans",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "MembershipPlans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "MembershipModules",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "MembershipModules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "MembershipLessons",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "MembershipLessons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "MembershipContents",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "MembershipContents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "MemberProgresses",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "MemberProgresses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "MarketingTemplates",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "MarketingTemplates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "MarketingForecasts",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "MarketingForecasts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "MarketingConfigs",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "MarketingConfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "MarketingAnalyticsRecords",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "MarketingAnalyticsRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "marketing_auto_responders",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "marketing_auto_responders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "LoyaltyTransactions",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "LoyaltyTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "LoyaltyRewards",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "LoyaltyRewards",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "LoyaltyPrograms",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "LoyaltyPrograms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "LoyaltyBalances",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "LoyaltyBalances",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "LoginHistories",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "LoginHistories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Locations",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Locations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "LegalDocuments",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "LegalAgreements",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "LeadCaptures",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "LeadCaptures",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "LandingPages",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "LandingPages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "JobQuotas",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "JobQuotas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Invoices",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Invoices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "InvoiceItems",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "InvoiceItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Invitations",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Invitations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "InventoryTransactions",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "InventoryTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "InventoryItems",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "InventoryItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "IndexingStatuses",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "IndexingStatuses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "IncidentRecords",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "IncidentRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Households",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Households",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "HipaaConfigs",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "HipaaConfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "HangfireWorkerNodes",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "HangfireWorkerNodes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "GroupBookings",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "GroupBookings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "GroupBookingParticipants",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "GroupBookingParticipants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "GiftCertificates",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "GiftCertificates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "GiftCertificateRedemptions",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "GiftCertificateRedemptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "GeneratedContents",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "GeneratedContents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "GdprConsents",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "GdprConsents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "GatedContents",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "GatedContents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "FunnelSteps",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "FunnelSteps",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "FormSubmissions",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "FormSubmissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "FormFields",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "FormFields",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "FormDefinitions",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "FormDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "BookingId",
                table: "ExternalReviews",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClientId",
                table: "ExternalReviews",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ExternalReviews",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ExternalReviews",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ErrorLogs",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ErrorLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "EmailMarketingSyncs",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "EmailMarketingSyncs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "DuplicateClientMatches",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "DuplicateClientMatches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "DunningCycles",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "DunningCycles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "DigitalWaivers",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "DeploymentRecords",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Deals",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Deals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "DealActivities",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "DealActivities",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "DeadLetterMessages",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "DeadLetterMessages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "DataWarehouseExports",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "DataWarehouseExports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "DataProcessingLogs",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "DataProcessingLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "DataImportJobs",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "DataImportJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "DataExports",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "DataExports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CustomRoles",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "CustomRoles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CustomFieldValues",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "CustomFieldValues",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CustomFieldDefinitions",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "CustomFieldDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CustomDomains",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "CustomDomains",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CurrencyConfigs",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "CurrencyConfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CrmTasks",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "CrmTasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CreditTransactions",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "CreditTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CreditAccountTransactions",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "CreditAccountTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CreditAccounts",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "CreditAccounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ConversionEvents",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ConversionEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ConversionAnalyses",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ConversionAnalyses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ContentCalendars",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ContentCalendars",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CommunicationLogs",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "CommunicationLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CommissionRules",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "CommissionRules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Clients",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Clients",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ClientReferrals",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ClientReferrals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ClientPortalConfigs",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ClientPortalConfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ClientPhotos",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ClientPhotos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ClientNotes",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ClientNotes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ClientMemberships",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ClientMemberships",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ClientContraindications",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ClientContraindications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ClientContentProgresses",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ClientContentProgresses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ClassDefinitions",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ClassDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ChatWidgets",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ChatWidgets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ChatVisitors",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ChatVisitors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CartItems",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "CartItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "campaigns",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "campaigns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "campaign_analytics",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "campaign_analytics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CalendarSyncs",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "CalendarSyncs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "calendar_sync_tokens",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "calendar_sync_tokens",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "BusinessListings",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "BusinessListings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Bookings",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "BookingReminders",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "BookingReminders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "BookingCheckIns",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "BookingCheckIns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AvailabilitySnapshots",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "AvailabilitySnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AuditLogs",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "AuditLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AuditEntries",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "AuditEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ApiKeys",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ApiKeys",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ApiErrorCodes",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "ApiErrorCodes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AIMessages",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "AIMessages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AIKnowledgeBases",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "AIKnowledgeBases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AIDiscoveryReports",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "AIDiscoveryReports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AIDecisionLogs",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "AIDecisionLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AIConversations",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "AIConversations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AIAgentConfigs",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "AIAgentConfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AgentActions",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "AgentActions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AffiliatePayouts",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "AffiliatePayouts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AffiliateCommissions",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "AffiliateCommissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AdvancedFeatures",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "AdvancedFeatures",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AdCampaigns",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "AdCampaigns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AdAccounts",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "AdAccounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AccountingSyncConfigs",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "AccountingSyncConfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AccessibilityScans",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "AccessibilityScans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AvailabilityCaches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    AvailableSlotsMask = table.Column<string>(type: "text", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_AvailabilityCaches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "marketing_funnels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TriggerType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TriggerConfig = table.Column<string>(type: "jsonb", nullable: true),
                    ConversionGoal = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TotalEntered = table.Column<int>(type: "integer", nullable: false),
                    TotalConverted = table.Column<int>(type: "integer", nullable: false),
                    ConversionRate = table.Column<decimal>(type: "numeric", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PausedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_marketing_funnels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "resource_bookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BookedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_resource_bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_resource_bookings_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPasskeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CredentialId = table.Column<byte[]>(type: "bytea", nullable: false),
                    PublicKey = table.Column<byte[]>(type: "bytea", nullable: false),
                    UserHandle = table.Column<byte[]>(type: "bytea", nullable: false),
                    SignatureCounter = table.Column<long>(type: "bigint", nullable: false),
                    CredentialType = table.Column<string>(type: "text", nullable: false),
                    Aaguid = table.Column<string>(type: "text", nullable: false),
                    RegDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RegOrigin = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPasskeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebPushSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Endpoint = table.Column<string>(type: "text", nullable: false),
                    P256dh = table.Column<string>(type: "text", nullable: false),
                    Auth = table.Column<string>(type: "text", nullable: false),
                    Tag = table.Column<string>(type: "text", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_WebPushSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebPushSubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Status",
                table: "Invoices",
                column: "Status",
                filter: "\"Status\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Status",
                table: "Bookings",
                column: "Status",
                filter: "\"Status\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_resource_bookings_ResourceId",
                table: "resource_bookings",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_WebPushSubscriptions_UserId",
                table: "WebPushSubscriptions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvailabilityCaches");

            migrationBuilder.DropTable(
                name: "marketing_funnels");

            migrationBuilder.DropTable(
                name: "resource_bookings");

            migrationBuilder.DropTable(
                name: "UserPasskeys");

            migrationBuilder.DropTable(
                name: "WebPushSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_Status",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_Status",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "workflows");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "workflows");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "workflow_templates");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "workflow_templates");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "workflow_executions");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "workflow_executions");

            migrationBuilder.DropColumn(
                name: "is_compensated",
                table: "workflow_executions");

            migrationBuilder.DropColumn(
                name: "retry_count",
                table: "workflow_executions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "workflow_execution_logs");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "workflow_execution_logs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "WhiteLabelConfigs");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "WhiteLabelConfigs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "WhatsAppTemplates");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "WhatsAppTemplates");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "WhatsAppConfigs");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "WhatsAppConfigs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Webhooks");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Webhooks");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "WebhookEvents");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "WebhookEvents");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "WebhookDeliveryLog");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "WebhookDeliveryLog");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "WebhookDeliveries");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "WebhookDeliveries");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "WaiverSignatures");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "WaiverSignatures");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "WaitlistConfigs");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "WaitlistConfigs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "VoiceCalls");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "VoiceCalls");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "VisualRegressionTests");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "VisualRegressionTests");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "UserUiPreferences");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "UserUiPreferences");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SocialProvider",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "UserConsent");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "UserConsent");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "UserActivityLogs");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "UserActivityLogs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TwoFaRecoveryRequests");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "TwoFaRecoveryRequests");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TwoFactorConfigs");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "TwoFactorConfigs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TranslationEntries");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "TranslationEntries");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Tips");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Tips");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TenantStatusHistories");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "TenantStatusHistories");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TenantQuotas");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "TenantQuotas");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TenantManagements");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "TenantManagements");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TenantIntegrations");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "TenantIntegrations");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TaxRates");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "TaxRates");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SupportTicketComments");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SupportTicketComments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SubscriptionPlanVersions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "StripePayouts");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "StripePayouts");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "StaffWorkingHours");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "StaffWorkingHours");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "StaffTips");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "StaffTips");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "StaffTimesheets");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "StaffTimesheets");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "StaffShiftSwaps");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "StaffShiftSwaps");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "StaffShifts");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "StaffShifts");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "StaffServices");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "StaffServices");

            migrationBuilder.DropColumn(
                name: "HourlyRate",
                table: "StaffMember");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "StaffMember");

            migrationBuilder.DropColumn(
                name: "Timezone",
                table: "StaffMember");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "StaffMember");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "StaffExceptions");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "StaffExceptions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "StaffCommissions");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "StaffCommissions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "StaffClockIns");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "StaffClockIns");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "StaffCertifications");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "StaffCertifications");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SsoConfigs");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SsoConfigs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SplitPayments");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SplitPayments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SocialPosts");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SocialPosts");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Soc2Evidences");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Soc2Evidences");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SmsConsents");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SmsConsents");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SmsCampaignRegistrations");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SmsCampaignRegistrations");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SmsA2PBrands");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SmsA2PBrands");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SlotHolds");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SlotHolds");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ShippingProviders");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ShippingProviders");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ServiceUpsells");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ServiceUpsells");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ServicePackages");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ServicePackages");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ServiceBundleItems");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ServiceBundleItems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SeoAnalyses");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SeoAnalyses");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SecurityEvents");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SecurityEvents");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SectionTemplates");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SectionTemplates");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SdkReleases");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ScheduleBlocks");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ScheduleBlocks");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ScalingPolicies");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ScalingPolicies");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SavedSearchFilters");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SavedSearchFilters");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SandboxEnvironments");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SandboxEnvironments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SamlConfigurations");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SamlConfigurations");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SalesPipelines");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SalesPipelines");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ReviewRequests");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ReviewRequests");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ReportDefinitions");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ReportDefinitions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "RegionConfigs");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "RegionConfigs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Referrals");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Referrals");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ReferralRecords");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ReferralRecords");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "RecurringPatterns");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "RecurringPatterns");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "RecentSearches");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "RecentSearches");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PushNotificationTokens");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "PushNotificationTokens");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PromoRedemption");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "PromoRedemption");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PromoCodes");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "PromoCodes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ProcessedWebhooks");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ProcessedWebhooks");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PredictiveScores");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "PredictiveScores");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PluginInstallations");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "PluginInstallations");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PipelineStages");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "PipelineStages");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PaymentDisputes");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "PaymentDisputes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PartnerAccounts");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "PartnerAccounts");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "PageAnalyticsRecords");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "PageAnalyticsRecords");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OnboardingProgress");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "OnboardingProgress");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OfflineSyncQueues");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "OfflineSyncQueues");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OAuthTokens");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "OAuthTokens");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OAuthApps");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "OAuthApps");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "NotificationTemplates");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "NotificationTemplates");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "NotificationFallbackChannels");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "NotificationFallbackChannels");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "MlModelTrainings");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "MlModelTrainings");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "MigrationRecords");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "MembershipPlans");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "MembershipPlans");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "MembershipModules");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "MembershipModules");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "MembershipLessons");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "MembershipLessons");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "MembershipContents");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "MembershipContents");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "MemberProgresses");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "MemberProgresses");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "MarketingTemplates");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "MarketingTemplates");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "MarketingForecasts");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "MarketingForecasts");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "MarketingConfigs");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "MarketingConfigs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "MarketingAnalyticsRecords");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "MarketingAnalyticsRecords");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "marketing_auto_responders");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "marketing_auto_responders");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "LoyaltyTransactions");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "LoyaltyTransactions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "LoyaltyRewards");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "LoyaltyRewards");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "LoyaltyPrograms");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "LoyaltyPrograms");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "LoyaltyBalances");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "LoyaltyBalances");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "LoginHistories");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "LoginHistories");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "LegalDocuments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "LegalAgreements");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "LeadCaptures");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "LeadCaptures");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "LandingPages");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "LandingPages");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "JobQuotas");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "JobQuotas");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Invitations");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Invitations");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "IndexingStatuses");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "IndexingStatuses");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "IncidentRecords");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "IncidentRecords");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Households");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Households");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "HipaaConfigs");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "HipaaConfigs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "HangfireWorkerNodes");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "HangfireWorkerNodes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "GroupBookings");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "GroupBookings");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "GroupBookingParticipants");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "GroupBookingParticipants");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "GiftCertificates");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "GiftCertificates");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "GiftCertificateRedemptions");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "GiftCertificateRedemptions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "GeneratedContents");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "GeneratedContents");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "GdprConsents");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "GdprConsents");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "GatedContents");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "GatedContents");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "FunnelSteps");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "FunnelSteps");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "FormSubmissions");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "FormSubmissions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "FormFields");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "FormFields");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "FormDefinitions");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "FormDefinitions");

            migrationBuilder.DropColumn(
                name: "BookingId",
                table: "ExternalReviews");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "ExternalReviews");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ExternalReviews");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ExternalReviews");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ErrorLogs");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ErrorLogs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "EmailMarketingSyncs");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "EmailMarketingSyncs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "DuplicateClientMatches");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "DuplicateClientMatches");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "DunningCycles");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "DunningCycles");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "DigitalWaivers");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "DeploymentRecords");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "DealActivities");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "DealActivities");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "DeadLetterMessages");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "DeadLetterMessages");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "DataWarehouseExports");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "DataWarehouseExports");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "DataProcessingLogs");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "DataProcessingLogs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "DataImportJobs");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "DataImportJobs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "DataExports");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "DataExports");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CustomRoles");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "CustomRoles");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CustomFieldValues");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "CustomFieldValues");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CustomFieldDefinitions");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "CustomFieldDefinitions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CustomDomains");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "CustomDomains");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CurrencyConfigs");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "CurrencyConfigs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CrmTasks");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "CrmTasks");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CreditTransactions");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "CreditTransactions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CreditAccountTransactions");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "CreditAccountTransactions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CreditAccounts");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "CreditAccounts");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ConversionEvents");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ConversionEvents");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ConversionAnalyses");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ConversionAnalyses");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ContentCalendars");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ContentCalendars");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CommunicationLogs");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "CommunicationLogs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CommissionRules");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "CommissionRules");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ClientReferrals");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ClientReferrals");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ClientPortalConfigs");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ClientPortalConfigs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ClientPhotos");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ClientPhotos");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ClientNotes");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ClientNotes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ClientMemberships");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ClientMemberships");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ClientContraindications");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ClientContraindications");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ClientContentProgresses");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ClientContentProgresses");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ClassDefinitions");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ClassDefinitions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ChatWidgets");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ChatWidgets");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ChatVisitors");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ChatVisitors");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "campaigns");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "campaigns");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "campaign_analytics");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "campaign_analytics");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CalendarSyncs");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "CalendarSyncs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "calendar_sync_tokens");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "calendar_sync_tokens");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "BusinessListings");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "BusinessListings");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "BookingReminders");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "BookingReminders");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "BookingCheckIns");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "BookingCheckIns");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AvailabilitySnapshots");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AvailabilitySnapshots");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AuditEntries");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AuditEntries");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ApiErrorCodes");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "ApiErrorCodes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AIMessages");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AIMessages");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AIKnowledgeBases");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AIKnowledgeBases");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AIDiscoveryReports");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AIDiscoveryReports");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AIDecisionLogs");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AIDecisionLogs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AIConversations");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AIConversations");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AIAgentConfigs");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AIAgentConfigs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AgentActions");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AgentActions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AffiliatePayouts");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AffiliatePayouts");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AffiliateCommissions");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AffiliateCommissions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AdvancedFeatures");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AdvancedFeatures");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AdCampaigns");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AdCampaigns");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AdAccounts");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AdAccounts");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AccountingSyncConfigs");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AccountingSyncConfigs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AccessibilityScans");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AccessibilityScans");
        }
    }
}
