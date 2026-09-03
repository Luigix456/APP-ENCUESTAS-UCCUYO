using AcademicSurveySystem.Domain.Common;

namespace AcademicSurveySystem.Domain.Identity;

public static class IdentityCatalog
{
    public static class RoleIds
    {
        public static readonly Guid AdministratorId =
            new("11111111-1111-1111-1111-111111111111");
    }

    public static readonly DateTimeOffset CatalogDateUtc =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static IReadOnlyCollection<RoleDefinition> Roles { get; } =
    [
        new(
            RoleIds.AdministratorId,
            "administrator",
            "Administrador",
            "Acceso completo a la administración y configuración del sistema."),
        new(
            new Guid("22222222-2222-2222-2222-222222222222"),
            "surveyor",
            "Encuestadora",
            "Puede consultar información académica y administrar sesiones de encuesta."),
        new(
            new Guid("33333333-3333-3333-3333-333333333333"),
            "dean",
            "Decana",
            "Puede consultar resultados institucionales e informes."),
        new(
            new Guid("44444444-4444-4444-4444-444444444444"),
            "career_director",
            "Director de carrera",
            "Puede consultar resultados e informes correspondientes a su carrera.")
    ];

    public static IReadOnlyCollection<PermissionDefinition> Permissions { get; } =
    [
        new(
            new Guid("aaaaaaaa-0001-0000-0000-000000000001"),
            "identity.users.read",
            "Consultar usuarios",
            "identity",
            "Permite consultar usuarios del sistema."),
        new(
            new Guid("aaaaaaaa-0002-0000-0000-000000000002"),
            "identity.users.create",
            "Crear usuarios",
            "identity",
            "Permite crear usuarios del sistema."),
        new(
            new Guid("aaaaaaaa-0003-0000-0000-000000000003"),
            "identity.users.update",
            "Modificar usuarios",
            "identity",
            "Permite modificar datos de usuarios del sistema."),
        new(
            new Guid("aaaaaaaa-0004-0000-0000-000000000004"),
            "identity.users.assign_roles",
            "Asignar roles a usuarios",
            "identity",
            "Permite asignar roles institucionales a usuarios."),
        new(
            new Guid("aaaaaaaa-0005-0000-0000-000000000005"),
            "identity.roles.read",
            "Consultar roles y permisos",
            "identity",
            "Permite consultar roles y permisos disponibles."),
        new(
            new Guid("bbbbbbbb-0001-0000-0000-000000000001"),
            "academic.catalog.read",
            "Consultar catálogo académico",
            "academic",
            "Permite consultar la información del catálogo académico."),
        new(
            new Guid("bbbbbbbb-0002-0000-0000-000000000002"),
            "academic.catalog.manage",
            "Administrar catálogo académico",
            "academic",
            "Permite administrar la información del catálogo académico."),
        new(
            new Guid("cccccccc-0001-0000-0000-000000000001"),
            "surveys.templates.read",
            "Consultar encuestas",
            "surveys",
            "Permite consultar plantillas de encuestas."),
        new(
            new Guid("cccccccc-0002-0000-0000-000000000002"),
            "surveys.templates.manage",
            "Administrar encuestas",
            "surveys",
            "Permite administrar plantillas de encuestas."),
        new(
            new Guid("cccccccc-0003-0000-0000-000000000003"),
            "surveys.sessions.manage",
            "Administrar sesiones de encuesta",
            "surveys",
            "Permite administrar sesiones de encuesta."),
        new(
            new Guid("dddddddd-0001-0000-0000-000000000001"),
            "results.read_all",
            "Consultar todos los resultados",
            "results",
            "Permite consultar resultados institucionales completos."),
        new(
            new Guid("dddddddd-0002-0000-0000-000000000002"),
            "results.read_career",
            "Consultar resultados de una carrera",
            "results",
            "Permite consultar resultados correspondientes a una carrera."),
        new(
            new Guid("eeeeeeee-0001-0000-0000-000000000001"),
            "reports.export",
            "Exportar informes",
            "reports",
            "Permite exportar informes del sistema."),
        new(
            new Guid("ffffffff-0001-0000-0000-000000000001"),
            "audit.read",
            "Consultar auditoría",
            "audit",
            "Permite consultar registros de auditoría del sistema.")
    ];

    public static IReadOnlyCollection<RolePermissionDefinition> RolePermissions { get; } =
        BuildRolePermissions();

    public static RoleDefinition GetRole(string code)
    {
        return Roles.SingleOrDefault(role => role.Code == code)
            ?? throw new DomainException($"Role '{code}' was not found in the identity catalog.");
    }

    public static PermissionDefinition GetPermission(string code)
    {
        return Permissions.SingleOrDefault(permission => permission.Code == code)
            ?? throw new DomainException($"Permission '{code}' was not found in the identity catalog.");
    }

    private static IReadOnlyCollection<RolePermissionDefinition> BuildRolePermissions()
    {
        var definitions = new List<RolePermissionDefinition>();

        AddRolePermissions(definitions, "administrator", Permissions.Select(permission => permission.Code));
        AddRolePermissions(
            definitions,
            "surveyor",
            [
                "academic.catalog.read",
                "surveys.templates.read",
                "surveys.sessions.manage"
            ]);
        AddRolePermissions(
            definitions,
            "dean",
            [
                "academic.catalog.read",
                "results.read_all",
                "reports.export"
            ]);
        AddRolePermissions(
            definitions,
            "career_director",
            [
                "academic.catalog.read",
                "results.read_career",
                "reports.export"
            ]);

        return definitions;
    }

    private static void AddRolePermissions(
        ICollection<RolePermissionDefinition> definitions,
        string roleCode,
        IEnumerable<string> permissionCodes)
    {
        var role = GetRole(roleCode);

        foreach (var permissionCode in permissionCodes)
        {
            var permission = GetPermission(permissionCode);
            definitions.Add(new RolePermissionDefinition(role.Id, role.Code, permission.Id, permission.Code));
        }
    }
}

public sealed record RoleDefinition(
    Guid Id,
    string Code,
    string Name,
    string Description);

public sealed record PermissionDefinition(
    Guid Id,
    string Code,
    string Name,
    string Module,
    string Description);

public sealed record RolePermissionDefinition(
    Guid RoleId,
    string RoleCode,
    Guid PermissionId,
    string PermissionCode);
