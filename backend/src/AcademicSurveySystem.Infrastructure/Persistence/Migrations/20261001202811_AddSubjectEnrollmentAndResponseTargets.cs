using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademicSurveySystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubjectEnrollmentAndResponseTargets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "expected_respondent_count",
                table: "survey_assignments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "subject_enrollments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    academic_cycle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enrolled_student_count = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subject_enrollments", x => x.id);
                    table.CheckConstraint("ck_subject_enrollments_enrolled_student_count_positive", "enrolled_student_count > 0");
                    table.ForeignKey(
                        name: "FK_subject_enrollments_academic_cycles_academic_cycle_id",
                        column: x => x.academic_cycle_id,
                        principalTable: "academic_cycles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_subject_enrollments_subjects_subject_id",
                        column: x => x.subject_id,
                        principalTable: "subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_survey_assignments_expected_respondent_count_positive",
                table: "survey_assignments",
                sql: "expected_respondent_count IS NULL OR expected_respondent_count > 0");

            migrationBuilder.CreateIndex(
                name: "IX_subject_enrollments_academic_cycle_id",
                table: "subject_enrollments",
                column: "academic_cycle_id");

            migrationBuilder.CreateIndex(
                name: "IX_subject_enrollments_subject_id",
                table: "subject_enrollments",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_subject_enrollments_subject_id_academic_cycle_id",
                table: "subject_enrollments",
                columns: new[] { "subject_id", "academic_cycle_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "subject_enrollments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_survey_assignments_expected_respondent_count_positive",
                table: "survey_assignments");

            migrationBuilder.DropColumn(
                name: "expected_respondent_count",
                table: "survey_assignments");
        }
    }
}
