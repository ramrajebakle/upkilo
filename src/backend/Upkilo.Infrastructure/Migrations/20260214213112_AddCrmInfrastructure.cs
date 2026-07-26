using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Upkilo.Core.Entities;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCrmInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "workflows",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "workflows",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "workflows",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "workflow_templates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "workflow_templates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "workflow_templates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Webhooks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Webhooks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Webhooks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "WebhookDeliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "WebhookDeliveries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "WebhookDeliveries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomRoleId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "StaffWorkingHours",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "StaffWorkingHours",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "StaffWorkingHours",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "StaffServices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "StaffServices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "StaffServices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseCommissionRate",
                table: "StaffMembers",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CommissionType",
                table: "StaffMembers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateJoined",
                table: "StaffMembers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "StaffMembers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "StaffMembers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmploymentType",
                table: "StaffMembers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "StaffMembers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "Tags",
                table: "StaffMembers",
                type: "jsonb",
                nullable: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "StaffExceptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "StaffExceptions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "StaffExceptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "SlotHolds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "SlotHolds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SlotHolds",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Services",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Services",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Services",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "RecurringPatterns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "RecurringPatterns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "RecurringPatterns",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Payments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "MembershipPlans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "MembershipPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "MembershipPlans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Locations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Locations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Locations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "Invoices",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Industry",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Invoices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Dictionary<string, object>>(
                name: "Metadata",
                table: "Invoices",
                type: "jsonb",
                nullable: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "InvoiceItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "InvoiceItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "InvoiceItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Invitations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Invitations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Invitations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "GiftCertificates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "GiftCertificates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "GiftCertificates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "GiftCertificateRedemptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "GiftCertificateRedemptions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "GiftCertificateRedemptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "CustomFieldDefinitions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "CustomFieldDefinitions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CustomFieldDefinitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Clients",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Clients",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HouseholdId",
                table: "Clients",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Clients",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LoyaltyPoints",
                table: "Clients",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LoyaltyTier",
                table: "Clients",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ClientMemberships",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ClientMemberships",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ClientMemberships",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "campaigns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "campaigns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "campaigns",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "campaign_analytics",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "campaign_analytics",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "campaign_analytics",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "calendar_sync_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "calendar_sync_tokens",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "calendar_sync_tokens",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckedInAt",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Bookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsWalkIn",
                table: "Bookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ClientContraindications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AddedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientContraindications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientContraindications_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    IsPrivate = table.Column<bool>(type: "boolean", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientNotes_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientNotes_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientPhotos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    FileUrl = table.Column<string>(type: "text", nullable: false),
                    Caption = table.Column<string>(type: "text", nullable: true),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    MimeType = table.Column<string>(type: "text", nullable: true),
                    TakenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientPhotos_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClientPhotos_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientPhotos_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CommunicationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: true),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExternalReference = table.Column<string>(type: "text", nullable: true),
                    Metadata = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunicationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunicationLogs_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommunicationLogs_Users_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CreditTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "numeric", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ReferenceId = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditTransactions_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomDomains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Hostname = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    VerificationToken = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SslStatus = table.Column<int>(type: "integer", nullable: false),
                    LastVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomDomains", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomDomains_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomRole",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Permissions = table.Column<Dictionary<string, bool>>(type: "jsonb", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_CustomRole", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailVerificationTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailVerificationTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailVerificationTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Households",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PrimaryClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingAddress = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    SharedBilling = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_Households", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Households_Clients_PrimaryClientId",
                        column: x => x.PrimaryClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoginAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: false),
                    AttemptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Succeeded = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PasswordHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedWebhooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<string>(type: "text", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedWebhooks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromotionCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DiscountType = table.Column<int>(type: "integer", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxRedemptions = table.Column<int>(type: "integer", nullable: true),
                    CurrentRedemptions = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MinTier = table.Column<int>(type: "integer", nullable: true),
                    DurationMonths = table.Column<int>(type: "integer", nullable: true),
                    StripePromotionId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StaffCommissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    BaseAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CommissionRate = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    TotalEarned = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffCommissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffCommissions_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StaffCommissions_StaffMembers_StaffId",
                        column: x => x.StaffId,
                        principalTable: "StaffMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StaffShifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffShifts_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StaffShifts_StaffMembers_StaffId",
                        column: x => x.StaffId,
                        principalTable: "StaffMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    AnnualPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    StripePriceIdMonthly = table.Column<string>(type: "text", nullable: false),
                    StripePriceIdAnnual = table.Column<string>(type: "text", nullable: false),
                    TrialDays = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Features = table.Column<PlanFeatures>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxRates_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserActivityLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityType = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    ResourceType = table.Column<string>(type: "text", nullable: true),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserActivityLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromotionRedemptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PromotionCodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RedeemedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DiscountApplied = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionRedemptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromotionRedemptions_PromotionCodes_PromotionCodeId",
                        column: x => x.PromotionCodeId,
                        principalTable: "PromotionCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PromotionRedemptions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StaffClockIns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClockInTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClockOutTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    LatLong = table.Column<string>(type: "text", nullable: true),
                    Device = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffClockIns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffClockIns_StaffMembers_StaffId",
                        column: x => x.StaffId,
                        principalTable: "StaffMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StaffClockIns_StaffShifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "StaffShifts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TenantSubscriptions",
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

            migrationBuilder.CreateIndex(
                name: "IX_Users_CustomRoleId",
                table: "Users",
                column: "CustomRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_HouseholdId",
                table: "Clients",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientContraindications_ClientId",
                table: "ClientContraindications",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientNotes_AuthorId",
                table: "ClientNotes",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientNotes_ClientId",
                table: "ClientNotes",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientPhotos_BookingId",
                table: "ClientPhotos",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientPhotos_ClientId",
                table: "ClientPhotos",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientPhotos_ServiceId",
                table: "ClientPhotos",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationLogs_ClientId",
                table: "CommunicationLogs",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunicationLogs_StaffId",
                table: "CommunicationLogs",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_ClientId",
                table: "CreditTransactions",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomDomains_Hostname",
                table: "CustomDomains",
                column: "Hostname",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomDomains_TenantId",
                table: "CustomDomains",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationTokens_UserId",
                table: "EmailVerificationTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Households_PrimaryClientId",
                table: "Households",
                column: "PrimaryClientId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordHistories_UserId",
                table: "PasswordHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserId",
                table: "PasswordResetTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedWebhooks_EventId",
                table: "ProcessedWebhooks",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRedemptions_PromotionCodeId",
                table: "PromotionRedemptions",
                column: "PromotionCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRedemptions_TenantId",
                table: "PromotionRedemptions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffClockIns_ShiftId",
                table: "StaffClockIns",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffClockIns_StaffId",
                table: "StaffClockIns",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffCommissions_BookingId",
                table: "StaffCommissions",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffCommissions_StaffId",
                table: "StaffCommissions",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffShifts_LocationId",
                table: "StaffShifts",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffShifts_StaffId",
                table: "StaffShifts",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxRates_TenantId",
                table: "TaxRates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantSubscriptions_PlanId",
                table: "TenantSubscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantSubscriptions_TenantId",
                table: "TenantSubscriptions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityLogs_UserId",
                table: "UserActivityLogs",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_Households_HouseholdId",
                table: "Clients",
                column: "HouseholdId",
                principalTable: "Households",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_CustomRole_CustomRoleId",
                table: "Users",
                column: "CustomRoleId",
                principalTable: "CustomRole",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clients_Households_HouseholdId",
                table: "Clients");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_CustomRole_CustomRoleId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "ClientContraindications");

            migrationBuilder.DropTable(
                name: "ClientNotes");

            migrationBuilder.DropTable(
                name: "ClientPhotos");

            migrationBuilder.DropTable(
                name: "CommunicationLogs");

            migrationBuilder.DropTable(
                name: "CreditTransactions");

            migrationBuilder.DropTable(
                name: "CustomDomains");

            migrationBuilder.DropTable(
                name: "CustomRole");

            migrationBuilder.DropTable(
                name: "EmailVerificationTokens");

            migrationBuilder.DropTable(
                name: "Households");

            migrationBuilder.DropTable(
                name: "LoginAttempts");

            migrationBuilder.DropTable(
                name: "PasswordHistories");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropTable(
                name: "ProcessedWebhooks");

            migrationBuilder.DropTable(
                name: "PromotionRedemptions");

            migrationBuilder.DropTable(
                name: "StaffClockIns");

            migrationBuilder.DropTable(
                name: "StaffCommissions");

            migrationBuilder.DropTable(
                name: "TaxRates");

            migrationBuilder.DropTable(
                name: "TenantSubscriptions");

            migrationBuilder.DropTable(
                name: "UserActivityLogs");

            migrationBuilder.DropTable(
                name: "PromotionCodes");

            migrationBuilder.DropTable(
                name: "StaffShifts");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropIndex(
                name: "IX_Users_CustomRoleId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Clients_HouseholdId",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "workflows");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "workflows");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "workflows");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "workflow_templates");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "workflow_templates");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "workflow_templates");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Webhooks");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Webhooks");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Webhooks");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "WebhookDeliveries");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "WebhookDeliveries");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "WebhookDeliveries");

            migrationBuilder.DropColumn(
                name: "CustomRoleId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "StaffWorkingHours");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "StaffWorkingHours");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "StaffWorkingHours");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "StaffServices");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "StaffServices");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "StaffServices");

            migrationBuilder.DropColumn(
                name: "BaseCommissionRate",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "CommissionType",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "DateJoined",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "EmploymentType",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "StaffExceptions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "StaffExceptions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "StaffExceptions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SlotHolds");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "SlotHolds");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SlotHolds");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "RecurringPatterns");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "RecurringPatterns");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "RecurringPatterns");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "MembershipPlans");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "MembershipPlans");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "MembershipPlans");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Industry",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Invitations");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Invitations");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Invitations");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "GiftCertificates");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "GiftCertificates");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "GiftCertificates");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "GiftCertificateRedemptions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "GiftCertificateRedemptions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "GiftCertificateRedemptions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CustomFieldDefinitions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "CustomFieldDefinitions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CustomFieldDefinitions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "HouseholdId",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "LoyaltyPoints",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "LoyaltyTier",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ClientMemberships");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ClientMemberships");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ClientMemberships");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "campaigns");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "campaigns");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "campaigns");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "campaign_analytics");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "campaign_analytics");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "campaign_analytics");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "calendar_sync_tokens");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "calendar_sync_tokens");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "calendar_sync_tokens");

            migrationBuilder.DropColumn(
                name: "CheckedInAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IsWalkIn",
                table: "Bookings");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "Invoices",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldPrecision: 10,
                oldScale: 2);
        }
    }
}
