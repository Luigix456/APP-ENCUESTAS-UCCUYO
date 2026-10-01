namespace AcademicSurveySystem.Application.Identity.UserManagement;

public sealed record RoleDto(
    Guid Id,
    string Code,
    string Name,
    IReadOnlyCollection<string> Permissions);
