using AcademicSurveySystem.Application.Surveys.Results;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AcademicSurveySystem.Infrastructure.Surveys.Results;

public sealed class ResultsAccessService : IResultsAccessService
{
    private readonly ApplicationDbContext _dbContext;

    public ResultsAccessService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ResultsAccessScope> GetDiscoveryScopeAsync(
        Guid userId,
        bool hasReadAll,
        bool hasReadCareer,
        CancellationToken cancellationToken)
    {
        if (hasReadAll)
        {
            return ResultsAccessScope.All();
        }

        if (!hasReadCareer)
        {
            return ResultsAccessScope.Forbidden();
        }

        var careerIds = await _dbContext.UserCareers
            .AsNoTracking()
            .Where(userCareer => userCareer.UserId == userId)
            .Select(userCareer => userCareer.CareerId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        return ResultsAccessScope.Career(careerIds);
    }

    public async Task<ResultsAccessDecision> AuthorizeSurveyAssignmentAsync(
        Guid userId,
        bool hasReadAll,
        bool hasReadCareer,
        Guid surveyAssignmentId,
        CancellationToken cancellationToken)
    {
        var careerId = await _dbContext.SurveyAssignments
            .AsNoTracking()
            .Where(assignment => assignment.Id == surveyAssignmentId)
            .Select(assignment => (Guid?)assignment.CareerId)
            .SingleOrDefaultAsync(cancellationToken);

        if (careerId is null)
        {
            return ResultsAccessDecision.SurveyAssignmentNotFound();
        }

        return await AuthorizeCareerAsync(userId, hasReadAll, hasReadCareer, careerId.Value, cancellationToken);
    }

    public async Task<ResultsAccessDecision> AuthorizeSurveySessionAsync(
        Guid userId,
        bool hasReadAll,
        bool hasReadCareer,
        Guid surveySessionId,
        CancellationToken cancellationToken)
    {
        var careerId = await _dbContext.SurveySessions
            .AsNoTracking()
            .Where(session => session.Id == surveySessionId)
            .Select(session => (Guid?)session.SurveyAssignment.CareerId)
            .SingleOrDefaultAsync(cancellationToken);

        if (careerId is null)
        {
            return ResultsAccessDecision.SurveySessionNotFound();
        }

        return await AuthorizeCareerAsync(userId, hasReadAll, hasReadCareer, careerId.Value, cancellationToken);
    }

    private async Task<ResultsAccessDecision> AuthorizeCareerAsync(
        Guid userId,
        bool hasReadAll,
        bool hasReadCareer,
        Guid careerId,
        CancellationToken cancellationToken)
    {
        if (hasReadAll)
        {
            return ResultsAccessDecision.Allowed();
        }

        if (!hasReadCareer)
        {
            return ResultsAccessDecision.Forbidden();
        }

        var hasCareer = await _dbContext.UserCareers
            .AsNoTracking()
            .AnyAsync(
                userCareer => userCareer.UserId == userId && userCareer.CareerId == careerId,
                cancellationToken);

        return hasCareer
            ? ResultsAccessDecision.Allowed()
            : ResultsAccessDecision.Forbidden();
    }
}
