using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadScoreToClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LeadScore",
                table: "Clients",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CreditAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    Balance = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    LastTransactionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OAuthApps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppName = table.Column<string>(type: "text", nullable: false),
                    ClientId = table.Column<string>(type: "text", nullable: false),
                    ClientSecretHash = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    RedirectUris = table.Column<string>(type: "text", nullable: false),
                    Scopes = table.Column<string>(type: "text", nullable: false),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    WebsiteUrl = table.Column<string>(type: "text", nullable: true),
                    PrivacyPolicyUrl = table.Column<string>(type: "text", nullable: true),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RateLimitPerMinute = table.Column<int>(type: "integer", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthApps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecentSearches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    QueryString = table.Column<string>(type: "text", nullable: false),
                    SearchType = table.Column<string>(type: "text", nullable: false),
                    ResultCount = table.Column<int>(type: "integer", nullable: false),
                    SearchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecentSearches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedSearchFilters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SearchType = table.Column<string>(type: "text", nullable: false),
                    QueryString = table.Column<string>(type: "text", nullable: false),
                    FiltersJson = table.Column<string>(type: "text", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedSearchFilters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SplitPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SplitType = table.Column<string>(type: "text", nullable: false),
                    InstallmentSchedule = table.Column<string>(type: "text", nullable: true),
                    DepositAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    DepositPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    StripePaymentIntentId = table.Column<string>(type: "text", nullable: true),
                    DepositPaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FullyPaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SplitPayments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CreditAccountTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "numeric", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ReferenceType = table.Column<string>(type: "text", nullable: true),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    PerformedById = table.Column<Guid>(type: "uuid", nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditAccountTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditAccountTransactions_CreditAccounts_CreditAccountId",
                        column: x => x.CreditAccountId,
                        principalTable: "CreditAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OAuthTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OAuthAppId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenType = table.Column<string>(type: "text", nullable: false),
                    AccessTokenHash = table.Column<string>(type: "text", nullable: false),
                    RefreshTokenHash = table.Column<string>(type: "text", nullable: true),
                    AuthorizationCode = table.Column<string>(type: "text", nullable: true),
                    Scopes = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_OAuthTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OAuthTokens_OAuthApps_OAuthAppId",
                        column: x => x.OAuthAppId,
                        principalTable: "OAuthApps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditAccountTransactions_CreditAccountId",
                table: "CreditAccountTransactions",
                column: "CreditAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_OAuthTokens_OAuthAppId",
                table: "OAuthTokens",
                column: "OAuthAppId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditAccountTransactions");

            migrationBuilder.DropTable(
                name: "OAuthTokens");

            migrationBuilder.DropTable(
                name: "RecentSearches");

            migrationBuilder.DropTable(
                name: "SavedSearchFilters");

            migrationBuilder.DropTable(
                name: "SplitPayments");

            migrationBuilder.DropTable(
                name: "CreditAccounts");

            migrationBuilder.DropTable(
                name: "OAuthApps");

            migrationBuilder.DropColumn(
                name: "LeadScore",
                table: "Clients");
        }
    }
}
