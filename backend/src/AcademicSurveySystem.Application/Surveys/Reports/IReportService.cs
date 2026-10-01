using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Surveys.Reports;

public interface IReportService
{
    Task<ApplicationResult<SurveyReportDto>> BuildSurveyAssignmentReportAsync(
        Guid surveyAssignmentId,
        CancellationToken cancellationToken);
}
