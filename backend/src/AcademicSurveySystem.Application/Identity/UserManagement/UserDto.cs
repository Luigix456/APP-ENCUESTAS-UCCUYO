namespace AcademicSurveySystem.Application.Identity.UserManagement;

public sealed record UserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Status,
    IReadOnlyCollection<UserRoleDto> Roles,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
