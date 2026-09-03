namespace AcademicSurveySystem.Application.Academic.Common;

public sealed record TeacherSubjectAssignmentDto(
    Guid Id,
    Guid TeacherId,
    string TeacherFullName,
    Guid SubjectId,
    string SubjectName,
    Guid CareerId,
    string CareerName,
    Guid AcademicCycleId,
    int AcademicCycleYear,
    string AcademicCyclePeriod,
    string TeachingRole,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
