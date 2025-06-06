using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BugTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create WorkflowDefinitions table
            migrationBuilder.CreateTable(
                name: "WorkflowDefinitions",
                columns: table => new
                {
                    WorkflowDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Version = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DefinitionJson = table.Column<string>(type: "ntext", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowDefinitions", x => x.WorkflowDefinitionId);
                });

            // Create WorkflowExecutions table
            migrationBuilder.CreateTable(
                name: "WorkflowExecutions",
                columns: table => new
                {
                    WorkflowExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentStepId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContextJson = table.Column<string>(type: "ntext", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowExecutions", x => x.WorkflowExecutionId);
                    table.ForeignKey(
                        name: "FK_WorkflowExecutions_CustomTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "CustomTasks",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkflowExecutions_WorkflowDefinitions_WorkflowDefinitionId",
                        column: x => x.WorkflowDefinitionId,
                        principalTable: "WorkflowDefinitions",
                        principalColumn: "WorkflowDefinitionId",
                        onDelete: ReferentialAction.Restrict);
                });

            // Create WorkflowAuditLogs table
            migrationBuilder.CreateTable(
                name: "WorkflowAuditLogs",
                columns: table => new
                {
                    WorkflowAuditLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Result = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PreviousStepId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NextStepId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Decision = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "ntext", nullable: true),
                    ConditionsEvaluated = table.Column<string>(type: "ntext", nullable: true),
                    ContextSnapshot = table.Column<string>(type: "ntext", nullable: true),
                    PerformedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowAuditLogs", x => x.WorkflowAuditLogId);
                    table.ForeignKey(
                        name: "FK_WorkflowAuditLogs_WorkflowExecutions_WorkflowExecutionId",
                        column: x => x.WorkflowExecutionId,
                        principalTable: "WorkflowExecutions",
                        principalColumn: "WorkflowExecutionId",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinitions_Name_Version",
                table: "WorkflowDefinitions",
                columns: new[] { "Name", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutions_TaskId",
                table: "WorkflowExecutions",
                column: "TaskId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutions_WorkflowDefinitionId",
                table: "WorkflowExecutions",
                column: "WorkflowDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowAuditLogs_WorkflowExecutionId_Timestamp",
                table: "WorkflowAuditLogs",
                columns: new[] { "WorkflowExecutionId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowAuditLogs");

            migrationBuilder.DropTable(
                name: "WorkflowExecutions");

            migrationBuilder.DropTable(
                name: "WorkflowDefinitions");
        }
    }
}