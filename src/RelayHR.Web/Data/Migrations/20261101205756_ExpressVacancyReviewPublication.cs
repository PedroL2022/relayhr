using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RelayHR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExpressVacancyReviewPublication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "Employees",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PositionId",
                table: "Employees",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExpressVacancyRequests",
                columns: table => new
                {
                    ExpressVacancyRequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeExitId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessingMilliseconds = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpressVacancyRequests", x => x.ExpressVacancyRequestId);
                    table.CheckConstraint("CK_ExpressVacancyRequests_Status", "[Status] IN ('Pending', 'Processing', 'Completed', 'Failed')");
                    table.ForeignKey(
                        name: "FK_ExpressVacancyRequests_EmployeeExits_EmployeeExitId",
                        column: x => x.EmployeeExitId,
                        principalTable: "EmployeeExits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    PositionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Requirements = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.PositionId);
                    table.ForeignKey(
                        name: "FK_Positions_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpressVacancies",
                columns: table => new
                {
                    ExpressVacancyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeExitId = table.Column<int>(type: "int", nullable: false),
                    PositionId = table.Column<int>(type: "int", nullable: true),
                    JobTitle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    JobDescription = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Requirements = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "PendingReview"),
                    MissingFields = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedByUserId = table.Column<int>(type: "int", nullable: true),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpressVacancies", x => x.ExpressVacancyId);
                    table.CheckConstraint("CK_ExpressVacancies_Status", "[Status] IN ('PendingReview', 'InReview', 'Approved', 'Discarded', 'Published')");
                    table.ForeignKey(
                        name: "FK_ExpressVacancies_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpressVacancies_EmployeeExits_EmployeeExitId",
                        column: x => x.EmployeeExitId,
                        principalTable: "EmployeeExits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpressVacancies_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "PositionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpressVacancies_Users_PublishedByUserId",
                        column: x => x.PublishedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpressVacancies_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DepartmentId",
                table: "Employees",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_PositionId",
                table: "Employees",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpressVacancies_DepartmentId",
                table: "ExpressVacancies",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpressVacancies_EmployeeExitId",
                table: "ExpressVacancies",
                column: "EmployeeExitId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpressVacancies_PositionId",
                table: "ExpressVacancies",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpressVacancies_PublishedByUserId",
                table: "ExpressVacancies",
                column: "PublishedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpressVacancies_UpdatedByUserId",
                table: "ExpressVacancies",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpressVacancyRequests_EmployeeExitId",
                table: "ExpressVacancyRequests",
                column: "EmployeeExitId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpressVacancyRequests_Status_NextAttemptAt",
                table: "ExpressVacancyRequests",
                columns: new[] { "Status", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Positions_DepartmentId",
                table: "Positions",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Departments_DepartmentId",
                table: "Employees",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Positions_PositionId",
                table: "Employees",
                column: "PositionId",
                principalTable: "Positions",
                principalColumn: "PositionId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Departments_DepartmentId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Positions_PositionId",
                table: "Employees");

            migrationBuilder.DropTable(
                name: "ExpressVacancies");

            migrationBuilder.DropTable(
                name: "ExpressVacancyRequests");

            migrationBuilder.DropTable(
                name: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_Employees_DepartmentId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_PositionId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "PositionId",
                table: "Employees");
        }
    }
}
