using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProctorIQ.Migrations
{
    /// <inheritdoc />
    public partial class AddExamSoftDeleteColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "Exams",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Exams",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Exams");
        }
    }
}
