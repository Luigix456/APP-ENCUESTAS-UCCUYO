namespace AcademicSurveySystem.Application.Surveys.Dtos;

public sealed record SurveyQuestionOptionDto(
    Guid Id,
    string Text,
    string Value,
    int Order,
    bool IsActive);
