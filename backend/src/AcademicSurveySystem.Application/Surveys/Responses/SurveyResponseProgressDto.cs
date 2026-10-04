namespace AcademicSurveySystem.Application.Surveys.Responses;

public sealed record SurveyResponseProgressDto(
    Guid SurveyAssignmentId,
    int? ExpectedRespondentCount,
    int ResponseCount,
    int? RemainingCount,
    decimal? ParticipationPercentage);
