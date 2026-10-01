using AcademicSurveySystem.Application.Surveys.Results;

namespace AcademicSurveySystem.Application.Surveys.Reports;

public sealed record SurveyReportDto(
    ReportInstitutionDto Institution,
    DateTimeOffset GeneratedAtUtc,
    Guid SurveyAssignmentId,
    Guid SurveyId,
    string SurveyTitle,
    int SurveyVersionNumber,
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
    int TotalResponses,
    int TotalSessions,
    DateTimeOffset? FirstSubmittedAtUtc,
    DateTimeOffset? LastSubmittedAtUtc,
    IReadOnlyCollection<SurveyQuestionResultsDto> Questions);

public sealed record ReportInstitutionDto(
    string InstitutionName,
    string SystemName,
    string? FacultyName);
