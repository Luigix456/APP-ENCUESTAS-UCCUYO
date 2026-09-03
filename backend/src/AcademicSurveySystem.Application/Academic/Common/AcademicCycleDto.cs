namespace AcademicSurveySystem.Application.Academic.Common;

public sealed record AcademicCycleDto(
    Guid Id,
    int Year,
    string Period,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
