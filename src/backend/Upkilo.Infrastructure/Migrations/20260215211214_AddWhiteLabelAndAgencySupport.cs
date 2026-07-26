using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWhiteLabelAndAgencySupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_StaffId",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "CustomFieldDefinitions",
                newName: "Label");

            migrationBuilder.RenameColumn(
                name: "Key",
                table: "CustomFieldDefinitions",
                newName: "EntityType");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:hstore", ",,");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentTenantId",
                table: "Tenants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TipAmount",
                table: "StaffCommissions",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TipAmount",
                table: "Payments",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "Options",
                table: "CustomFieldDefinitions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultValue",
                table: "CustomFieldDefinitions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FieldType",
                table: "CustomFieldDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsSearchable",
                table: "CustomFieldDefinitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ValidationRules",
                table: "CustomFieldDefinitions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GroupSize",
                table: "Bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PerParticipantPrice",
                table: "Bookings",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Bookings",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "AccessibilityScans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PageUrl = table.Column<string>(type: "text", nullable: false),
                    ScanEngine = table.Column<string>(type: "text", nullable: false),
                    ViolationCount = table.Column<int>(type: "integer", nullable: false),
                    PassCount = table.Column<int>(type: "integer", nullable: false),
                    IncompleteCount = table.Column<int>(type: "integer", nullable: false),
                    Violations = table.Column<string>(type: "text", nullable: true),
                    WcagLevel = table.Column<string>(type: "text", nullable: false),
                    ComplianceScore = table.Column<decimal>(type: "numeric", nullable: false),
                    ScannedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessibilityScans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountingSyncConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: true),
                    RefreshToken = table.Column<string>(type: "text", nullable: true),
                    CompanyId = table.Column<string>(type: "text", nullable: true),
                    SyncInvoices = table.Column<bool>(type: "boolean", nullable: false),
                    SyncPayments = table.Column<bool>(type: "boolean", nullable: false),
                    SyncContacts = table.Column<bool>(type: "boolean", nullable: false),
                    LastSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSyncError = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingSyncConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Platform = table.Column<string>(type: "text", nullable: false),
                    ExternalAccountId = table.Column<string>(type: "text", nullable: false),
                    AccountName = table.Column<string>(type: "text", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: true),
                    RefreshToken = table.Column<string>(type: "text", nullable: true),
                    TokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsConnected = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdminImpersonationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActionsPerformed = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminImpersonationLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentName = table.Column<string>(type: "text", nullable: false),
                    ActionType = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    RiskLevel = table.Column<string>(type: "text", nullable: false),
                    RequiresReview = table.Column<bool>(type: "boolean", nullable: false),
                    WasAutoApplied = table.Column<bool>(type: "boolean", nullable: false),
                    WasRolledBack = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_AgentActions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIAgentConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentName = table.Column<string>(type: "text", nullable: false),
                    SystemPrompt = table.Column<string>(type: "text", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    HandoffConfidenceThreshold = table.Column<decimal>(type: "numeric", nullable: false),
                    AutoBookEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AllowedChannels = table.Column<string>(type: "text", nullable: false),
                    HandoffTriggers = table.Column<Dictionary<string, string>>(type: "hstore", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIAgentConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "text", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LastMessage = table.Column<string>(type: "text", nullable: true),
                    LastActivityAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    Intent = table.Column<string>(type: "text", nullable: true),
                    HumanInteractionRequired = table.Column<bool>(type: "boolean", nullable: false),
                    AssignedStaffId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIConversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIConversations_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AIConversations_StaffMembers_AssignedStaffId",
                        column: x => x.AssignedStaffId,
                        principalTable: "StaffMembers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AIDecisionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentName = table.Column<string>(type: "text", nullable: false),
                    DecisionType = table.Column<string>(type: "text", nullable: false),
                    InputData = table.Column<string>(type: "text", nullable: false),
                    OutputDecision = table.Column<string>(type: "text", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "numeric", nullable: false),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "text", nullable: true),
                    RequiresHumanReview = table.Column<bool>(type: "boolean", nullable: false),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Feedback = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIDecisionLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIKnowledgeBases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Question = table.Column<string>(type: "text", nullable: false),
                    Answer = table.Column<string>(type: "text", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    VectorEmbedding = table.Column<float[]>(type: "real[]", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIKnowledgeBases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiErrorCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ErrorCode = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    HttpStatusCode = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    DetailTemplate = table.Column<string>(type: "text", nullable: true),
                    Resolution = table.Column<string>(type: "text", nullable: true),
                    IsDeprecated = table.Column<bool>(type: "boolean", nullable: false),
                    DeprecationNotice = table.Column<string>(type: "text", nullable: true),
                    ApiVersion = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiErrorCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Prefix = table.Column<string>(type: "text", nullable: false),
                    KeyHash = table.Column<string>(type: "text", nullable: false),
                    LastFourChars = table.Column<string>(type: "text", nullable: false),
                    Scopes = table.Column<List<string>>(type: "text[]", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    PerformedById = table.Column<Guid>(type: "uuid", nullable: true),
                    PerformedByName = table.Column<string>(type: "text", nullable: true),
                    OldValues = table.Column<string>(type: "text", nullable: true),
                    NewValues = table.Column<string>(type: "text", nullable: true),
                    ChangedFields = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    PerformedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BookingCheckIns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    QrCodeData = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CheckedInAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CheckInMethod = table.Column<string>(type: "text", nullable: true),
                    EstimatedWaitMinutes = table.Column<int>(type: "integer", nullable: true),
                    StaffNotified = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingCheckIns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessListings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessName = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    State = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: true),
                    ZipCode = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Website = table.Column<string>(type: "text", nullable: true),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    AverageRating = table.Column<double>(type: "double precision", nullable: false),
                    ReviewCount = table.Column<int>(type: "integer", nullable: false),
                    ServiceTags = table.Column<string>(type: "text", nullable: true),
                    OperatingHours = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_BusinessListings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CalendarSyncs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: true),
                    RefreshToken = table.Column<string>(type: "text", nullable: true),
                    CalendarId = table.Column<string>(type: "text", nullable: true),
                    SyncToExternal = table.Column<bool>(type: "boolean", nullable: false),
                    SyncFromExternal = table.Column<bool>(type: "boolean", nullable: false),
                    LastSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SyncErrors = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarSyncs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatVisitors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitorName = table.Column<string>(type: "text", nullable: true),
                    VisitorEmail = table.Column<string>(type: "text", nullable: true),
                    SessionId = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    CurrentPage = table.Column<string>(type: "text", nullable: true),
                    Referrer = table.Column<string>(type: "text", nullable: true),
                    PageViews = table.Column<int>(type: "integer", nullable: false),
                    FirstSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastActiveAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatVisitors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatWidgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    WelcomeMessage = table.Column<string>(type: "text", nullable: true),
                    OfflineMessage = table.Column<string>(type: "text", nullable: true),
                    PreChatFormFields = table.Column<string>(type: "text", nullable: true),
                    CannedResponses = table.Column<string>(type: "text", nullable: true),
                    Appearance = table.Column<string>(type: "text", nullable: true),
                    EnableAiHandoff = table.Column<bool>(type: "boolean", nullable: false),
                    BusinessHours = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatWidgets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClassDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    MaxCapacity = table.Column<int>(type: "integer", nullable: false),
                    CurrentEnrolled = table.Column<int>(type: "integer", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    InstructorId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecurrenceRule = table.Column<string>(type: "text", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: true),
                    AllowWaitlist = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_ClassDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClientPortalConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AllowBookingHistory = table.Column<bool>(type: "boolean", nullable: false),
                    AllowInvoiceDownload = table.Column<bool>(type: "boolean", nullable: false),
                    AllowProfileUpdate = table.Column<bool>(type: "boolean", nullable: false),
                    AllowMessaging = table.Column<bool>(type: "boolean", nullable: false),
                    AllowDocumentAccess = table.Column<bool>(type: "boolean", nullable: false),
                    AllowForms = table.Column<bool>(type: "boolean", nullable: false),
                    CustomBranding = table.Column<string>(type: "text", nullable: true),
                    WelcomeMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientPortalConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConversionAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PageUrl = table.Column<string>(type: "text", nullable: false),
                    PageViews = table.Column<int>(type: "integer", nullable: false),
                    UniqueVisitors = table.Column<int>(type: "integer", nullable: false),
                    BounceRate = table.Column<decimal>(type: "numeric", nullable: false),
                    ConversionRate = table.Column<decimal>(type: "numeric", nullable: false),
                    DropOffPoints = table.Column<string>(type: "text", nullable: true),
                    CtaSuggestions = table.Column<string>(type: "text", nullable: true),
                    HeadlineSuggestions = table.Column<string>(type: "text", nullable: true),
                    AbVariants = table.Column<string>(type: "text", nullable: true),
                    AnalyzedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversionAnalyses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConversionEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventName = table.Column<string>(type: "text", nullable: false),
                    EventCategory = table.Column<string>(type: "text", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    Source = table.Column<string>(type: "text", nullable: true),
                    Medium = table.Column<string>(type: "text", nullable: true),
                    CampaignTag = table.Column<string>(type: "text", nullable: true),
                    Platform = table.Column<string>(type: "text", nullable: true),
                    Revenue = table.Column<decimal>(type: "numeric", nullable: true),
                    ExternalClickId = table.Column<string>(type: "text", nullable: true),
                    SentToServer = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_ConversionEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversionEvents_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CurrencyConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BaseCurrency = table.Column<string>(type: "text", nullable: false),
                    SupportedCurrencies = table.Column<string[]>(type: "text[]", nullable: false),
                    DisplayFormat = table.Column<string>(type: "text", nullable: false),
                    AutoConvert = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomFieldDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    TextValue = table.Column<string>(type: "text", nullable: true),
                    NumberValue = table.Column<decimal>(type: "numeric", nullable: true),
                    DateValue = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BooleanValue = table.Column<bool>(type: "boolean", nullable: true),
                    JsonValue = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomFieldValues_CustomFieldDefinitions_CustomFieldDefinit~",
                        column: x => x.CustomFieldDefinitionId,
                        principalTable: "CustomFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DataImportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportType = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TotalRows = table.Column<int>(type: "integer", nullable: false),
                    ProcessedRows = table.Column<int>(type: "integer", nullable: false),
                    SuccessRows = table.Column<int>(type: "integer", nullable: false),
                    ErrorRows = table.Column<int>(type: "integer", nullable: false),
                    DuplicatesFound = table.Column<int>(type: "integer", nullable: false),
                    FieldMapping = table.Column<string>(type: "text", nullable: true),
                    ValidationErrors = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_DataImportJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataProcessingLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    PerformedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProcessingLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataProcessingLogs_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DataWarehouseExports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    ConnectionString = table.Column<string>(type: "text", nullable: false),
                    DatasetName = table.Column<string>(type: "text", nullable: true),
                    IncrementalSync = table.Column<bool>(type: "boolean", nullable: false),
                    CronSchedule = table.Column<string>(type: "text", nullable: false),
                    TableMapping = table.Column<string>(type: "text", nullable: true),
                    LastExportAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastExportRowCount = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_DataWarehouseExports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeploymentRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    Environment = table.Column<string>(type: "text", nullable: false),
                    Strategy = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CanaryPercentage = table.Column<int>(type: "integer", nullable: true),
                    HealthCheckUrl = table.Column<string>(type: "text", nullable: true),
                    HealthCheckPassed = table.Column<bool>(type: "boolean", nullable: false),
                    RollbackReason = table.Column<string>(type: "text", nullable: true),
                    DeployedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RolledBackAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeploymentRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DigitalWaivers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    ApplicableServices = table.Column<string>(type: "text", nullable: true),
                    ExpiryDays = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_DigitalWaivers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DuplicateClientMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientAId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientBId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "numeric", nullable: false),
                    MatchedFields = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    MergedIntoId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuplicateClientMatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailMarketingSyncs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    ApiKey = table.Column<string>(type: "text", nullable: true),
                    ListId = table.Column<string>(type: "text", nullable: true),
                    AutoSync = table.Column<bool>(type: "boolean", nullable: false),
                    FieldMapping = table.Column<string>(type: "text", nullable: true),
                    LastExportAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastExportCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailMarketingSyncs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Platform = table.Column<string>(type: "text", nullable: false),
                    ReviewerName = table.Column<string>(type: "text", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    ReviewText = table.Column<string>(type: "text", nullable: true),
                    ResponseText = table.Column<string>(type: "text", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExternalReviewId = table.Column<string>(type: "text", nullable: true),
                    Sentiment = table.Column<string>(type: "text", nullable: false),
                    ReviewDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalReviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    IsMultiStep = table.Column<bool>(type: "boolean", nullable: false),
                    TotalSteps = table.Column<int>(type: "integer", nullable: false),
                    ConditionalLogic = table.Column<string>(type: "text", nullable: true),
                    CustomCss = table.Column<string>(type: "text", nullable: true),
                    EmbedCode = table.Column<string>(type: "text", nullable: true),
                    RedirectUrl = table.Column<string>(type: "text", nullable: true),
                    SuccessMessage = table.Column<string>(type: "text", nullable: true),
                    SubmissionCount = table.Column<int>(type: "integer", nullable: false),
                    ViewCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FunnelSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FunnelId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    StepOrder = table.Column<int>(type: "integer", nullable: false),
                    StepType = table.Column<string>(type: "text", nullable: false),
                    PageUrl = table.Column<string>(type: "text", nullable: true),
                    EnteredCount = table.Column<int>(type: "integer", nullable: false),
                    CompletedCount = table.Column<int>(type: "integer", nullable: false),
                    DropOffRate = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FunnelSteps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GatedContents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    FileUrl = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    RequiresAuth = table.Column<bool>(type: "boolean", nullable: false),
                    RequiredMembershipTier = table.Column<string>(type: "text", nullable: true),
                    AccessCount = table.Column<int>(type: "integer", nullable: false),
                    IsDripContent = table.Column<bool>(type: "boolean", nullable: false),
                    DripDelayDays = table.Column<int>(type: "integer", nullable: true),
                    AvailableFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GatedContents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GeneratedContents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Keywords = table.Column<string>(type: "text", nullable: true),
                    IntentCluster = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsAIGenerated = table.Column<bool>(type: "boolean", nullable: false),
                    DuplicateCheckHash = table.Column<string>(type: "text", nullable: true),
                    WordCount = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratedContents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HangfireWorkerNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeId = table.Column<string>(type: "text", nullable: false),
                    HostName = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    QueuePriority = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyLimit = table.Column<int>(type: "integer", nullable: false),
                    LastHeartbeat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HangfireWorkerNodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HipaaConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    EncryptionAtRest = table.Column<bool>(type: "boolean", nullable: false),
                    EncryptionInTransit = table.Column<bool>(type: "boolean", nullable: false),
                    AccessLogging = table.Column<bool>(type: "boolean", nullable: false),
                    BaaDocument = table.Column<string>(type: "text", nullable: true),
                    LastAuditAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AuditFindings = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HipaaConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IncidentRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    AffectedServices = table.Column<string>(type: "text", nullable: true),
                    RootCause = table.Column<string>(type: "text", nullable: true),
                    ResolutionSteps = table.Column<string>(type: "text", nullable: true),
                    PostMortemUrl = table.Column<string>(type: "text", nullable: true),
                    OnCallResponderId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TimeTotResolutionMinutes = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IndexingStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PageUrl = table.Column<string>(type: "text", nullable: false),
                    SearchEngine = table.Column<string>(type: "text", nullable: false),
                    IsIndexed = table.Column<bool>(type: "boolean", nullable: false),
                    IsSubmitted = table.Column<bool>(type: "boolean", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastCheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    Impressions = table.Column<int>(type: "integer", nullable: false),
                    Clicks = table.Column<int>(type: "integer", nullable: false),
                    AveragePosition = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndexingStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Sku = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: true),
                    CostPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    SalePrice = table.Column<decimal>(type: "numeric", nullable: true),
                    QuantityOnHand = table.Column<int>(type: "integer", nullable: false),
                    ReorderLevel = table.Column<int>(type: "integer", nullable: false),
                    ReorderQuantity = table.Column<int>(type: "integer", nullable: true),
                    Supplier = table.Column<string>(type: "text", nullable: true),
                    SupplierSku = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsRetail = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LandingPages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    HtmlContent = table.Column<string>(type: "text", nullable: false),
                    CssOverrides = table.Column<string>(type: "text", nullable: true),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: true),
                    VariantGroup = table.Column<string>(type: "text", nullable: true),
                    VariantLabel = table.Column<string>(type: "text", nullable: true),
                    Views = table.Column<int>(type: "integer", nullable: false),
                    Conversions = table.Column<int>(type: "integer", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LandingPages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LandingPages_campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "campaigns",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LegalDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Locale = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyBalances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalPoints = table.Column<int>(type: "integer", nullable: false),
                    LifetimePoints = table.Column<int>(type: "integer", nullable: false),
                    CurrentTier = table.Column<string>(type: "text", nullable: false),
                    StampCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyBalances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoyaltyBalances_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyPrograms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PointsPerDollar = table.Column<decimal>(type: "numeric", nullable: false),
                    PointsRedemptionRate = table.Column<decimal>(type: "numeric", nullable: false),
                    ReferralBonusPoints = table.Column<int>(type: "integer", nullable: false),
                    Tiers = table.Column<string>(type: "text", nullable: false),
                    PointExpiryDays = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyPrograms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketingAnalyticsRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MetricType = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<decimal>(type: "numeric", nullable: false),
                    RecordDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Insight = table.Column<string>(type: "text", nullable: true),
                    IsAnomaly = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingAnalyticsRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketingConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessUrl = table.Column<string>(type: "text", nullable: false),
                    IndustryNiche = table.Column<string>(type: "text", nullable: true),
                    TargetAudience = table.Column<string>(type: "text", nullable: true),
                    PrimaryGoal = table.Column<string>(type: "text", nullable: false),
                    TargetRegions = table.Column<string>(type: "text", nullable: true),
                    IsAutonomousMode = table.Column<bool>(type: "boolean", nullable: false),
                    IsOnboarded = table.Column<bool>(type: "boolean", nullable: false),
                    LastCrawlAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketingForecasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ForecastType = table.Column<string>(type: "text", nullable: false),
                    HorizonDays = table.Column<int>(type: "integer", nullable: false),
                    PredictedValue = table.Column<decimal>(type: "numeric", nullable: false),
                    ConfidencePercent = table.Column<decimal>(type: "numeric", nullable: false),
                    ForecastDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Methodology = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingForecasts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MemberProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgressPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BookmarkData = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberProgresses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MigrationRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MigrationId = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Script = table.Column<string>(type: "text", nullable: false),
                    RollbackScript = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    IsZeroDowntime = table.Column<bool>(type: "boolean", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RolledBackAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MigrationRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MlModelTrainings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelType = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TrainingDataSize = table.Column<int>(type: "integer", nullable: false),
                    Accuracy = table.Column<decimal>(type: "numeric", nullable: true),
                    Precision = table.Column<decimal>(type: "numeric", nullable: true),
                    Recall = table.Column<decimal>(type: "numeric", nullable: true),
                    ModelArtifactUrl = table.Column<string>(type: "text", nullable: true),
                    Hyperparameters = table.Column<string>(type: "text", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_MlModelTrainings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    ActionUrl = table.Column<string>(type: "text", nullable: true),
                    EntityType = table.Column<string>(type: "text", nullable: true),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Priority = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OfflineSyncQueues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<string>(type: "text", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ConflictResolution = table.Column<string>(type: "text", nullable: true),
                    QueuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfflineSyncQueues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "text", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric", nullable: false),
                    Tax = table.Column<decimal>(type: "numeric", nullable: false),
                    ShippingCost = table.Column<decimal>(type: "numeric", nullable: false),
                    Total = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ShippingAddress = table.Column<string>(type: "text", nullable: true),
                    TrackingNumber = table.Column<string>(type: "text", nullable: true),
                    PaymentIntentId = table.Column<string>(type: "text", nullable: true),
                    ShippedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    CorrelationId = table.Column<string>(type: "text", nullable: true),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PageAnalyticsRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PageUrl = table.Column<string>(type: "text", nullable: false),
                    PageType = table.Column<string>(type: "text", nullable: false),
                    TotalViews = table.Column<int>(type: "integer", nullable: false),
                    UniqueVisitors = table.Column<int>(type: "integer", nullable: false),
                    BounceRate = table.Column<decimal>(type: "numeric", nullable: false),
                    AvgTimeOnPageSeconds = table.Column<decimal>(type: "numeric", nullable: false),
                    Conversions = table.Column<int>(type: "integer", nullable: false),
                    ConversionRate = table.Column<decimal>(type: "numeric", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageAnalyticsRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PartnerAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerName = table.Column<string>(type: "text", nullable: false),
                    PartnerType = table.Column<string>(type: "text", nullable: false),
                    ContactEmail = table.Column<string>(type: "text", nullable: false),
                    ContactPhone = table.Column<string>(type: "text", nullable: true),
                    RevenueSharePercent = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalEarnings = table.Column<decimal>(type: "numeric", nullable: false),
                    PendingPayout = table.Column<decimal>(type: "numeric", nullable: false),
                    ManagedAccounts = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    PayoutMethod = table.Column<string>(type: "text", nullable: true),
                    PayoutDetails = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PluginDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    Author = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    IconUrl = table.Column<string>(type: "text", nullable: true),
                    ManifestJson = table.Column<string>(type: "text", nullable: true),
                    SettingsSchema = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    IsFree = table.Column<bool>(type: "boolean", nullable: false),
                    InstallCount = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<decimal>(type: "numeric", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PluginDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PredictiveScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScoreType = table.Column<string>(type: "text", nullable: false),
                    Score = table.Column<decimal>(type: "numeric", nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric", nullable: false),
                    Factors = table.Column<string>(type: "text", nullable: true),
                    Recommendation = table.Column<string>(type: "text", nullable: true),
                    ScoredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredictiveScores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Sku = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    ProductType = table.Column<string>(type: "text", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    StockQuantity = table.Column<int>(type: "integer", nullable: false),
                    TrackInventory = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Variants = table.Column<string>(type: "text", nullable: true),
                    DigitalFileUrl = table.Column<string>(type: "text", nullable: true),
                    Weight = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromoCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    DiscountType = table.Column<string>(type: "text", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "numeric", nullable: false),
                    MinimumOrderAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    UsageLimit = table.Column<int>(type: "integer", nullable: true),
                    TimesUsed = table.Column<int>(type: "integer", nullable: false),
                    FirstTimeOnly = table.Column<bool>(type: "boolean", nullable: false),
                    ApplicableServices = table.Column<string>(type: "text", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_PromoCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PushNotificationTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceToken = table.Column<string>(type: "text", nullable: false),
                    Platform = table.Column<string>(type: "text", nullable: false),
                    DeviceModel = table.Column<string>(type: "text", nullable: true),
                    OsVersion = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushNotificationTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReferralRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferrerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferredTenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferralCode = table.Column<string>(type: "text", nullable: false),
                    ReferredEmail = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ReferrerCredit = table.Column<decimal>(type: "numeric", nullable: false),
                    ReferredCredit = table.Column<decimal>(type: "numeric", nullable: false),
                    QualifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RewardedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferralRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegionConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegionCode = table.Column<string>(type: "text", nullable: false),
                    RegionName = table.Column<string>(type: "text", nullable: false),
                    AzureRegion = table.Column<string>(type: "text", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    LastSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DataResidency = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegionConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ReportType = table.Column<string>(type: "text", nullable: false),
                    DataSource = table.Column<string>(type: "text", nullable: false),
                    Columns = table.Column<string>(type: "text", nullable: true),
                    Filters = table.Column<string>(type: "text", nullable: true),
                    GroupBy = table.Column<string>(type: "text", nullable: true),
                    SortBy = table.Column<string>(type: "text", nullable: true),
                    ScheduleCron = table.Column<string>(type: "text", nullable: true),
                    ScheduleRecipients = table.Column<string>(type: "text", nullable: true),
                    ExportFormat = table.Column<string>(type: "text", nullable: false),
                    IsTemplate = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Resources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    Amenities = table.Column<string>(type: "text", nullable: true),
                    HourlyRate = table.Column<decimal>(type: "numeric", nullable: true),
                    Color = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LinkedServiceIds = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReviewRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    Channel = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ReviewUrl = table.Column<string>(type: "text", nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_ReviewRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewRequests_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesPipelines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_SalesPipelines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SandboxEnvironments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SandboxId = table.Column<string>(type: "text", nullable: false),
                    ApiKeyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeedDataConfig = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SandboxEnvironments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScalingPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceType = table.Column<string>(type: "text", nullable: false),
                    MinInstances = table.Column<int>(type: "integer", nullable: false),
                    MaxInstances = table.Column<int>(type: "integer", nullable: false),
                    CurrentInstances = table.Column<int>(type: "integer", nullable: false),
                    CpuThresholdPercent = table.Column<int>(type: "integer", nullable: false),
                    MemoryThresholdPercent = table.Column<int>(type: "integer", nullable: false),
                    CooldownSeconds = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScalingPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AllDay = table.Column<bool>(type: "boolean", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleBlocks_StaffMembers_StaffId",
                        column: x => x.StaffId,
                        principalTable: "StaffMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SdkReleases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    DownloadUrl = table.Column<string>(type: "text", nullable: true),
                    ChangelogUrl = table.Column<string>(type: "text", nullable: true),
                    DownloadCount = table.Column<int>(type: "integer", nullable: false),
                    ReleasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SdkReleases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SectionTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    HtmlContent = table.Column<string>(type: "text", nullable: false),
                    CssContent = table.Column<string>(type: "text", nullable: true),
                    JsContent = table.Column<string>(type: "text", nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "text", nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectionTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SeoAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PageUrl = table.Column<string>(type: "text", nullable: false),
                    CurrentTitle = table.Column<string>(type: "text", nullable: true),
                    SuggestedTitle = table.Column<string>(type: "text", nullable: true),
                    CurrentMetaDescription = table.Column<string>(type: "text", nullable: true),
                    SuggestedMetaDescription = table.Column<string>(type: "text", nullable: true),
                    StructuredDataJson = table.Column<string>(type: "text", nullable: true),
                    InternalLinkSuggestions = table.Column<string>(type: "text", nullable: true),
                    ContentGaps = table.Column<string>(type: "text", nullable: true),
                    Score = table.Column<int>(type: "integer", nullable: true),
                    AnalyzedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeoAnalyses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServicePackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    TotalPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    OriginalPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    ServiceIds = table.Column<string>(type: "text", nullable: false),
                    SessionCount = table.Column<int>(type: "integer", nullable: false),
                    SessionsUsed = table.Column<int>(type: "integer", nullable: false),
                    ValidityDays = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_ServicePackages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShippingProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ApiKey = table.Column<string>(type: "text", nullable: true),
                    AccountNumber = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SupportedRegions = table.Column<string>(type: "text", nullable: true),
                    DefaultServiceLevel = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SmsA2PBrands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandName = table.Column<string>(type: "text", nullable: false),
                    TcrBrandId = table.Column<string>(type: "text", nullable: false),
                    Ein = table.Column<string>(type: "text", nullable: false),
                    VerticalType = table.Column<string>(type: "text", nullable: false),
                    TrustScore = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmsA2PBrands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SmsConsents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    IsOptedIn = table.Column<bool>(type: "boolean", nullable: false),
                    ConsentMethod = table.Column<string>(type: "text", nullable: false),
                    ConsentedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OptedOutAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OptOutKeyword = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmsConsents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Soc2Evidences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ControlId = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    EvidenceType = table.Column<string>(type: "text", nullable: false),
                    EvidenceUrl = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CollectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Soc2Evidences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SocialPosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Platform = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Hashtags = table.Column<string>(type: "text", nullable: true),
                    CTA = table.Column<string>(type: "text", nullable: true),
                    MediaUrl = table.Column<string>(type: "text", nullable: true),
                    Tone = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExternalPostId = table.Column<string>(type: "text", nullable: true),
                    Impressions = table.Column<int>(type: "integer", nullable: false),
                    Engagements = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialPosts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SsoConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    Protocol = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<string>(type: "text", nullable: true),
                    MetadataUrl = table.Column<string>(type: "text", nullable: true),
                    SignInUrl = table.Column<string>(type: "text", nullable: true),
                    Certificate = table.Column<string>(type: "text", nullable: true),
                    ClientId = table.Column<string>(type: "text", nullable: true),
                    ClientSecret = table.Column<string>(type: "text", nullable: true),
                    AttributeMapping = table.Column<string>(type: "text", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    EnforceForAllUsers = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SsoConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StaffTips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    PaymentMethod = table.Column<string>(type: "text", nullable: false),
                    IsPooled = table.Column<bool>(type: "boolean", nullable: false),
                    PoolDistribution = table.Column<string>(type: "text", nullable: true),
                    TipDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffTips", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ContactName = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    Website = table.Column<string>(type: "text", nullable: true),
                    PaymentTerms = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantIntegrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IntegrationId = table.Column<string>(type: "text", nullable: false),
                    IsConnected = table.Column<bool>(type: "boolean", nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: true),
                    RefreshToken = table.Column<string>(type: "text", nullable: true),
                    ApiKey = table.Column<string>(type: "text", nullable: true),
                    Settings = table.Column<string>(type: "text", nullable: true),
                    ConnectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantIntegrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantManagements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SuspensionReason = table.Column<string>(type: "text", nullable: true),
                    SuspendedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ScheduledDeletionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsSandbox = table.Column<bool>(type: "boolean", nullable: false),
                    LastBackupId = table.Column<string>(type: "text", nullable: true),
                    LastBackupAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DataExportUrl = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantManagements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantQuotas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaxApiRequestsPerMinute = table.Column<int>(type: "integer", nullable: false),
                    MaxStorageMb = table.Column<int>(type: "integer", nullable: false),
                    MaxStaffMembers = table.Column<int>(type: "integer", nullable: false),
                    MaxClients = table.Column<int>(type: "integer", nullable: false),
                    MaxMonthlyEmails = table.Column<int>(type: "integer", nullable: false),
                    MaxMonthlySms = table.Column<int>(type: "integer", nullable: false),
                    MaxMonthlyAiTokens = table.Column<int>(type: "integer", nullable: false),
                    MaxMonthlyAiBudget = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxBookingsPerDay = table.Column<int>(type: "integer", nullable: false),
                    MaxWorkflows = table.Column<int>(type: "integer", nullable: false),
                    CurrentStorageUsedMb = table.Column<int>(type: "integer", nullable: false),
                    CurrentMonthlyEmails = table.Column<int>(type: "integer", nullable: false),
                    CurrentMonthlySms = table.Column<int>(type: "integer", nullable: false),
                    CurrentMonthlyAiTokens = table.Column<int>(type: "integer", nullable: false),
                    QuotaResetDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantQuotas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TranslationEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Locale = table.Column<string>(type: "text", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: true),
                    IsCustom = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranslationEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TwoFactorConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Method = table.Column<string>(type: "text", nullable: false),
                    TotpSecret = table.Column<string>(type: "text", nullable: true),
                    BackupCodes = table.Column<string>(type: "text", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    EnforcedByRole = table.Column<bool>(type: "boolean", nullable: false),
                    TrustedDevices = table.Column<string>(type: "text", nullable: true),
                    LastVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TwoFactorConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserUiPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DarkModeEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    KeyboardShortcuts = table.Column<string>(type: "text", nullable: true),
                    CalendarColorCoding = table.Column<string>(type: "text", nullable: true),
                    DashboardLayout = table.Column<string>(type: "text", nullable: true),
                    Timezone = table.Column<string>(type: "text", nullable: true),
                    Locale = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserUiPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisualRegressionTests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestName = table.Column<string>(type: "text", nullable: false),
                    PageUrl = table.Column<string>(type: "text", nullable: false),
                    BaselineScreenshotUrl = table.Column<string>(type: "text", nullable: true),
                    CurrentScreenshotUrl = table.Column<string>(type: "text", nullable: true),
                    DiffScreenshotUrl = table.Column<string>(type: "text", nullable: true),
                    DiffPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisualRegressionTests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VoiceCalls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    Direction = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    RecordingUrl = table.Column<string>(type: "text", nullable: true),
                    TranscriptText = table.Column<string>(type: "text", nullable: true),
                    TranscriptSummary = table.Column<string>(type: "text", nullable: true),
                    Purpose = table.Column<string>(type: "text", nullable: false),
                    WasTransferredToHuman = table.Column<bool>(type: "boolean", nullable: false),
                    BookingCreatedId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceCalls", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WaitlistConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AutoBookEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    MaxWaitMinutes = table.Column<int>(type: "integer", nullable: false),
                    ExpiryHours = table.Column<int>(type: "integer", nullable: false),
                    NotifyOnAvailability = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationChannel = table.Column<string>(type: "text", nullable: false),
                    PriorityQueueEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    PriorityRules = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaitlistConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookDeliveryLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WebhookType = table.Column<string>(type: "text", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    ExternalEventId = table.Column<string>(type: "text", nullable: true),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "text", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookDeliveryLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookEndpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Secret = table.Column<string>(type: "text", nullable: true),
                    Events = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    FailureCount = table.Column<int>(type: "integer", nullable: false),
                    LastTriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSuccessAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookEndpoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PhoneNumberId = table.Column<string>(type: "text", nullable: false),
                    BusinessAccountId = table.Column<string>(type: "text", nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    WebhookVerifyToken = table.Column<string>(type: "text", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateName = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    HeaderType = table.Column<string>(type: "text", nullable: false),
                    BodyText = table.Column<string>(type: "text", nullable: false),
                    FooterText = table.Column<string>(type: "text", nullable: true),
                    Buttons = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    MetaTemplateId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WhiteLabelConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomDomain = table.Column<string>(type: "text", nullable: true),
                    CustomLogoUrl = table.Column<string>(type: "text", nullable: true),
                    PrimaryColor = table.Column<string>(type: "text", nullable: true),
                    SecondaryColor = table.Column<string>(type: "text", nullable: true),
                    CustomEmailDomain = table.Column<string>(type: "text", nullable: true),
                    RemovePoweredBy = table.Column<bool>(type: "boolean", nullable: false),
                    CustomFavicon = table.Column<string>(type: "text", nullable: true),
                    CustomCss = table.Column<string>(type: "text", nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    DomainVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhiteLabelConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalCampaignId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Platform = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DailyBudget = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalBudget = table.Column<decimal>(type: "numeric", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Objective = table.Column<string>(type: "text", nullable: true),
                    OptimizationGoal = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdCampaigns_AdAccounts_AdAccountId",
                        column: x => x.AdAccountId,
                        principalTable: "AdAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AIMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ToolCalls = table.Column<string>(type: "text", nullable: true),
                    ToolOutputs = table.Column<string>(type: "text", nullable: true),
                    TokenUsage = table.Column<decimal>(type: "numeric", nullable: true),
                    Cost = table.Column<decimal>(type: "numeric", nullable: true),
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
                    table.PrimaryKey("PK_AIMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIMessages_AIConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "AIConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WaiverSignatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WaiverId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    SignatureData = table.Column<string>(type: "text", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: false),
                    SignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_WaiverSignatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaiverSignatures_DigitalWaivers_WaiverId",
                        column: x => x.WaiverId,
                        principalTable: "DigitalWaivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    FieldType = table.Column<int>(type: "integer", nullable: false),
                    Placeholder = table.Column<string>(type: "text", nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    ValidationRules = table.Column<string>(type: "text", nullable: true),
                    Options = table.Column<string>(type: "text", nullable: true),
                    StepNumber = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ConditionalVisibility = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormFields_FormDefinitions_FormDefinitionId",
                        column: x => x.FormDefinitionId,
                        principalTable: "FormDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FormDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Data = table.Column<string>(type: "text", nullable: false),
                    SubmitterEmail = table.Column<string>(type: "text", nullable: true),
                    SubmitterName = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<string>(type: "text", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormSubmissions_FormDefinitions_FormDefinitionId",
                        column: x => x.FormDefinitionId,
                        principalTable: "FormDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentCalendars",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    Platform = table.Column<string>(type: "text", nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    GeneratedContentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentCalendars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentCalendars_GeneratedContents_GeneratedContentId",
                        column: x => x.GeneratedContentId,
                        principalTable: "GeneratedContents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    QuantityAfter = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    ReferenceType = table.Column<string>(type: "text", nullable: true),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeadCaptures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LandingPageId = table.Column<Guid>(type: "uuid", nullable: true),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<string>(type: "text", nullable: true),
                    Medium = table.Column<string>(type: "text", nullable: true),
                    Campaign = table.Column<string>(type: "text", nullable: true),
                    AdPlatform = table.Column<string>(type: "text", nullable: true),
                    FormData = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_LeadCaptures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeadCaptures_LandingPages_LandingPageId",
                        column: x => x.LandingPageId,
                        principalTable: "LandingPages",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PluginInstallations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PluginId = table.Column<Guid>(type: "uuid", nullable: false),
                    SettingsJson = table.Column<string>(type: "text", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    InstalledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PluginInstallations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PluginInstallations_PluginDefinitions_PluginId",
                        column: x => x.PluginId,
                        principalTable: "PluginDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CartItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessionId = table.Column<string>(type: "text", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    SelectedVariant = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CartItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PipelineStages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PipelineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    WinProbability = table.Column<decimal>(type: "numeric", nullable: false),
                    Color = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PipelineStages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PipelineStages_SalesPipelines_PipelineId",
                        column: x => x.PipelineId,
                        principalTable: "SalesPipelines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SmsCampaignRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignType = table.Column<string>(type: "text", nullable: false),
                    UseCase = table.Column<string>(type: "text", nullable: false),
                    SampleMessages = table.Column<string>(type: "text", nullable: false),
                    OptInWorkflow = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ThroughputLimit = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmsCampaignRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SmsCampaignRegistrations_SmsA2PBrands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "SmsA2PBrands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "text", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpectedDeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ItemsJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Deals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PipelineId = table.Column<Guid>(type: "uuid", nullable: false),
                    StageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ExpectedCloseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LossReason = table.Column<string>(type: "text", nullable: true),
                    AssignedToId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Deals_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Deals_PipelineStages_StageId",
                        column: x => x.StageId,
                        principalTable: "PipelineStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Deals_SalesPipelines_PipelineId",
                        column: x => x.PipelineId,
                        principalTable: "SalesPipelines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DealActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DealId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityType = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    PerformedById = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_DealActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DealActivities_Deals_DealId",
                        column: x => x.DealId,
                        principalTable: "Deals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_ParentTenantId",
                table: "Tenants",
                column: "ParentTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_StaffId_StartTime_EndTime",
                table: "Bookings",
                columns: new[] { "StaffId", "StartTime", "EndTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AdCampaigns_AdAccountId",
                table: "AdCampaigns",
                column: "AdAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AIConversations_AssignedStaffId",
                table: "AIConversations",
                column: "AssignedStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_AIConversations_ClientId",
                table: "AIConversations",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_AIMessages_ConversationId",
                table: "AIMessages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ProductId",
                table: "CartItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentCalendars_GeneratedContentId",
                table: "ContentCalendars",
                column: "GeneratedContentId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversionEvents_ClientId",
                table: "ConversionEvents",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldValues_CustomFieldDefinitionId",
                table: "CustomFieldValues",
                column: "CustomFieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_DataProcessingLogs_ClientId",
                table: "DataProcessingLogs",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_DealActivities_DealId",
                table: "DealActivities",
                column: "DealId");

            migrationBuilder.CreateIndex(
                name: "IX_Deals_ClientId",
                table: "Deals",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Deals_PipelineId",
                table: "Deals",
                column: "PipelineId");

            migrationBuilder.CreateIndex(
                name: "IX_Deals_StageId",
                table: "Deals",
                column: "StageId");

            migrationBuilder.CreateIndex(
                name: "IX_FormFields_FormDefinitionId",
                table: "FormFields",
                column: "FormDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_FormDefinitionId",
                table: "FormSubmissions",
                column: "FormDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_InventoryItemId",
                table: "InventoryTransactions",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_LandingPages_CampaignId",
                table: "LandingPages",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadCaptures_LandingPageId",
                table: "LeadCaptures",
                column: "LandingPageId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyBalances_ClientId",
                table: "LoyaltyBalances",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PipelineStages_PipelineId",
                table: "PipelineStages",
                column: "PipelineId");

            migrationBuilder.CreateIndex(
                name: "IX_PluginInstallations_PluginId",
                table: "PluginInstallations",
                column: "PluginId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewRequests_ClientId",
                table: "ReviewRequests",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleBlocks_StaffId",
                table: "ScheduleBlocks",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_SmsCampaignRegistrations_BrandId",
                table: "SmsCampaignRegistrations",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_WaiverSignatures_WaiverId",
                table: "WaiverSignatures",
                column: "WaiverId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tenants_Tenants_ParentTenantId",
                table: "Tenants",
                column: "ParentTenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tenants_Tenants_ParentTenantId",
                table: "Tenants");

            migrationBuilder.DropTable(
                name: "AccessibilityScans");

            migrationBuilder.DropTable(
                name: "AccountingSyncConfigs");

            migrationBuilder.DropTable(
                name: "AdCampaigns");

            migrationBuilder.DropTable(
                name: "AdminImpersonationLogs");

            migrationBuilder.DropTable(
                name: "AgentActions");

            migrationBuilder.DropTable(
                name: "AIAgentConfigs");

            migrationBuilder.DropTable(
                name: "AIDecisionLogs");

            migrationBuilder.DropTable(
                name: "AIKnowledgeBases");

            migrationBuilder.DropTable(
                name: "AIMessages");

            migrationBuilder.DropTable(
                name: "ApiErrorCodes");

            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "AuditEntries");

            migrationBuilder.DropTable(
                name: "BookingCheckIns");

            migrationBuilder.DropTable(
                name: "BusinessListings");

            migrationBuilder.DropTable(
                name: "CalendarSyncs");

            migrationBuilder.DropTable(
                name: "CartItems");

            migrationBuilder.DropTable(
                name: "ChatVisitors");

            migrationBuilder.DropTable(
                name: "ChatWidgets");

            migrationBuilder.DropTable(
                name: "ClassDefinitions");

            migrationBuilder.DropTable(
                name: "ClientPortalConfigs");

            migrationBuilder.DropTable(
                name: "ContentCalendars");

            migrationBuilder.DropTable(
                name: "ConversionAnalyses");

            migrationBuilder.DropTable(
                name: "ConversionEvents");

            migrationBuilder.DropTable(
                name: "CurrencyConfigs");

            migrationBuilder.DropTable(
                name: "CustomFieldValues");

            migrationBuilder.DropTable(
                name: "DataImportJobs");

            migrationBuilder.DropTable(
                name: "DataProcessingLogs");

            migrationBuilder.DropTable(
                name: "DataWarehouseExports");

            migrationBuilder.DropTable(
                name: "DealActivities");

            migrationBuilder.DropTable(
                name: "DeploymentRecords");

            migrationBuilder.DropTable(
                name: "DuplicateClientMatches");

            migrationBuilder.DropTable(
                name: "EmailMarketingSyncs");

            migrationBuilder.DropTable(
                name: "ExternalReviews");

            migrationBuilder.DropTable(
                name: "FormFields");

            migrationBuilder.DropTable(
                name: "FormSubmissions");

            migrationBuilder.DropTable(
                name: "FunnelSteps");

            migrationBuilder.DropTable(
                name: "GatedContents");

            migrationBuilder.DropTable(
                name: "HangfireWorkerNodes");

            migrationBuilder.DropTable(
                name: "HipaaConfigs");

            migrationBuilder.DropTable(
                name: "IncidentRecords");

            migrationBuilder.DropTable(
                name: "IndexingStatuses");

            migrationBuilder.DropTable(
                name: "InventoryTransactions");

            migrationBuilder.DropTable(
                name: "LeadCaptures");

            migrationBuilder.DropTable(
                name: "LegalDocuments");

            migrationBuilder.DropTable(
                name: "LoyaltyBalances");

            migrationBuilder.DropTable(
                name: "LoyaltyPrograms");

            migrationBuilder.DropTable(
                name: "MarketingAnalyticsRecords");

            migrationBuilder.DropTable(
                name: "MarketingConfigs");

            migrationBuilder.DropTable(
                name: "MarketingForecasts");

            migrationBuilder.DropTable(
                name: "MemberProgresses");

            migrationBuilder.DropTable(
                name: "MigrationRecords");

            migrationBuilder.DropTable(
                name: "MlModelTrainings");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "OfflineSyncQueues");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "PageAnalyticsRecords");

            migrationBuilder.DropTable(
                name: "PartnerAccounts");

            migrationBuilder.DropTable(
                name: "PluginInstallations");

            migrationBuilder.DropTable(
                name: "PredictiveScores");

            migrationBuilder.DropTable(
                name: "PromoCodes");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "PushNotificationTokens");

            migrationBuilder.DropTable(
                name: "ReferralRecords");

            migrationBuilder.DropTable(
                name: "RegionConfigs");

            migrationBuilder.DropTable(
                name: "ReportDefinitions");

            migrationBuilder.DropTable(
                name: "Resources");

            migrationBuilder.DropTable(
                name: "ReviewRequests");

            migrationBuilder.DropTable(
                name: "SandboxEnvironments");

            migrationBuilder.DropTable(
                name: "ScalingPolicies");

            migrationBuilder.DropTable(
                name: "ScheduleBlocks");

            migrationBuilder.DropTable(
                name: "SdkReleases");

            migrationBuilder.DropTable(
                name: "SectionTemplates");

            migrationBuilder.DropTable(
                name: "SeoAnalyses");

            migrationBuilder.DropTable(
                name: "ServicePackages");

            migrationBuilder.DropTable(
                name: "ShippingProviders");

            migrationBuilder.DropTable(
                name: "SmsCampaignRegistrations");

            migrationBuilder.DropTable(
                name: "SmsConsents");

            migrationBuilder.DropTable(
                name: "Soc2Evidences");

            migrationBuilder.DropTable(
                name: "SocialPosts");

            migrationBuilder.DropTable(
                name: "SsoConfigs");

            migrationBuilder.DropTable(
                name: "StaffTips");

            migrationBuilder.DropTable(
                name: "TenantIntegrations");

            migrationBuilder.DropTable(
                name: "TenantManagements");

            migrationBuilder.DropTable(
                name: "TenantQuotas");

            migrationBuilder.DropTable(
                name: "TranslationEntries");

            migrationBuilder.DropTable(
                name: "TwoFactorConfigs");

            migrationBuilder.DropTable(
                name: "UserUiPreferences");

            migrationBuilder.DropTable(
                name: "VisualRegressionTests");

            migrationBuilder.DropTable(
                name: "VoiceCalls");

            migrationBuilder.DropTable(
                name: "WaitlistConfigs");

            migrationBuilder.DropTable(
                name: "WaiverSignatures");

            migrationBuilder.DropTable(
                name: "WebhookDeliveryLogs");

            migrationBuilder.DropTable(
                name: "WebhookEndpoints");

            migrationBuilder.DropTable(
                name: "WhatsAppConfigs");

            migrationBuilder.DropTable(
                name: "WhatsAppTemplates");

            migrationBuilder.DropTable(
                name: "WhiteLabelConfigs");

            migrationBuilder.DropTable(
                name: "AdAccounts");

            migrationBuilder.DropTable(
                name: "AIConversations");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "GeneratedContents");

            migrationBuilder.DropTable(
                name: "Deals");

            migrationBuilder.DropTable(
                name: "FormDefinitions");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "LandingPages");

            migrationBuilder.DropTable(
                name: "PluginDefinitions");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "SmsA2PBrands");

            migrationBuilder.DropTable(
                name: "DigitalWaivers");

            migrationBuilder.DropTable(
                name: "PipelineStages");

            migrationBuilder.DropTable(
                name: "SalesPipelines");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_ParentTenantId",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_StaffId_StartTime_EndTime",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ParentTenantId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "TipAmount",
                table: "StaffCommissions");

            migrationBuilder.DropColumn(
                name: "TipAmount",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DefaultValue",
                table: "CustomFieldDefinitions");

            migrationBuilder.DropColumn(
                name: "FieldType",
                table: "CustomFieldDefinitions");

            migrationBuilder.DropColumn(
                name: "IsSearchable",
                table: "CustomFieldDefinitions");

            migrationBuilder.DropColumn(
                name: "ValidationRules",
                table: "CustomFieldDefinitions");

            migrationBuilder.DropColumn(
                name: "GroupSize",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PerParticipantPrice",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "Label",
                table: "CustomFieldDefinitions",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "EntityType",
                table: "CustomFieldDefinitions",
                newName: "Key");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:hstore", ",,");

            migrationBuilder.AlterColumn<string>(
                name: "Options",
                table: "CustomFieldDefinitions",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_StaffId",
                table: "Bookings",
                column: "StaffId");
        }
    }
}
