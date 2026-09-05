namespace AcademicSurveySystem.Application.Surveys.Dtos;

public sealed record SurveyMatrixRowDto(
    Guid Id,
    string Text,
    int Order,
    bool IsActive);
