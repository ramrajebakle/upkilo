using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinalFixCorrected : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationFallbackChannels");

            migrationBuilder.AddColumn<string>(
                name: "LanguageCode",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AuditLogsV2",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    OldValues = table.Column<string>(type: "text", nullable: true),
                    NewValues = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    Details = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequestId = table.Column<string>(type: "text", nullable: true),
                    CorrelationId = table.Column<string>(type: "text", nullable: true),
                    DurationMs = table.Column<double>(type: "double precision", nullable: true),
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
                    table.PrimaryKey("PK_AuditLogsV2", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "experiments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    VariantA = table.Column<string>(type: "text", nullable: false),
                    VariantB = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TrafficSplit = table.Column<double>(type: "double precision", nullable: false),
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
                    table.PrimaryKey("PK_experiments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "prompt_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    prompt_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    system_prompt = table.Column<string>(type: "text", nullable: false),
                    user_prompt_template = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    change_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    model = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    temperature = table.Column<double>(type: "double precision", nullable: false),
                    max_tokens = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    activated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rolled_back_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    model_params = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prompt_versions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_daily_metrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revenue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    booking_count = table.Column<int>(type: "integer", nullable: false),
                    new_client_count = table.Column<int>(type: "integer", nullable: false),
                    cancelled_booking_count = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_tenant_daily_metrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_dashboard_stats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_clients = table.Column<int>(type: "integer", nullable: false),
                    total_bookings = table.Column<int>(type: "integer", nullable: false),
                    total_revenue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    pending_bookings = table.Column<int>(type: "integer", nullable: false),
                    completed_bookings = table.Column<int>(type: "integer", nullable: false),
                    revenue_this_month = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    bookings_this_month = table.Column<int>(type: "integer", nullable: false),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_tenant_dashboard_stats", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "BusinessListings",
                columns: new[] { "Id", "AverageRating", "BusinessName", "Category", "City", "Country", "CreatedAt", "DeletedAt", "DeletedBy", "Description", "IsActive", "IsDeleted", "IsFeatured", "IsVerified", "LogoUrl", "OperatingHours", "Phone", "PremiumScore", "ReviewCount", "ServiceTags", "Slug", "State", "TenantId", "UpdatedAt", "Version", "Website", "ZipCode" },
                values: new object[,]
                {
                    { new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"), 4.9000000000000004, "Luxe Hair Studio", "Beauty", "New York", "US", new DateTime(2026, 4, 6, 23, 1, 49, 824, DateTimeKind.Utc).AddTicks(1770), null, null, "Premium hair styling and coloring in the heart of Manhattan. Our expert stylists have over 15 years of experience.", true, false, true, false, null, null, null, 95.5, 128, "haircut,color,balayage,styling", "luxe-hair-studio-nyc", "NY", new Guid("0192a3b4-c5d6-4e5f-8901-234567890123"), new DateTime(2026, 4, 6, 23, 1, 49, 824, DateTimeKind.Utc).AddTicks(1775), 1, null, null },
                    { new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"), 4.7999999999999998, "Zen Garden Spa", "Wellness", "San Francisco", "US", new DateTime(2026, 4, 6, 23, 1, 49, 824, DateTimeKind.Utc).AddTicks(1849), null, null, "A tranquil escape in San Francisco. Massage therapy, organic facials, and holistic wellness treatments.", true, false, true, false, null, null, null, 92.0, 215, "massage,facial,wellness,spa", "zen-garden-spa-sf", "CA", new Guid("12345678-1234-1234-1234-123456781234"), new DateTime(2026, 4, 6, 23, 1, 49, 824, DateTimeKind.Utc).AddTicks(1851), 1, null, null },
                    { new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"), 4.7000000000000002, "Iron Forge Gym", "Fitness", "Austin", "US", new DateTime(2026, 4, 6, 23, 1, 49, 824, DateTimeKind.Utc).AddTicks(1866), null, null, "Top-tier strength and conditioning facility in Austin. Personal training and group classes available.", true, false, true, false, null, null, null, 88.5, 89, "gym,training,fitness,weights", "iron-forge-gym-austin", "TX", new Guid("87654321-4321-4321-4321-876543214321"), new DateTime(2026, 4, 6, 23, 1, 49, 824, DateTimeKind.Utc).AddTicks(1867), 1, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_daily_metrics_TenantId_date",
                table: "tenant_daily_metrics",
                columns: new[] { "TenantId", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_dashboard_stats_TenantId",
                table: "tenant_dashboard_stats",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogsV2");

            migrationBuilder.DropTable(
                name: "experiments");

            migrationBuilder.DropTable(
                name: "prompt_versions");

            migrationBuilder.DropTable(
                name: "tenant_daily_metrics");

            migrationBuilder.DropTable(
                name: "tenant_dashboard_stats");

            migrationBuilder.DeleteData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"));

            migrationBuilder.DeleteData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"));

            migrationBuilder.DeleteData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"));

            migrationBuilder.DropColumn(
                name: "LanguageCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "Users");

            migrationBuilder.CreateTable(
                name: "NotificationFallbackChannels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    FirstFallbackChannel = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationType = table.Column<string>(type: "text", nullable: false),
                    PrimaryChannel = table.Column<string>(type: "text", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
                    SecondFallbackChannel = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimeoutSecondsBeforeFallback = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_NotificationFallbackChannels_UserId",
                table: "NotificationFallbackChannels",
                column: "UserId");
        }
    }
}
