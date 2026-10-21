using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TaskTracker.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DueDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProjectId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tasks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Projects",
                columns: new[] { "Id", "CreatedAtUtc", "Description", "Name" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 10, 1, 9, 0, 0, 0, DateTimeKind.Utc), "Refresh the public marketing website.", "Website Redesign" },
                    { 2, new DateTime(2026, 10, 1, 9, 0, 0, 0, DateTimeKind.Utc), "Prepare the first public release of the mobile app.", "Mobile App Launch" }
                });

            migrationBuilder.InsertData(
                table: "Tasks",
                columns: new[] { "Id", "CreatedAtUtc", "Description", "DueDateUtc", "ProjectId", "Status", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 10, 1, 9, 0, 0, 0, DateTimeKind.Utc), "Low-fidelity wireframes for the home and pricing pages.", new DateTime(2026, 10, 10, 0, 0, 0, 0, DateTimeKind.Utc), 1, "Done", "Create wireframes", null },
                    { 2, new DateTime(2026, 10, 1, 9, 0, 0, 0, DateTimeKind.Utc), "Implement the approved design with mobile breakpoints.", new DateTime(2026, 10, 31, 0, 0, 0, 0, DateTimeKind.Utc), 1, "InProgress", "Build responsive layout", null },
                    { 3, new DateTime(2026, 10, 1, 9, 0, 0, 0, DateTimeKind.Utc), null, null, 1, "Todo", "Run accessibility audit", null },
                    { 4, new DateTime(2026, 10, 1, 9, 0, 0, 0, DateTimeKind.Utc), "Screenshots, descriptions and privacy details for both stores.", new DateTime(2026, 11, 15, 0, 0, 0, 0, DateTimeKind.Utc), 2, "Todo", "Set up app store listings", null },
                    { 5, new DateTime(2026, 10, 1, 9, 0, 0, 0, DateTimeKind.Utc), null, null, 2, "InProgress", "Beta test with internal users", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ProjectId_Status",
                table: "Tasks",
                columns: new[] { "ProjectId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tasks");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
