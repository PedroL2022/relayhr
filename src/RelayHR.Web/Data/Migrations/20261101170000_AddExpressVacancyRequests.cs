using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RelayHR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExpressVacancyRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                        principalColumn: "EmployeeExitId",
                        onDelete: ReferentialAction.Restrict);
                });

            // Una salida solo puede tener una solicitud de generación.
            migrationBuilder.CreateIndex(
                name: "IX_ExpressVacancyRequests_EmployeeExitId",
                table: "ExpressVacancyRequests",
                column: "EmployeeExitId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpressVacancyRequests_Status_NextAttemptAt",
                table: "ExpressVacancyRequests",
                columns: new[] { "Status", "NextAttemptAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ExpressVacancyRequests");
        }
    }
}
