namespace AcademicSurveySystem.Application.Surveys.Dtos;

public sealed record SurveySectionDto(
    Guid Id,
    string Title,
    string? Description,
    int Order,
    bool IsActive,
    IReadOnlyCollection<SurveyQuestionDto> Questions);
