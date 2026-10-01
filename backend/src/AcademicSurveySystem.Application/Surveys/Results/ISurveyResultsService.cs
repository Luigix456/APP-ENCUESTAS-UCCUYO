using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Surveys.Results;

public interface ISurveyResultsService
{
    Task<ApplicationResult<IReadOnlyCollection<SurveyAssignmentResultListItemDto>>> GetSurveyAssignmentResultsAsync(
        ResultsAccessScope accessScope,
        SurveyAssignmentResultsFilter filter,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SurveyResultsSummaryDto>> GetSurveyAssignmentSummaryAsync(
        Guid surveyAssignmentId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<IReadOnlyCollection<SurveyQuestionResultsDto>>> GetSurveyAssignmentQuestionResultsAsync(
        Guid surveyAssignmentId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SurveyQuestionResultsDto>> GetSurveyAssignmentQuestionResultAsync(
        Guid surveyAssignmentId,
        Guid questionId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SurveyResultsSummaryDto>> GetSurveySessionSummaryAsync(
        Guid surveySessionId,
        CancellationToken cancellationToken);
}
