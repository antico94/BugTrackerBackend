using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BugTracker.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.ClientId);
                });

            migrationBuilder.CreateTable(
                name: "CoreBugs",
                columns: table => new
                {
                    BugId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BugTitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    JiraKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    JiraLink = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BugDescription = table.Column<string>(type: "ntext", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FoundInBuild = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AffectedVersions = table.Column<string>(type: "ntext", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssessedProductType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssessedImpactedVersions = table.Column<string>(type: "ntext", nullable: false),
                    IsAssessed = table.Column<bool>(type: "bit", nullable: false),
                    AssessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssessedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoreBugs", x => x.BugId);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyCoreBugs",
                columns: table => new
                {
                    WeeklyCoreBugsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    WeekStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WeekEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyCoreBugs", x => x.WeeklyCoreBugsId);
                });

            migrationBuilder.CreateTable(
                name: "TrialManagers",
                columns: table => new
                {
                    TrialManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    JiraKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    JiraLink = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WebLink = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Protocol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrialManagers", x => x.TrialManagerId);
                    table.ForeignKey(
                        name: "FK_TrialManagers_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "ClientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyCoreBugEntries",
                columns: table => new
                {
                    WeeklyCoreBugEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WeeklyCoreBugsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BugId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyCoreBugEntries", x => x.WeeklyCoreBugEntryId);
                    table.ForeignKey(
                        name: "FK_WeeklyCoreBugEntries_CoreBugs_BugId",
                        column: x => x.BugId,
                        principalTable: "CoreBugs",
                        principalColumn: "BugId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WeeklyCoreBugEntries_WeeklyCoreBugs_WeeklyCoreBugsId",
                        column: x => x.WeeklyCoreBugsId,
                        principalTable: "WeeklyCoreBugs",
                        principalColumn: "WeeklyCoreBugsId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Studies",
                columns: table => new
                {
                    StudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Protocol = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrialManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Studies", x => x.StudyId);
                    table.ForeignKey(
                        name: "FK_Studies_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "ClientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Studies_TrialManagers_TrialManagerId",
                        column: x => x.TrialManagerId,
                        principalTable: "TrialManagers",
                        principalColumn: "TrialManagerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InteractiveResponseTechnologies",
                columns: table => new
                {
                    InteractiveResponseTechnologyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    JiraKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    JiraLink = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WebLink = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Protocol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrialManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InteractiveResponseTechnologies", x => x.InteractiveResponseTechnologyId);
                    table.ForeignKey(
                        name: "FK_InteractiveResponseTechnologies_Studies_StudyId",
                        column: x => x.StudyId,
                        principalTable: "Studies",
                        principalColumn: "StudyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InteractiveResponseTechnologies_TrialManagers_TrialManagerId",
                        column: x => x.TrialManagerId,
                        principalTable: "TrialManagers",
                        principalColumn: "TrialManagerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomTasks",
                columns: table => new
                {
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskTitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TaskDescription = table.Column<string>(type: "ntext", nullable: false),
                    JiraTaskKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    JiraTaskLink = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BugId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TrialManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InteractiveResponseTechnologyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomTasks", x => x.TaskId);
                    table.CheckConstraint("CK_Task_Product", "(TrialManagerId IS NOT NULL AND InteractiveResponseTechnologyId IS NULL) OR (TrialManagerId IS NULL AND InteractiveResponseTechnologyId IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_CustomTasks_CoreBugs_BugId",
                        column: x => x.BugId,
                        principalTable: "CoreBugs",
                        principalColumn: "BugId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomTasks_InteractiveResponseTechnologies_InteractiveResponseTechnologyId",
                        column: x => x.InteractiveResponseTechnologyId,
                        principalTable: "InteractiveResponseTechnologies",
                        principalColumn: "InteractiveResponseTechnologyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomTasks_Studies_StudyId",
                        column: x => x.StudyId,
                        principalTable: "Studies",
                        principalColumn: "StudyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomTasks_TrialManagers_TrialManagerId",
                        column: x => x.TrialManagerId,
                        principalTable: "TrialManagers",
                        principalColumn: "TrialManagerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExternalModules",
                columns: table => new
                {
                    ExternalModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExternalModuleType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InteractiveResponseTechnologyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalModules", x => x.ExternalModuleId);
                    table.ForeignKey(
                        name: "FK_ExternalModules_InteractiveResponseTechnologies_InteractiveResponseTechnologyId",
                        column: x => x.InteractiveResponseTechnologyId,
                        principalTable: "InteractiveResponseTechnologies",
                        principalColumn: "InteractiveResponseTechnologyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskNotes",
                columns: table => new
                {
                    TaskNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "ntext", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskNotes", x => x.TaskNoteId);
                    table.ForeignKey(
                        name: "FK_TaskNotes_CustomTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "CustomTasks",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskSteps",
                columns: table => new
                {
                    TaskStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "ntext", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    IsDecision = table.Column<bool>(type: "bit", nullable: false),
                    IsAutoCheck = table.Column<bool>(type: "bit", nullable: false),
                    IsTerminal = table.Column<bool>(type: "bit", nullable: false),
                    RequiresNote = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecisionAnswer = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Notes = table.Column<string>(type: "ntext", nullable: false),
                    AutoCheckResult = table.Column<bool>(type: "bit", nullable: true),
                    NextStepIfYes = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NextStepIfNo = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NextStepIfTrue = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NextStepIfFalse = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskSteps", x => x.TaskStepId);
                    table.ForeignKey(
                        name: "FK_TaskSteps_CustomTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "CustomTasks",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoreBugs_JiraKey",
                table: "CoreBugs",
                column: "JiraKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomTasks_BugId",
                table: "CustomTasks",
                column: "BugId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomTasks_InteractiveResponseTechnologyId",
                table: "CustomTasks",
                column: "InteractiveResponseTechnologyId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomTasks_StudyId",
                table: "CustomTasks",
                column: "StudyId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomTasks_TrialManagerId",
                table: "CustomTasks",
                column: "TrialManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalModules_InteractiveResponseTechnologyId",
                table: "ExternalModules",
                column: "InteractiveResponseTechnologyId");

            migrationBuilder.CreateIndex(
                name: "IX_InteractiveResponseTechnologies_StudyId",
                table: "InteractiveResponseTechnologies",
                column: "StudyId");

            migrationBuilder.CreateIndex(
                name: "IX_InteractiveResponseTechnologies_TrialManagerId",
                table: "InteractiveResponseTechnologies",
                column: "TrialManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Studies_ClientId",
                table: "Studies",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Studies_TrialManagerId",
                table: "Studies",
                column: "TrialManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskNotes_TaskId",
                table: "TaskNotes",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskSteps_TaskId",
                table: "TaskSteps",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TrialManagers_ClientId",
                table: "TrialManagers",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyCoreBugEntries_BugId",
                table: "WeeklyCoreBugEntries",
                column: "BugId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyCoreBugEntries_WeeklyCoreBugsId_BugId",
                table: "WeeklyCoreBugEntries",
                columns: new[] { "WeeklyCoreBugsId", "BugId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalModules");

            migrationBuilder.DropTable(
                name: "TaskNotes");

            migrationBuilder.DropTable(
                name: "TaskSteps");

            migrationBuilder.DropTable(
                name: "WeeklyCoreBugEntries");

            migrationBuilder.DropTable(
                name: "CustomTasks");

            migrationBuilder.DropTable(
                name: "WeeklyCoreBugs");

            migrationBuilder.DropTable(
                name: "CoreBugs");

            migrationBuilder.DropTable(
                name: "InteractiveResponseTechnologies");

            migrationBuilder.DropTable(
                name: "Studies");

            migrationBuilder.DropTable(
                name: "TrialManagers");

            migrationBuilder.DropTable(
                name: "Clients");
        }
    }
}
