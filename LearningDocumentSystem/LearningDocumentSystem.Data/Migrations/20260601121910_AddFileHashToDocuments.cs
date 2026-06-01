using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningDocumentSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFileHashToDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileHash",
                table: "Documents",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_FileHash",
                table: "Documents",
                column: "FileHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_FileHash",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "FileHash",
                table: "Documents");
        }
    }
}
