using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRebookRemindersEnabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RebookRemindersEnabled",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                // true, NOT the false EF scaffolds for a bool. AddColumn's defaultValue is what
                // backfills every EXISTING tenant, and this column is a pause switch: shipping
                // false would leave every tenant already on the platform permanently paused, so
                // setting a rebooking interval on a service would silently do nothing and there
                // would be no visible reason why. The real opt-in is Service.RebookAfterDays,
                // which is null until a tenant sets it, so defaulting this to true sends nothing
                // on its own. Must match the initialiser on Tenant.RebookRemindersEnabled.
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RebookRemindersEnabled",
                table: "Tenants");
        }
    }
}
