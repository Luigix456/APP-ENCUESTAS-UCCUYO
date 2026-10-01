using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Surveys.Reports;

public interface IPdfReportGenerator
{
    Task<ApplicationResult<GeneratedReportFileDto>> GenerateSurveyAssignmentReportAsync(
        SurveyReportDto report,
        CancellationToken cancellationToken);
}
