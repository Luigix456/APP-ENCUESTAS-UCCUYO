namespace AcademicSurveySystem.Application.Identity.UserManagement;

public sealed record UserRoleDto(
    Guid Id,
    string Code,
    string Name);
