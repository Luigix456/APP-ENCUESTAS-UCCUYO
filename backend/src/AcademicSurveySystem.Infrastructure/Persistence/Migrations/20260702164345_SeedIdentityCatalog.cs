using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AcademicSurveySystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedIdentityCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "id", "code", "created_at_utc", "description", "module", "name", "updated_at_utc" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-0001-0000-0000-000000000001"), "identity.users.read", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Permite consultar usuarios del sistema.", "identity", "Consultar usuarios", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("aaaaaaaa-0002-0000-0000-000000000002"), "identity.users.create", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Permite crear usuarios del sistema.", "identity", "Crear usuarios", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("aaaaaaaa-0003-0000-0000-000000000003"), "identity.users.update", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Permite modificar datos de usuarios del sistema.", "identity", "Modificar usuarios", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("aaaaaaaa-0004-0000-0000-000000000004"), "identity.users.assign_roles", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Permite asignar roles institucionales a usuarios.", "identity", "Asignar roles a usuarios", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("aaaaaaaa-0005-0000-0000-000000000005"), "identity.roles.read", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Permite consultar roles y permisos disponibles.", "identity", "Consultar roles y permisos", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("bbbbbbbb-0001-0000-0000-000000000001"), "academic.catalog.read", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Permite consultar la información del catálogo académico.", "academic", "Consultar catálogo académico", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("bbbbbbbb-0002-0000-0000-000000000002"), "academic.catalog.manage", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Permite administrar la información del catálogo académico.", "academic", "Administrar catálogo académico", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("cccccccc-0001-0000-0000-000000000001"), "surveys.templates.read", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Permite consultar plantillas de encuestas.", "surveys", "Consultar encuestas", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("cccccccc-0002-0000-0000-000000000002"), "surveys.templates.manage", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Permite administrar plantillas de encuestas.", "surveys", "Administrar encuestas", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("cccccccc-0003-0000-0000-000000000003"), "surveys.sessions.manage", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Permite administrar sesiones de encuesta.", "surveys", "Administrar sesiones de encuesta", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("dddddddd-0001-0000-0000-000000000001"), "results.read_all", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Permite consultar resultados institucionales completos.", "results", "Consultar todos los resultados", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("dddddddd-0002-0000-0000-000000000002"), "results.read_career", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Permite consultar resultados correspondientes a una carrera.", "results", "Consultar resultados de una carrera", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("eeeeeeee-0001-0000-0000-000000000001"), "reports.export", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Permite exportar informes del sistema.", "reports", "Exportar informes", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("ffffffff-0001-0000-0000-000000000001"), "audit.read", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Permite consultar registros de auditoría del sistema.", "audit", "Consultar auditoría", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "code", "created_at_utc", "description", "is_active", "is_system", "name", "updated_at_utc" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "administrator", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Acceso completo a la administración y configuración del sistema.", true, true, "Administrador", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "surveyor", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Puede consultar información académica y administrar sesiones de encuesta.", true, true, "Encuestadora", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "dean", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Puede consultar resultados institucionales e informes.", true, true, "Decana", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "career_director", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Puede consultar resultados e informes correspondientes a su carrera.", true, true, "Director de carrera", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: new[] { "permission_id", "role_id", "granted_at_utc" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-0001-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("aaaaaaaa-0002-0000-0000-000000000002"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("aaaaaaaa-0003-0000-0000-000000000003"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("aaaaaaaa-0004-0000-0000-000000000004"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("aaaaaaaa-0005-0000-0000-000000000005"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("bbbbbbbb-0001-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("bbbbbbbb-0002-0000-0000-000000000002"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("cccccccc-0001-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("cccccccc-0002-0000-0000-000000000002"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("cccccccc-0003-0000-0000-000000000003"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("dddddddd-0001-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("dddddddd-0002-0000-0000-000000000002"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("eeeeeeee-0001-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("ffffffff-0001-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("bbbbbbbb-0001-0000-0000-000000000001"), new Guid("22222222-2222-2222-2222-222222222222"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("cccccccc-0001-0000-0000-000000000001"), new Guid("22222222-2222-2222-2222-222222222222"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("cccccccc-0003-0000-0000-000000000003"), new Guid("22222222-2222-2222-2222-222222222222"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("bbbbbbbb-0001-0000-0000-000000000001"), new Guid("33333333-3333-3333-3333-333333333333"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("dddddddd-0001-0000-0000-000000000001"), new Guid("33333333-3333-3333-3333-333333333333"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("eeeeeeee-0001-0000-0000-000000000001"), new Guid("33333333-3333-3333-3333-333333333333"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("bbbbbbbb-0001-0000-0000-000000000001"), new Guid("44444444-4444-4444-4444-444444444444"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("dddddddd-0002-0000-0000-000000000002"), new Guid("44444444-4444-4444-4444-444444444444"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("eeeeeeee-0001-0000-0000-000000000001"), new Guid("44444444-4444-4444-4444-444444444444"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("aaaaaaaa-0001-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("aaaaaaaa-0002-0000-0000-000000000002"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("aaaaaaaa-0003-0000-0000-000000000003"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("aaaaaaaa-0004-0000-0000-000000000004"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("aaaaaaaa-0005-0000-0000-000000000005"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("bbbbbbbb-0001-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("bbbbbbbb-0002-0000-0000-000000000002"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("cccccccc-0001-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("cccccccc-0002-0000-0000-000000000002"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("cccccccc-0003-0000-0000-000000000003"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("dddddddd-0001-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("dddddddd-0002-0000-0000-000000000002"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("eeeeeeee-0001-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("ffffffff-0001-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("bbbbbbbb-0001-0000-0000-000000000001"), new Guid("22222222-2222-2222-2222-222222222222") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("cccccccc-0001-0000-0000-000000000001"), new Guid("22222222-2222-2222-2222-222222222222") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("cccccccc-0003-0000-0000-000000000003"), new Guid("22222222-2222-2222-2222-222222222222") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("bbbbbbbb-0001-0000-0000-000000000001"), new Guid("33333333-3333-3333-3333-333333333333") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("dddddddd-0001-0000-0000-000000000001"), new Guid("33333333-3333-3333-3333-333333333333") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("eeeeeeee-0001-0000-0000-000000000001"), new Guid("33333333-3333-3333-3333-333333333333") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("bbbbbbbb-0001-0000-0000-000000000001"), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("dddddddd-0002-0000-0000-000000000002"), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.DeleteData(
                table: "role_permissions",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { new Guid("eeeeeeee-0001-0000-0000-000000000001"), new Guid("44444444-4444-4444-4444-444444444444") });

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-0001-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-0002-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-0003-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-0004-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-0005-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("bbbbbbbb-0001-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("bbbbbbbb-0002-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("cccccccc-0001-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("cccccccc-0002-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("cccccccc-0003-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("dddddddd-0001-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("dddddddd-0002-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("eeeeeeee-0001-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("ffffffff-0001-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"));
        }
    }
}
