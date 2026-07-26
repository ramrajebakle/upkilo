using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovePlaceholderEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MlModelTrainings");

            migrationBuilder.DropTable(
                name: "VisualRegressionTests");

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 14, 21, 51, 545, DateTimeKind.Utc).AddTicks(9430), new DateTime(2026, 6, 22, 14, 21, 51, 545, DateTimeKind.Utc).AddTicks(9434) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 14, 21, 51, 545, DateTimeKind.Utc).AddTicks(9445), new DateTime(2026, 6, 22, 14, 21, 51, 545, DateTimeKind.Utc).AddTicks(9446) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 14, 21, 51, 545, DateTimeKind.Utc).AddTicks(9451), new DateTime(2026, 6, 22, 14, 21, 51, 545, DateTimeKind.Utc).AddTicks(9452) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MlModelTrainings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Accuracy = table.Column<decimal>(type: "numeric", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    Hyperparameters = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModelArtifactUrl = table.Column<string>(type: "text", nullable: true),
                    ModelType = table.Column<string>(type: "text", nullable: false),
                    Precision = table.Column<decimal>(type: "numeric", nullable: true),
                    Recall = table.Column<decimal>(type: "numeric", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainingDataSize = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MlModelTrainings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisualRegressionTests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BaselineScreenshotUrl = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentScreenshotUrl = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    DiffPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    DiffScreenshotUrl = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    PageUrl = table.Column<string>(type: "text", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestName = table.Column<string>(type: "text", nullable: false),
                    TestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisualRegressionTests", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 8, 2, 43, 887, DateTimeKind.Utc).AddTicks(7012), new DateTime(2026, 6, 22, 8, 2, 43, 887, DateTimeKind.Utc).AddTicks(7016) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 8, 2, 43, 887, DateTimeKind.Utc).AddTicks(7033), new DateTime(2026, 6, 22, 8, 2, 43, 887, DateTimeKind.Utc).AddTicks(7033) });

            migrationBuilder.UpdateData(
                table: "BusinessListings",
                keyColumn: "Id",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 22, 8, 2, 43, 887, DateTimeKind.Utc).AddTicks(7039), new DateTime(2026, 6, 22, 8, 2, 43, 887, DateTimeKind.Utc).AddTicks(7039) });
        }
    }
}
