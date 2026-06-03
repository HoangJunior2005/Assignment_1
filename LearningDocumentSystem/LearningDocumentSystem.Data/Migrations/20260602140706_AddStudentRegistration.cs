using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningDocumentSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SchoolID",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Schools",
                columns: table => new
                {
                    SchoolID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SchoolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schools", x => x.SchoolID);
                });

            migrationBuilder.CreateTable(
                name: "StudentRegistries",
                columns: table => new
                {
                    RegistryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SchoolID = table.Column<int>(type: "int", nullable: false),
                    IsActivated = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ActivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentRegistries", x => x.RegistryID);
                    table.ForeignKey(
                        name: "FK_StudentRegistries_Schools_SchoolID",
                        column: x => x.SchoolID,
                        principalTable: "Schools",
                        principalColumn: "SchoolID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentRegistries_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_SchoolID",
                table: "Users",
                column: "SchoolID");

            migrationBuilder.CreateIndex(
                name: "IX_Schools_SchoolCode",
                table: "Schools",
                column: "SchoolCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentRegistries_SchoolID",
                table: "StudentRegistries",
                column: "SchoolID");

            migrationBuilder.CreateIndex(
                name: "IX_StudentRegistries_StudentCode",
                table: "StudentRegistries",
                column: "StudentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentRegistries_UserID",
                table: "StudentRegistries",
                column: "UserID",
                unique: true,
                filter: "[UserID] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Schools_SchoolID",
                table: "Users",
                column: "SchoolID",
                principalTable: "Schools",
                principalColumn: "SchoolID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Schools_SchoolID",
                table: "Users");

            migrationBuilder.DropTable(
                name: "StudentRegistries");

            migrationBuilder.DropTable(
                name: "Schools");

            migrationBuilder.DropIndex(
                name: "IX_Users_SchoolID",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SchoolID",
                table: "Users");
        }
    }
}
