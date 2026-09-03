using AcademicSurveySystem.Domain.Identity.Enums;

namespace AcademicSurveySystem.Application.Identity.Authentication;

public sealed record AuthenticationUserSnapshot(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string PasswordHash,
    UserStatus Status,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
