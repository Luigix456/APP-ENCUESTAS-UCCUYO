namespace AcademicSurveySystem.Application.Surveys.Dtos;

public sealed record SurveySummaryDto(
    Guid Id,
    string Title,
    string? Description,
    string Target,
    string Status,
    bool IsAnonymous,
    bool IsActive,
    int SectionCount,
    int QuestionCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
