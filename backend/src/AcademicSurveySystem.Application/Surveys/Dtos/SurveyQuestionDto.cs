namespace AcademicSurveySystem.Application.Surveys.Dtos;

public sealed record SurveyQuestionDto(
    Guid Id,
    string Text,
    string Type,
    bool IsRequired,
    bool AllowsComment,
    bool AllowsOtherOption,
    int Order,
    bool IsActive,
    IReadOnlyCollection<SurveyQuestionOptionDto> Options,
    IReadOnlyCollection<SurveyMatrixRowDto> MatrixRows,
    int? RatingMin = null,
    int? RatingMax = null);
