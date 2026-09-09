using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademicSurveySystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveyAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "survey_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    survey_id = table.Column<Guid>(type: "uuid", nullable: false),
                    career_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    academic_cycle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    teacher_subject_assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_survey_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_survey_assignments_academic_cycles_academic_cycle_id",
                        column: x => x.academic_cycle_id,
                        principalTable: "academic_cycles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_survey_assignments_careers_career_id",
                        column: x => x.career_id,
                        principalTable: "careers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_survey_assignments_subjects_subject_id",
                        column: x => x.subject_id,
                        principalTable: "subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_survey_assignments_surveys_survey_id",
                        column: x => x.survey_id,
                        principalTable: "surveys",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_survey_assignments_teacher_subject_assignments_teacher_subj~",
                        column: x => x.teacher_subject_assignment_id,
                        principalTable: "teacher_subject_assignments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_survey_assignments_academic_cycle_id",
                table: "survey_assignments",
                column: "academic_cycle_id");

            migrationBuilder.CreateIndex(
                name: "IX_survey_assignments_career_id",
                table: "survey_assignments",
                column: "career_id");

            migrationBuilder.CreateIndex(
                name: "IX_survey_assignments_subject_id",
                table: "survey_assignments",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_survey_assignments_survey_id",
                table: "survey_assignments",
                column: "survey_id");

            migrationBuilder.CreateIndex(
                name: "IX_survey_assignments_survey_id_career_id_subject_id_academic_~",
                table: "survey_assignments",
                columns: new[] { "survey_id", "career_id", "subject_id", "academic_cycle_id", "teacher_subject_assignment_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_survey_assignments_teacher_subject_assignment_id",
                table: "survey_assignments",
                column: "teacher_subject_assignment_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "survey_assignments");
        }
    }
}
