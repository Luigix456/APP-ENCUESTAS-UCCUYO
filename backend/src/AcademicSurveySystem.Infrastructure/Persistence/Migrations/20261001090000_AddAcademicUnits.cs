using System;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademicSurveySystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261001090000_AddAcademicUnits")]
    public partial class AddAcademicUnits : Migration
    {
        private static readonly Guid InitialAcademicUnitId =
            new("2b886ae7-7b4b-42c4-bb6b-1a7a86d7ab0c");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "academic_units",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_academic_units", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_academic_units_code",
                table: "academic_units",
                column: "code",
                unique: true);

            migrationBuilder.Sql(
                $"""
                INSERT INTO academic_units (
                    id,
                    code,
                    name,
                    is_active,
                    created_at_utc,
                    updated_at_utc)
                VALUES (
                    '{InitialAcademicUnitId}',
                    'economicas',
                    'Facultad de Ciencias Económicas y Empresariales',
                    TRUE,
                    TIMESTAMPTZ '2026-10-01 00:00:00+00',
                    TIMESTAMPTZ '2026-10-01 00:00:00+00');
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "academic_unit_id",
                table: "careers",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                $"""
                UPDATE careers
                SET academic_unit_id = '{InitialAcademicUnitId}';
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "academic_unit_id",
                table: "careers",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_careers_academic_unit_id",
                table: "careers",
                column: "academic_unit_id");

            migrationBuilder.AddForeignKey(
                name: "FK_careers_academic_units_academic_unit_id",
                table: "careers",
                column: "academic_unit_id",
                principalTable: "academic_units",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_careers_academic_units_academic_unit_id",
                table: "careers");

            migrationBuilder.DropIndex(
                name: "IX_careers_academic_unit_id",
                table: "careers");

            migrationBuilder.DropColumn(
                name: "academic_unit_id",
                table: "careers");

            migrationBuilder.DropTable(
                name: "academic_units");
        }
    }
}
