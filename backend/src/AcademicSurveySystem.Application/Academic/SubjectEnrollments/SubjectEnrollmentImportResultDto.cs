namespace AcademicSurveySystem.Application.Academic.SubjectEnrollments;

public sealed record SubjectEnrollmentImportResultDto(
    int CreatedCount,
    int UpdatedCount,
    int UnchangedCount,
    int TotalProcessed);
