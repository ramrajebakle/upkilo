using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJsonbIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Users_Preferences",
                table: "Users",
                column: "Preferences")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Metadata",
                table: "Tenants",
                column: "Metadata")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Settings",
                table: "Tenants",
                column: "Settings")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Preferences",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_Metadata",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_Settings",
                table: "Tenants");
        }
    }
}
