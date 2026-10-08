namespace AcademicSurveySystem.Application.Academic.SubjectEnrollments;

public sealed record SubjectEnrollmentImportTemplateFileDto(
    string FileName,
    string ContentType,
    byte[] Content);
