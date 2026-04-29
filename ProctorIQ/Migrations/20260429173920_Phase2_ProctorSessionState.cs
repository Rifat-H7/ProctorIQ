using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProctorIQ.Migrations
{
    /// <inheritdoc />
    public partial class Phase2_ProctorSessionState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentConnectionId",
                table: "ExamAttempts",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenAtUtc",
                table: "ExamAttempts",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionStatus",
                table: "ExamAttempts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WarningCount",
                table: "ExamAttempts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempts_ExamId_SessionStatus",
                table: "ExamAttempts",
                columns: new[] { "ExamId", "SessionStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempts_LastSeenAtUtc",
                table: "ExamAttempts",
                column: "LastSeenAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExamAttempts_ExamId_SessionStatus",
                table: "ExamAttempts");

            migrationBuilder.DropIndex(
                name: "IX_ExamAttempts_LastSeenAtUtc",
                table: "ExamAttempts");

            migrationBuilder.DropColumn(
                name: "CurrentConnectionId",
                table: "ExamAttempts");

            migrationBuilder.DropColumn(
                name: "LastSeenAtUtc",
                table: "ExamAttempts");

            migrationBuilder.DropColumn(
                name: "SessionStatus",
                table: "ExamAttempts");

            migrationBuilder.DropColumn(
                name: "WarningCount",
                table: "ExamAttempts");
        }
    }
}
