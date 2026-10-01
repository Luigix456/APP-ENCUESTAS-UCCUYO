namespace AcademicSurveySystem.Application.Academic.Common;

public sealed record CareerDto(
    Guid Id,
    Guid AcademicUnitId,
    string AcademicUnitCode,
    string AcademicUnitName,
    string Code,
    string Name,
    string Type,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
