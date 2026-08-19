using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerServiceRefundPolicy : Migration
    {
        /// <inheritdoc />
        // defaultValue is 18 / 12 / 50, NOT the 0 that EF scaffolds from a non-nullable numeric.
        //
        // This matters more than a tidy default. AddColumn's defaultValue is what every EXISTING
        // row is backfilled with, and PublicBookingController.CanCancel refunds in full whenever
        // the hours remaining are greater than or equal to FullRefundHours. Shipping 0 would
        // therefore have set every service already in the database to "refund 100% of the
        // deposit on any cancellation, however late" — silently, on deploy, for every tenant.
        //
        // These three values are the policy agreed for the default: full refund beyond 18 hours,
        // 50% between 12 and 18 hours, nothing inside 12. They must stay in step with the
        // property initialisers on Service.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FullRefundHours",
                table: "Services",
                type: "integer",
                nullable: false,
                defaultValue: 18);

            migrationBuilder.AddColumn<int>(
                name: "PartialRefundHours",
                table: "Services",
                type: "integer",
                nullable: false,
                defaultValue: 12);

            migrationBuilder.AddColumn<decimal>(
                name: "PartialRefundPercent",
                table: "Services",
                type: "numeric",
                nullable: false,
                defaultValue: 50m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FullRefundHours",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "PartialRefundHours",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "PartialRefundPercent",
                table: "Services");
        }
    }
}
