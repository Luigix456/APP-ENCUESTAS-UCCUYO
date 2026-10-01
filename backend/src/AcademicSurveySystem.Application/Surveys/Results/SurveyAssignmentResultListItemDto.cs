namespace AcademicSurveySystem.Application.Surveys.Results;

public sealed record SurveyAssignmentResultListItemDto(
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
    string TeachingRole,
    bool IsActive,
    int TotalSessions,
    int TotalResponses,
    DateTimeOffset? FirstSubmittedAtUtc,
    DateTimeOffset? LastSubmittedAtUtc);
