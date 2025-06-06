using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BugTracker.Migrations
{
    /// <inheritdoc />
    public partial class ssS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "WorkflowDefinitions");

            migrationBuilder.AlterColumn<string>(
                name: "ContextJson",
                table: "WorkflowExecutions",
                type: "ntext",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "ntext",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "WorkflowExecutions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "WorkflowDefinitions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "WorkflowDefinitions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DurationMs",
                table: "WorkflowAuditLogs",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "WorkflowExecutions");

            migrationBuilder.DropColumn(
                name: "DurationMs",
                table: "WorkflowAuditLogs");

            migrationBuilder.AlterColumn<string>(
                name: "ContextJson",
                table: "WorkflowExecutions",
                type: "ntext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "ntext");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "WorkflowDefinitions",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "WorkflowDefinitions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "WorkflowDefinitions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
