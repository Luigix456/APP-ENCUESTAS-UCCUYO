using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademicSurveySystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionLineage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "question_lineage_id",
                table: "survey_questions",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE survey_questions SET question_lineage_id = id WHERE question_lineage_id IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "question_lineage_id",
                table: "survey_questions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_survey_questions_question_lineage_id",
                table: "survey_questions",
                column: "question_lineage_id");

            migrationBuilder.CreateIndex(
                name: "IX_survey_questions_survey_section_id_question_lineage_id",
                table: "survey_questions",
                columns: new[] { "survey_section_id", "question_lineage_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_survey_questions_question_lineage_id",
                table: "survey_questions");

            migrationBuilder.DropIndex(
                name: "IX_survey_questions_survey_section_id_question_lineage_id",
                table: "survey_questions");

            migrationBuilder.DropColumn(
                name: "question_lineage_id",
                table: "survey_questions");
        }
    }
}
