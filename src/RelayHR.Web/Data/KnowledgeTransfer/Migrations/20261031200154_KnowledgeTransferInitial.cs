using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RelayHR.Web.Data.KnowledgeTransfer.Migrations
{
    /// <inheritdoc />
    public partial class KnowledgeTransferInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeExits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeExits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeTransferProjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeExitId = table.Column<int>(type: "int", nullable: false),
                    DepartingEmployeeId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeTransferProjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeTransferProjects_EmployeeExits_EmployeeExitId",
                        column: x => x.EmployeeExitId,
                        principalTable: "EmployeeExits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeTransferTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeExitId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    DepartingEmployeeId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeTransferTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeTransferTasks_EmployeeExits_EmployeeExitId",
                        column: x => x.EmployeeExitId,
                        principalTable: "EmployeeExits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KnowledgeTransferTasks_KnowledgeTransferProjects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "KnowledgeTransferProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeTransferProjects_EmployeeExitId",
                table: "KnowledgeTransferProjects",
                column: "EmployeeExitId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeTransferTasks_EmployeeExitId",
                table: "KnowledgeTransferTasks",
                column: "EmployeeExitId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeTransferTasks_ProjectId",
                table: "KnowledgeTransferTasks",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KnowledgeTransferTasks");

            migrationBuilder.DropTable(
                name: "KnowledgeTransferProjects");

            migrationBuilder.DropTable(
                name: "EmployeeExits");
        }
    }
}
