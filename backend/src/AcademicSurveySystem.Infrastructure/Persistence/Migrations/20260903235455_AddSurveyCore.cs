using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademicSurveySystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveyCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "surveys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    target = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_anonymous = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_surveys", x => x.id);
                    table.ForeignKey(
                        name: "FK_surveys_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "survey_sections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    survey_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_survey_sections", x => x.id);
                    table.ForeignKey(
                        name: "FK_survey_sections_surveys_survey_id",
                        column: x => x.survey_id,
                        principalTable: "surveys",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "survey_questions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    survey_section_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    allows_comment = table.Column<bool>(type: "boolean", nullable: false),
                    allows_other_option = table.Column<bool>(type: "boolean", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_survey_questions", x => x.id);
                    table.ForeignKey(
                        name: "FK_survey_questions_survey_sections_survey_section_id",
                        column: x => x.survey_section_id,
                        principalTable: "survey_sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "survey_matrix_rows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    survey_question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_survey_matrix_rows", x => x.id);
                    table.ForeignKey(
                        name: "FK_survey_matrix_rows_survey_questions_survey_question_id",
                        column: x => x.survey_question_id,
                        principalTable: "survey_questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "survey_question_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    survey_question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_survey_question_options", x => x.id);
                    table.ForeignKey(
                        name: "FK_survey_question_options_survey_questions_survey_question_id",
                        column: x => x.survey_question_id,
                        principalTable: "survey_questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_survey_matrix_rows_survey_question_id",
                table: "survey_matrix_rows",
                column: "survey_question_id");

            migrationBuilder.CreateIndex(
                name: "IX_survey_matrix_rows_survey_question_id_order",
                table: "survey_matrix_rows",
                columns: new[] { "survey_question_id", "order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_survey_question_options_survey_question_id",
                table: "survey_question_options",
                column: "survey_question_id");

            migrationBuilder.CreateIndex(
                name: "IX_survey_question_options_survey_question_id_order",
                table: "survey_question_options",
                columns: new[] { "survey_question_id", "order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_survey_questions_survey_section_id",
                table: "survey_questions",
                column: "survey_section_id");

            migrationBuilder.CreateIndex(
                name: "IX_survey_questions_survey_section_id_order",
                table: "survey_questions",
                columns: new[] { "survey_section_id", "order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_survey_sections_survey_id",
                table: "survey_sections",
                column: "survey_id");

            migrationBuilder.CreateIndex(
                name: "IX_survey_sections_survey_id_order",
                table: "survey_sections",
                columns: new[] { "survey_id", "order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_surveys_created_by_user_id",
                table: "surveys",
                column: "created_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "survey_matrix_rows");

            migrationBuilder.DropTable(
                name: "survey_question_options");

            migrationBuilder.DropTable(
                name: "survey_questions");

            migrationBuilder.DropTable(
                name: "survey_sections");

            migrationBuilder.DropTable(
                name: "surveys");
        }
    }
}
