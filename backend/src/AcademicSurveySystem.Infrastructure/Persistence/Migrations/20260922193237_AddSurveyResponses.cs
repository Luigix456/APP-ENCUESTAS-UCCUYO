using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademicSurveySystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveyResponses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "survey_responses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    survey_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    survey_id = table.Column<Guid>(type: "uuid", nullable: false),
                    submitted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_survey_responses", x => x.id);
                    table.ForeignKey(
                        name: "FK_survey_responses_survey_sessions_survey_session_id",
                        column: x => x.survey_session_id,
                        principalTable: "survey_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_survey_responses_surveys_survey_id",
                        column: x => x.survey_id,
                        principalTable: "surveys",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "survey_answers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    survey_response_id = table.Column<Guid>(type: "uuid", nullable: false),
                    survey_question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text_value = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    numeric_value = table.Column<int>(type: "integer", nullable: true),
                    comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_survey_answers", x => x.id);
                    table.ForeignKey(
                        name: "FK_survey_answers_survey_questions_survey_question_id",
                        column: x => x.survey_question_id,
                        principalTable: "survey_questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_survey_answers_survey_responses_survey_response_id",
                        column: x => x.survey_response_id,
                        principalTable: "survey_responses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "survey_answer_options",
                columns: table => new
                {
                    survey_answer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    survey_question_option_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_survey_answer_options", x => new { x.survey_answer_id, x.survey_question_option_id });
                    table.ForeignKey(
                        name: "FK_survey_answer_options_survey_answers_survey_answer_id",
                        column: x => x.survey_answer_id,
                        principalTable: "survey_answers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_survey_answer_options_survey_question_options_survey_questi~",
                        column: x => x.survey_question_option_id,
                        principalTable: "survey_question_options",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "survey_matrix_answers",
                columns: table => new
                {
                    survey_answer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    survey_matrix_row_id = table.Column<Guid>(type: "uuid", nullable: false),
                    survey_question_option_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_survey_matrix_answers", x => new { x.survey_answer_id, x.survey_matrix_row_id });
                    table.ForeignKey(
                        name: "FK_survey_matrix_answers_survey_answers_survey_answer_id",
                        column: x => x.survey_answer_id,
                        principalTable: "survey_answers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_survey_matrix_answers_survey_matrix_rows_survey_matrix_row_~",
                        column: x => x.survey_matrix_row_id,
                        principalTable: "survey_matrix_rows",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_survey_matrix_answers_survey_question_options_survey_questi~",
                        column: x => x.survey_question_option_id,
                        principalTable: "survey_question_options",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_survey_answer_options_survey_question_option_id",
                table: "survey_answer_options",
                column: "survey_question_option_id");

            migrationBuilder.CreateIndex(
                name: "IX_survey_answers_survey_question_id",
                table: "survey_answers",
                column: "survey_question_id");

            migrationBuilder.CreateIndex(
                name: "IX_survey_answers_survey_response_id",
                table: "survey_answers",
                column: "survey_response_id");

            migrationBuilder.CreateIndex(
                name: "IX_survey_answers_survey_response_id_survey_question_id",
                table: "survey_answers",
                columns: new[] { "survey_response_id", "survey_question_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_survey_matrix_answers_survey_matrix_row_id",
                table: "survey_matrix_answers",
                column: "survey_matrix_row_id");

            migrationBuilder.CreateIndex(
                name: "IX_survey_matrix_answers_survey_question_option_id",
                table: "survey_matrix_answers",
                column: "survey_question_option_id");

            migrationBuilder.CreateIndex(
                name: "IX_survey_responses_submitted_at_utc",
                table: "survey_responses",
                column: "submitted_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_survey_responses_survey_id",
                table: "survey_responses",
                column: "survey_id");

            migrationBuilder.CreateIndex(
                name: "IX_survey_responses_survey_session_id",
                table: "survey_responses",
                column: "survey_session_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "survey_answer_options");

            migrationBuilder.DropTable(
                name: "survey_matrix_answers");

            migrationBuilder.DropTable(
                name: "survey_answers");

            migrationBuilder.DropTable(
                name: "survey_responses");
        }
    }
}
