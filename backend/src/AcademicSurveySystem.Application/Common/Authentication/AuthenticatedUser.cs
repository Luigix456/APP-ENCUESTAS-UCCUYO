namespace AcademicSurveySystem.Application.Common.Authentication;

public sealed record AuthenticatedUser(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
