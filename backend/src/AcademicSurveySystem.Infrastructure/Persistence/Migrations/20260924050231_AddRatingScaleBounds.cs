using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademicSurveySystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRatingScaleBounds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "rating_max",
                table: "survey_questions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "rating_min",
                table: "survey_questions",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "rating_max",
                table: "survey_questions");

            migrationBuilder.DropColumn(
                name: "rating_min",
                table: "survey_questions");
        }
    }
}
