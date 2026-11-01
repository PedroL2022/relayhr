using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RelayHR.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddFreelancerTables_HU06 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FreelancerCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FreelancerCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FreelancerProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    ProfessionalTitle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Experience = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "DOP"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FreelancerProfiles", x => x.Id);
                    table.CheckConstraint("CK_FreelancerProfile_HourlyRate", "[HourlyRate] >= 0");
                    table.ForeignKey(
                        name: "FK_FreelancerProfiles_FreelancerCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "FreelancerCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FreelancerProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FreelancerAvailabilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FreelancerProfileId = table.Column<int>(type: "int", nullable: false),
                    AvailableFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    AvailableUntil = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FreelancerAvailabilities", x => x.Id);
                    table.CheckConstraint("CK_FreelancerAvailability_ValidInterval", "[AvailableUntil] >= [AvailableFrom]");
                    table.ForeignKey(
                        name: "FK_FreelancerAvailabilities_FreelancerProfiles_FreelancerProfileId",
                        column: x => x.FreelancerProfileId,
                        principalTable: "FreelancerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FreelancerPortfolios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FreelancerProfileId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ProjectUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FreelancerPortfolios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FreelancerPortfolios_FreelancerProfiles_FreelancerProfileId",
                        column: x => x.FreelancerProfileId,
                        principalTable: "FreelancerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "FreelancerCategories",
                columns: new[] { "Id", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, true, "Diseño UI/UX" },
                    { 2, true, "Contabilidad y Finanzas" },
                    { 3, true, "Desarrollo de software" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "FullName", "PasswordHash" },
                values: new object[,]
                {
                    { 1, "carlos@ejemplo.com", "Carlos Pérez", "HASH_SECRETO_1" },
                    { 2, "laura@ejemplo.com", "Laura Gómez", "HASH_SECRETO_2" },
                    { 3, "marcos@ejemplo.com", "Marcos Díaz", "HASH_SECRETO_3" }
                });

            migrationBuilder.InsertData(
                table: "FreelancerProfiles",
                columns: new[] { "Id", "CategoryId", "Currency", "Experience", "HourlyRate", "IsActive", "ProfessionalTitle", "UserId" },
                values: new object[,]
                {
                    { 25, 3, "DOP", "4 años desarrollando APIs REST con .NET.", 1000m, true, "Desarrollador Backend", 1 },
                    { 26, 1, "DOP", "5 años diseñando interfaces web y móviles en Figma.", 1200m, true, "Diseñadora UX/UI Senior", 2 },
                    { 27, 3, "DOP", "7 años en microservicios y Azure.", 2000m, false, "Arquitecto Cloud .NET", 3 }
                });

            migrationBuilder.InsertData(
                table: "FreelancerAvailabilities",
                columns: new[] { "Id", "AvailableFrom", "AvailableUntil", "FreelancerProfileId" },
                values: new object[,]
                {
                    { 1, new DateOnly(2026, 11, 10), new DateOnly(2026, 11, 30), 25 },
                    { 2, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 15), 26 }
                });

            migrationBuilder.InsertData(
                table: "FreelancerPortfolios",
                columns: new[] { "Id", "Description", "FreelancerProfileId", "ProjectUrl", "Title" },
                values: new object[] { 1, "Desarrollo de una API para gestión de inventarios.", 25, "https://ejemplo.com/proyecto", "Sistema de inventario" });

            migrationBuilder.CreateIndex(
                name: "IX_FreelancerAvailability_Dates",
                table: "FreelancerAvailabilities",
                columns: new[] { "FreelancerProfileId", "AvailableFrom", "AvailableUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_FreelancerPortfolios_FreelancerProfileId",
                table: "FreelancerPortfolios",
                column: "FreelancerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_FreelancerProfile_Search",
                table: "FreelancerProfiles",
                columns: new[] { "IsActive", "CategoryId", "HourlyRate" });

            migrationBuilder.CreateIndex(
                name: "IX_FreelancerProfiles_CategoryId",
                table: "FreelancerProfiles",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FreelancerProfiles_UserId",
                table: "FreelancerProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FreelancerAvailabilities");

            migrationBuilder.DropTable(
                name: "FreelancerPortfolios");

            migrationBuilder.DropTable(
                name: "FreelancerProfiles");

            migrationBuilder.DropTable(
                name: "FreelancerCategories");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
