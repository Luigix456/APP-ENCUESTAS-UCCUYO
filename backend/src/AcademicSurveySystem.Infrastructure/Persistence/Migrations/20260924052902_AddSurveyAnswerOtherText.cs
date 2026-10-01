using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademicSurveySystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveyAnswerOtherText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "other_text",
                table: "survey_answers",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "other_text",
                table: "survey_answers");
        }
    }
}
