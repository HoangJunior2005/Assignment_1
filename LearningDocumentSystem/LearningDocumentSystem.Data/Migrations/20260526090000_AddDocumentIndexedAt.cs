using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningDocumentSystem.Data.Migrations
{
    public partial class AddDocumentIndexedAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "IndexedAt",
                table: "Documents",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IndexedAt",
                table: "Documents");
        }
    }
}
