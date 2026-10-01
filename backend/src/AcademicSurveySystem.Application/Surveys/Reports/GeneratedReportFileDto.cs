namespace AcademicSurveySystem.Application.Surveys.Reports;

public sealed record GeneratedReportFileDto(
    string FileName,
    string ContentType,
    byte[] Content);
