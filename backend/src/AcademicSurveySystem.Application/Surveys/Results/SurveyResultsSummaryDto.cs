namespace AcademicSurveySystem.Application.Surveys.Results;

public sealed record SurveyResultsSummaryDto(
    Guid SurveyAssignmentId,
    Guid SurveyId,
    string SurveyTitle,
    Guid CareerId,
    string CareerName,
    Guid SubjectId,
    string SubjectName,
    Guid AcademicCycleId,
    int AcademicCycleYear,
    string AcademicCyclePeriod,
    Guid TeacherId,
    string TeacherFullName,
    int TotalResponses,
    int TotalSessions,
    DateTimeOffset? FirstSubmittedAtUtc,
    DateTimeOffset? LastSubmittedAtUtc);
