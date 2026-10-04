namespace AcademicSurveySystem.Application.Surveys.Sessions;

/// <summary>
/// Represents a managed temporary survey session.
/// </summary>
public sealed record SurveySessionDto(
    Guid Id,
    Guid SurveyAssignmentId,
    string AccessCode,
    string PublicPath,
    string? PublicUrl,
    string? Title,
    string? Location,
    string Status,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? OpenedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    bool IsActive,
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
    Guid CreatedByUserId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    int SessionResponseCount = 0,
    int AssignmentResponseCount = 0,
    int? ExpectedRespondentCount = null,
    int? RemainingCount = null,
    decimal? ParticipationPercentage = null);
