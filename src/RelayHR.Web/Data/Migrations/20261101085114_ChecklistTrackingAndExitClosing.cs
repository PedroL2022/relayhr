using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RelayHR.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChecklistTrackingAndExitClosing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_EmployeeExits_Status",
                table: "EmployeeExits");

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "EmployeeExits",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClosedByUserId",
                table: "EmployeeExits",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "ChecklistTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompletedByUserId",
                table: "ChecklistTasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRequired",
                table: "ChecklistTasks",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "ChecklistTasks",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeExits_ClosedByUserId",
                table: "EmployeeExits",
                column: "ClosedByUserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmployeeExits_Status",
                table: "EmployeeExits",
                sql: "[Status] IN (N'Pending', N'InProgress', N'Completed', N'Cancelled', N'Closed')");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistTasks_CompletedByUserId",
                table: "ChecklistTasks",
                column: "CompletedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChecklistTasks_Users_CompletedByUserId",
                table: "ChecklistTasks",
                column: "CompletedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeExits_Users_ClosedByUserId",
                table: "EmployeeExits",
                column: "ClosedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChecklistTasks_Users_CompletedByUserId",
                table: "ChecklistTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeExits_Users_ClosedByUserId",
                table: "EmployeeExits");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeExits_ClosedByUserId",
                table: "EmployeeExits");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmployeeExits_Status",
                table: "EmployeeExits");

            migrationBuilder.DropIndex(
                name: "IX_ChecklistTasks_CompletedByUserId",
                table: "ChecklistTasks");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "EmployeeExits");

            migrationBuilder.DropColumn(
                name: "ClosedByUserId",
                table: "EmployeeExits");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "ChecklistTasks");

            migrationBuilder.DropColumn(
                name: "CompletedByUserId",
                table: "ChecklistTasks");

            migrationBuilder.DropColumn(
                name: "IsRequired",
                table: "ChecklistTasks");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "ChecklistTasks");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmployeeExits_Status",
                table: "EmployeeExits",
                sql: "[Status] IN (N'Pending', N'InProgress', N'Completed', N'Cancelled')");
        }
    }
}
