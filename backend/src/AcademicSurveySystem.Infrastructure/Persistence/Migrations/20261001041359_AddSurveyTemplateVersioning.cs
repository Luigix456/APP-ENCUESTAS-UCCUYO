using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademicSurveySystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveyTemplateVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "based_on_survey_id",
                table: "surveys",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "version_group_id",
                table: "surveys",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "version_number",
                table: "surveys",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(
                """
                UPDATE surveys
                SET version_group_id = id,
                    version_number = 1;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "version_group_id",
                table: "surveys",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "version_number",
                table: "surveys",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_surveys_based_on_survey_id",
                table: "surveys",
                column: "based_on_survey_id");

            migrationBuilder.CreateIndex(
                name: "ix_surveys_version_group_id",
                table: "surveys",
                columns: new[] { "version_group_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_surveys_version_group_id_draft",
                table: "surveys",
                column: "version_group_id",
                unique: true,
                filter: "\"status\" = 'Draft'");

            migrationBuilder.CreateIndex(
                name: "IX_surveys_version_group_id_version_number",
                table: "surveys",
                columns: new[] { "version_group_id", "version_number" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_surveys_surveys_based_on_survey_id",
                table: "surveys",
                column: "based_on_survey_id",
                principalTable: "surveys",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_surveys_surveys_based_on_survey_id",
                table: "surveys");

            migrationBuilder.DropIndex(
                name: "IX_surveys_based_on_survey_id",
                table: "surveys");

            migrationBuilder.DropIndex(
                name: "ix_surveys_version_group_id",
                table: "surveys");

            migrationBuilder.DropIndex(
                name: "ix_surveys_version_group_id_draft",
                table: "surveys");

            migrationBuilder.DropIndex(
                name: "IX_surveys_version_group_id_version_number",
                table: "surveys");

            migrationBuilder.DropColumn(
                name: "based_on_survey_id",
                table: "surveys");

            migrationBuilder.DropColumn(
                name: "version_group_id",
                table: "surveys");

            migrationBuilder.DropColumn(
                name: "version_number",
                table: "surveys");
        }
    }
}
