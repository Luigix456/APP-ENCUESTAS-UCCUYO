namespace AcademicSurveySystem.Application.Academic.SubjectEnrollments;

public sealed record SubjectEnrollmentImportFile(
    string FileName,
    string? ContentType,
    long Length,
    Stream Content);
