namespace AcademicSurveySystem.Application.Academic.Common;

public sealed record AcademicUnitDto(
    Guid Id,
    string Code,
    string Name,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
