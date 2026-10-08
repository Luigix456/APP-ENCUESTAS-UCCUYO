namespace AcademicSurveySystem.Application.Surveys.Results;

public sealed record QuestionHistoryDto(
    Guid QuestionLineageId,
    string LatestQuestionText,
    bool QuestionTextsChanged,
    string QuestionType,
    bool ComparisonSupported,
    string? UnsupportedReason,
    IReadOnlyCollection<QuestionHistoryPointDto> Points);

public sealed record QuestionHistoryPointDto(
    Guid SurveyAssignmentId,
    Guid AcademicCycleId,
    string AcademicCycleName,
    int AcademicCycleYear,
    int SurveyVersionNumber,
    Guid QuestionId,
    string QuestionText,
    int ResponseCount,
    bool DetailedResultsAvailable,
    int MinimumResponsesRequired,
    int ResponsesNeededToUnlock,
    IReadOnlyCollection<QuestionHistoryOptionDistributionDto>? Distribution = null,
    decimal? AverageRating = null,
    int? MinRating = null,
    int? MaxRating = null,
    bool RatingScaleChanged = false);

public sealed record QuestionHistoryOptionDistributionDto(
    string Label,
    int Count,
    decimal Percentage);
