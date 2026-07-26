using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipStripeIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnforceTwoFactor",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "SupportTickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "SupportTickets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAt",
                table: "SupportTickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SlaExpiresAt",
                table: "SupportTickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripePriceId",
                table: "MembershipPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeProductId",
                table: "MembershipPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundedAmount",
                table: "Invoices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedAt",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                table: "ClientMemberships",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeSubscriptionId",
                table: "ClientMemberships",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                table: "Bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerId",
                table: "Bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "Bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerPhone",
                table: "Bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RescheduleCount",
                table: "Bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ServiceName",
                table: "Bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StaffName",
                table: "Bookings",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SupportTicketComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsInternal = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTicketComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportTicketComments_SupportTickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "SupportTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicketComments_TicketId",
                table: "SupportTicketComments",
                column: "TicketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupportTicketComments");

            migrationBuilder.DropColumn(
                name: "EnforceTwoFactor",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "SlaExpiresAt",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "StripePriceId",
                table: "MembershipPlans");

            migrationBuilder.DropColumn(
                name: "StripeProductId",
                table: "MembershipPlans");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RefundedAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RefundedAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "ClientMemberships");

            migrationBuilder.DropColumn(
                name: "StripeSubscriptionId",
                table: "ClientMemberships");

            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CustomerPhone",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RescheduleCount",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ServiceName",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "StaffName",
                table: "Bookings");
        }
    }
}
