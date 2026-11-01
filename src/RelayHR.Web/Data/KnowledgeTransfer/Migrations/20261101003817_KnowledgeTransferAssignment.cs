using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RelayHR.Web.Data.KnowledgeTransfer.Migrations
{
    /// <inheritdoc />
    public partial class KnowledgeTransferAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAt",
                table: "KnowledgeTransferTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TemporaryResponsibleUserId",
                table: "KnowledgeTransferTasks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeTransferTasks_TemporaryResponsibleUserId",
                table: "KnowledgeTransferTasks",
                column: "TemporaryResponsibleUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_KnowledgeTransferTasks_AppUsers_TemporaryResponsibleUserId",
                table: "KnowledgeTransferTasks",
                column: "TemporaryResponsibleUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KnowledgeTransferTasks_AppUsers_TemporaryResponsibleUserId",
                table: "KnowledgeTransferTasks");

            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeTransferTasks_TemporaryResponsibleUserId",
                table: "KnowledgeTransferTasks");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "KnowledgeTransferTasks");

            migrationBuilder.DropColumn(
                name: "TemporaryResponsibleUserId",
                table: "KnowledgeTransferTasks");
        }
    }
}
