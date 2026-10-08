namespace AcademicSurveySystem.Application.Academic.SubjectEnrollments;

public sealed record SubjectEnrollmentImportPreviewDto(
    string FileName,
    int TotalRows,
    int ValidRows,
    int CreateRows,
    int UpdateRows,
    int UnchangedRows,
    int ErrorRows,
    IReadOnlyCollection<SubjectEnrollmentImportPreviewRowDto> Rows);
