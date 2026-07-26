using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SC5_OutboxDlqFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeadLetteredAt",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeadLetter",
                table: "OutboxMessages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRetryAt",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 19, 40, 51, 217, DateTimeKind.Utc).AddTicks(5010), new DateTime(2026, 6, 22, 19, 40, 51, 217, DateTimeKind.Utc).AddTicks(5014) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 19, 40, 51, 217, DateTimeKind.Utc).AddTicks(5028), new DateTime(2026, 6, 22, 19, 40, 51, 217, DateTimeKind.Utc).AddTicks(5028) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 19, 40, 51, 217, DateTimeKind.Utc).AddTicks(5033), new DateTime(2026, 6, 22, 19, 40, 51, 217, DateTimeKind.Utc).AddTicks(5034) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeadLetteredAt",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "IsDeadLetter",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "NextRetryAt",
                table: "OutboxMessages");

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 19, 3, 11, 305, DateTimeKind.Utc).AddTicks(2467), new DateTime(2026, 6, 22, 19, 3, 11, 305, DateTimeKind.Utc).AddTicks(2471) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 19, 3, 11, 305, DateTimeKind.Utc).AddTicks(2485), new DateTime(2026, 6, 22, 19, 3, 11, 305, DateTimeKind.Utc).AddTicks(2486) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 19, 3, 11, 305, DateTimeKind.Utc).AddTicks(2492), new DateTime(2026, 6, 22, 19, 3, 11, 305, DateTimeKind.Utc).AddTicks(2492) });
        }
    }
}
