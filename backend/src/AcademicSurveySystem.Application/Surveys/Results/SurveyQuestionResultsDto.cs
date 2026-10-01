namespace AcademicSurveySystem.Application.Surveys.Results;

public sealed record SurveyQuestionResultsDto(
    Guid QuestionId,
    string Text,
    string Type,
    int Order,
    int ResponseCount,
    SurveyChoiceResultsDto? Choice,
    SurveyTextResultsDto? TextValues,
    SurveyRatingResultsDto? Rating,
    SurveyMatrixResultsDto? Matrix,
    IReadOnlyCollection<SurveyQuestionCommentDto> Comments);

public sealed record SurveyChoiceResultsDto(
    IReadOnlyCollection<SurveyOptionDistributionDto> Options,
    SurveyOtherOptionResultsDto? Other = null);

public sealed record SurveyOtherOptionResultsDto(
    int Count,
    decimal Percentage,
    IReadOnlyCollection<string> Values);

public sealed record SurveyOptionDistributionDto(
    Guid OptionId,
    string Text,
    string Value,
    int Count,
    decimal Percentage);

public sealed record SurveyTextResultsDto(
    int ResponseCount,
    IReadOnlyCollection<string> Values);

public sealed record SurveyRatingResultsDto(
    int ResponseCount,
    decimal? Average,
    int? MinimumObserved,
    int? MaximumObserved,
    IReadOnlyCollection<SurveyRatingDistributionDto> Distribution,
    int? ConfiguredMinimum = null,
    int? ConfiguredMaximum = null);

public sealed record SurveyRatingDistributionDto(
    int Value,
    int Count,
    decimal Percentage);

public sealed record SurveyMatrixResultsDto(
    IReadOnlyCollection<SurveyMatrixRowResultsDto> Rows);

public sealed record SurveyMatrixRowResultsDto(
    Guid RowId,
    string RowText,
    int TotalResponses,
    IReadOnlyCollection<SurveyOptionDistributionDto> Options);

public sealed record SurveyQuestionCommentDto(
    Guid AnswerId,
    string Comment);
