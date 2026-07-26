using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Equipment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: true),
                    SerialNumber = table.Column<string>(type: "text", nullable: true),
                    Model = table.Column<string>(type: "text", nullable: true),
                    Manufacturer = table.Column<string>(type: "text", nullable: true),
                    PurchaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PurchasePrice = table.Column<decimal>(type: "numeric", nullable: true),
                    WarrantyExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: true),
                    AssignedToStaffId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastMaintenanceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextMaintenanceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Equipment_StaffMember_AssignedToStaffId",
                        column: x => x.AssignedToStaffId,
                        principalTable: "StaffMember",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EquipmentMaintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Cost = table.Column<decimal>(type: "numeric", nullable: true),
                    PerformedBy = table.Column<string>(type: "text", nullable: true),
                    NextDueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentMaintenance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentMaintenance_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_AssignedToStaffId",
                table: "Equipment",
                column: "AssignedToStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentMaintenance_EquipmentId",
                table: "EquipmentMaintenance",
                column: "EquipmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EquipmentMaintenance");

            migrationBuilder.DropTable(
                name: "Equipment");
        }
    }
}
