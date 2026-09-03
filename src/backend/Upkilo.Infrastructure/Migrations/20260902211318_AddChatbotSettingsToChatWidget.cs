using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChatbotSettingsToChatWidget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BotName",
                table: "ChatWidgets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HandoffEmail",
                table: "ChatWidgets",
                type: "text",
                nullable: true);

            // The scaffolder also emitted CreateTable("TenantFeatureOverrides") here and it has
            // been removed by hand.
            //
            // 20260830090551_AddTenantFeatureOverrides already creates that table and its
            // filtered unique index, but it was committed without the matching update to
            // AppDbContextModelSnapshot. The snapshot is what the scaffolder diffs against, so
            // it believed the table did not exist and re-created it. Running both migrations
            // would have failed on
            //
            //   42P07: relation "TenantFeatureOverrides" already exists
            //
            // taking the whole deployment's migration step down with it. Regenerating this
            // migration did repair the snapshot, which is why the duplicate is safe to delete:
            // the snapshot now records the table once, from the earlier migration.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BotName",
                table: "ChatWidgets");

            migrationBuilder.DropColumn(
                name: "HandoffEmail",
                table: "ChatWidgets");
        }
    }
}
