using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSeededMarketplaceListings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "BusinessListings",
                columns: new[] { "Id", "AverageRating", "BusinessName", "Category", "City", "Country", "CreatedAt", "DeletedAt", "DeletedBy", "Description", "IsActive", "IsDeleted", "IsFeatured", "IsVerified", "LogoUrl", "OperatingHours", "Phone", "PremiumScore", "ReviewCount", "ServiceTags", "Slug", "State", "TenantId", "UpdatedAt", "Version", "Website", "ZipCode" },
                values: new object[,]
                {
                    { new Guid("a1b2c3d4-e5f6-4a5b-6c7d-8e9f0a1b2c3d"), 4.9000000000000004, "Luxe Hair Studio", "Beauty", "New York", "US", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Premium hair styling and coloring in the heart of Manhattan. Our expert stylists have over 15 years of experience.", true, false, true, false, null, null, null, 95.5, 128, "haircut,color,balayage,styling", "luxe-hair-studio-nyc", "NY", new Guid("0192a3b4-c5d6-4e5f-8901-234567890123"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null },
                    { new Guid("b2c3d4e5-f6a7-5b6c-7d8e-9f0a1b2c3d4e"), 4.7999999999999998, "Zen Garden Spa", "Wellness", "San Francisco", "US", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "A tranquil escape in San Francisco. Massage therapy, organic facials, and holistic wellness treatments.", true, false, true, false, null, null, null, 92.0, 215, "massage,facial,wellness,spa", "zen-garden-spa-sf", "CA", new Guid("12345678-1234-1234-1234-123456781234"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null },
                    { new Guid("c3d4e5f6-a7b8-6c7d-8e9f-0a1b2c3d4e5f"), 4.7000000000000002, "Iron Forge Gym", "Fitness", "Austin", "US", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Top-tier strength and conditioning facility in Austin. Personal training and group classes available.", true, false, true, false, null, null, null, 88.5, 89, "gym,training,fitness,weights", "iron-forge-gym-austin", "TX", new Guid("87654321-4321-4321-4321-876543214321"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, null, null }
                });
        }
    }
}
