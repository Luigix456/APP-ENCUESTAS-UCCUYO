namespace AcademicSurveySystem.Application.Academic.Common;

public sealed record SubjectDto(
    Guid Id,
    Guid CareerId,
    string CareerName,
    string Code,
    string Name,
    int Year,
    string Period,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
