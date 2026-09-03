namespace AcademicSurveySystem.Application.Academic.Common;

public sealed record TeacherDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
