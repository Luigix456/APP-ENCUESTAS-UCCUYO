namespace AcademicSurveySystem.Application.Surveys.Results;

public interface IResultsAccessService
{
    Task<ResultsAccessScope> GetDiscoveryScopeAsync(
        Guid userId,
        bool hasReadAll,
        bool hasReadCareer,
        CancellationToken cancellationToken);

    Task<ResultsAccessDecision> AuthorizeSurveyAssignmentAsync(
        Guid userId,
        bool hasReadAll,
        bool hasReadCareer,
        Guid surveyAssignmentId,
        CancellationToken cancellationToken);

    Task<ResultsAccessDecision> AuthorizeSurveySessionAsync(
        Guid userId,
        bool hasReadAll,
        bool hasReadCareer,
        Guid surveySessionId,
        CancellationToken cancellationToken);
}
