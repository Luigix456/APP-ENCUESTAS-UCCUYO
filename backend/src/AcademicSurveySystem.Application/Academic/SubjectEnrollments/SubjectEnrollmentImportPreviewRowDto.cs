namespace AcademicSurveySystem.Application.Academic.SubjectEnrollments;

public sealed record SubjectEnrollmentImportPreviewRowDto(
    int RowNumber,
    string SubjectCode,
    string? ProvidedSubjectName,
    Guid? SubjectId,
    string? SubjectName,
    int? CurrentEnrolledStudentCount,
    int? NewEnrolledStudentCount,
    string Status,
    string? ErrorCode,
    string? ErrorMessage);
