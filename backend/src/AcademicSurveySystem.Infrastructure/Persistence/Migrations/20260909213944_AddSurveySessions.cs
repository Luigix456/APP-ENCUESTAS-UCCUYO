using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademicSurveySystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveySessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "survey_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    survey_assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    opened_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    closed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_survey_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_survey_sessions_survey_assignments_survey_assignment_id",
                        column: x => x.survey_assignment_id,
                        principalTable: "survey_assignments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_survey_sessions_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_survey_sessions_access_code",
                table: "survey_sessions",
                column: "access_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_survey_sessions_created_by_user_id",
                table: "survey_sessions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_survey_sessions_expires_at_utc",
                table: "survey_sessions",
                column: "expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_survey_sessions_status",
                table: "survey_sessions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_survey_sessions_survey_assignment_id",
                table: "survey_sessions",
                column: "survey_assignment_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "survey_sessions");
        }
    }
}
