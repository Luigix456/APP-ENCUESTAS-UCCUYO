using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademicSurveySystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCareerAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_careers",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    career_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_careers", x => new { x.user_id, x.career_id });
                    table.ForeignKey(
                        name: "FK_user_careers_careers_career_id",
                        column: x => x.career_id,
                        principalTable: "careers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_careers_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_careers_career_id",
                table: "user_careers",
                column: "career_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_careers");
        }
    }
}
