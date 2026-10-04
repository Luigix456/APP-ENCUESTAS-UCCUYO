namespace AcademicSurveySystem.Application.Academic.Common;

public sealed record SubjectEnrollmentDto(
    Guid Id,
    Guid SubjectId,
    string SubjectName,
    Guid CareerId,
    string CareerName,
    Guid AcademicCycleId,
    int AcademicCycleYear,
    string AcademicCyclePeriod,
    int EnrolledStudentCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
