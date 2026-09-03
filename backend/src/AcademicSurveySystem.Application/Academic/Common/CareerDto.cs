namespace AcademicSurveySystem.Application.Academic.Common;

public sealed record CareerDto(
    Guid Id,
    string Code,
    string Name,
    string Type,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
