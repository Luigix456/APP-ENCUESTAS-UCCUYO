namespace AcademicSurveySystem.Application.Surveys.Dtos;

public sealed record SurveyDetailDto(
    Guid Id,
    Guid CreatedByUserId,
    string Title,
    string? Description,
    string Target,
    string Status,
    bool IsAnonymous,
    bool IsActive,
    IReadOnlyCollection<SurveySectionDto> Sections,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
