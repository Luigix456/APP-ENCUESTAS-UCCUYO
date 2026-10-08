namespace AcademicSurveySystem.Application.Surveys.Results;

public sealed record SurveyHistoryDto(
    Guid CareerId,
    string CareerName,
    Guid SubjectId,
    string SubjectName,
    Guid TeacherId,
    string TeacherName,
    Guid SurveyVersionGroupId,
    string SurveyTitle,
    IReadOnlyCollection<SurveyHistoryPointDto> Points,
    IReadOnlyCollection<SurveyHistoryQuestionDto> Questions);

public sealed record SurveyHistoryPointDto(
    Guid SurveyAssignmentId,
    Guid AcademicCycleId,
    string AcademicCycleName,
    int AcademicCycleYear,
    Guid SurveyId,
    int SurveyVersionNumber,
    int? ExpectedRespondentCount,
    int ResponseCount,
    int? RemainingCount,
    decimal? ParticipationPercentage,
    int SessionCount,
    bool DetailedResultsAvailable);

public sealed record SurveyHistoryQuestionDto(
    Guid QuestionLineageId,
    string LatestQuestionText,
    bool QuestionTextsChanged,
    string QuestionType,
    bool ComparisonSupported,
    string? UnsupportedReason);
