namespace AcademicSurveySystem.Application.Dashboard;

public sealed record CareerParticipationDashboardDto(
    Guid CareerId,
    string CareerName,
    Guid AcademicCycleId,
    string AcademicCycleLabel,
    int TotalSubjects,
    int SubjectsWithResponses,
    int StudentSurveyAssignmentsCount,
    int OpenSessionsCount,
    decimal? AverageParticipationPercentage,
    int LowParticipationAssignmentsCount,
    decimal LowParticipationThresholdPercentage,
    IReadOnlyCollection<CareerParticipationDashboardItemDto> Items);

public sealed record CareerParticipationDashboardItemDto(
    Guid SurveyAssignmentId,
    Guid SubjectId,
    string SubjectName,
    Guid TeacherId,
    string TeacherName,
    Guid SurveyId,
    string SurveyTitle,
    int SurveyVersionNumber,
    int? ExpectedRespondentCount,
    int ResponseCount,
    int? RemainingCount,
    decimal? ParticipationPercentage,
    bool HasResponses,
    bool DetailedResultsAvailable,
    bool IsLowParticipation);
