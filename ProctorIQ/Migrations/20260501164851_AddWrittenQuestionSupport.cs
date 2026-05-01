using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProctorIQ.Migrations
{
    /// <inheritdoc />
    public partial class AddWrittenQuestionSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelectedTextAnswer",
                table: "CandidateAnswers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedTextAnswer",
                table: "CandidateAnswers");
        }
    }
}
