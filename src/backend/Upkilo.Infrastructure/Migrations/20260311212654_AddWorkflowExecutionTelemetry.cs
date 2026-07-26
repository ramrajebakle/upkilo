using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowExecutionTelemetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "workflow_executions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    current_step_index = table.Column<int>(type: "integer", nullable: false),
                    trigger_event_data = table.Column<string>(type: "jsonb", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_executions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_executions_workflows_workflow_id",
                        column: x => x.workflow_id,
                        principalTable: "workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_execution_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_execution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_index = table.Column<int>(type: "integer", nullable: false),
                    step_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    action_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    executed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    duration_ms = table.Column<int>(type: "integer", nullable: false),
                    message = table.Column<string>(type: "text", nullable: true),
                    error_details = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_execution_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_execution_logs_workflow_executions_workflow_execut~",
                        column: x => x.workflow_execution_id,
                        principalTable: "workflow_executions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_execution_logs_workflow_execution_id",
                table: "workflow_execution_logs",
                column: "workflow_execution_id");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_executions_workflow_id",
                table: "workflow_executions",
                column: "workflow_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workflow_execution_logs");

            migrationBuilder.DropTable(
                name: "workflow_executions");
        }
    }
}
