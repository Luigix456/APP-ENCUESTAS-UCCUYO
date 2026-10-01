using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Surveys.Results;
using AcademicSurveySystem.Domain.Surveys.Enums;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AcademicSurveySystem.Infrastructure.Surveys.Results;

public sealed class SurveyResultsService : ISurveyResultsService
{
    private readonly ApplicationDbContext _dbContext;

    public SurveyResultsService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<SurveyAssignmentResultListItemDto>>> GetSurveyAssignmentResultsAsync(
        ResultsAccessScope accessScope,
        SurveyAssignmentResultsFilter filter,
        CancellationToken cancellationToken)
    {
        if (!accessScope.IsAllowed)
        {
            return ApplicationResult<IReadOnlyCollection<SurveyAssignmentResultListItemDto>>.Validation([
                new ApplicationError("Results.Forbidden", "Results access is not allowed.")
            ]);
        }

        if (accessScope.Type == ResultsAccessScopeType.Career && accessScope.CareerIds.Count == 0)
        {
            return ApplicationResult<IReadOnlyCollection<SurveyAssignmentResultListItemDto>>.Success([]);
        }

        var sessionStatsQuery = _dbContext.SurveySessions
            .AsNoTracking()
            .GroupBy(session => session.SurveyAssignmentId)
            .Select(group => new
            {
                SurveyAssignmentId = group.Key,
                TotalSessions = (int?)group.Count()
            });

        var responseStatsQuery = _dbContext.SurveyResponses
            .AsNoTracking()
            .GroupBy(response => response.SurveySession.SurveyAssignmentId)
            .Select(group => new
            {
                SurveyAssignmentId = group.Key,
                TotalResponses = (int?)group.Count(),
                FirstSubmittedAtUtc = group.Min(response => (DateTimeOffset?)response.SubmittedAtUtc),
                LastSubmittedAtUtc = group.Max(response => (DateTimeOffset?)response.SubmittedAtUtc)
            });

        var assignmentsQuery = _dbContext.SurveyAssignments
            .AsNoTracking();

        assignmentsQuery = ApplyScope(assignmentsQuery, accessScope);
        assignmentsQuery = ApplyFilter(assignmentsQuery, filter);

        var results = await (
                from assignment in assignmentsQuery
                join sessionStats in sessionStatsQuery
                    on assignment.Id equals sessionStats.SurveyAssignmentId into sessionStatsGroup
                from sessionStats in sessionStatsGroup.DefaultIfEmpty()
                join responseStats in responseStatsQuery
                    on assignment.Id equals responseStats.SurveyAssignmentId into responseStatsGroup
                from responseStats in responseStatsGroup.DefaultIfEmpty()
                orderby assignment.AcademicCycle.Year descending,
                    assignment.Career.Name,
                    assignment.Subject.Name,
                    assignment.TeacherSubjectAssignment.Teacher.LastName,
                    assignment.TeacherSubjectAssignment.Teacher.FirstName,
                    assignment.Survey.Title
                select new SurveyAssignmentResultListItemDto(
                    assignment.Id,
                    assignment.SurveyId,
                    assignment.Survey.Title,
                    assignment.CareerId,
                    assignment.Career.Name,
                    assignment.SubjectId,
                    assignment.Subject.Name,
                    assignment.AcademicCycleId,
                    assignment.AcademicCycle.Year,
                    assignment.AcademicCycle.Period.ToString(),
                    assignment.TeacherSubjectAssignment.TeacherId,
                    assignment.TeacherSubjectAssignment.Teacher.FirstName
                        + " "
                        + assignment.TeacherSubjectAssignment.Teacher.LastName,
                    assignment.TeacherSubjectAssignment.TeachingRole,
                    assignment.IsActive,
                    sessionStats.TotalSessions ?? 0,
                    responseStats.TotalResponses ?? 0,
                    responseStats.FirstSubmittedAtUtc,
                    responseStats.LastSubmittedAtUtc))
            .ToArrayAsync(cancellationToken);

        return ApplicationResult<IReadOnlyCollection<SurveyAssignmentResultListItemDto>>.Success(results);
    }

    public async Task<ApplicationResult<SurveyResultsSummaryDto>> GetSurveyAssignmentSummaryAsync(
        Guid surveyAssignmentId,
        CancellationToken cancellationToken)
    {
        var context = await GetAssignmentContextAsync(surveyAssignmentId, cancellationToken);

        if (context is null)
        {
            return ApplicationResult<SurveyResultsSummaryDto>.NotFound("Survey assignment was not found.");
        }

        var summary = await BuildSummaryAsync(context, sessionId: null, cancellationToken);
        return ApplicationResult<SurveyResultsSummaryDto>.Success(summary);
    }

    public async Task<ApplicationResult<IReadOnlyCollection<SurveyQuestionResultsDto>>> GetSurveyAssignmentQuestionResultsAsync(
        Guid surveyAssignmentId,
        CancellationToken cancellationToken)
    {
        var context = await GetAssignmentContextAsync(surveyAssignmentId, cancellationToken);

        if (context is null)
        {
            return ApplicationResult<IReadOnlyCollection<SurveyQuestionResultsDto>>.NotFound(
                "Survey assignment was not found.");
        }

        var results = await BuildQuestionResultsAsync(context.SurveyId, surveyAssignmentId, null, cancellationToken);
        return ApplicationResult<IReadOnlyCollection<SurveyQuestionResultsDto>>.Success(results);
    }

    public async Task<ApplicationResult<SurveyQuestionResultsDto>> GetSurveyAssignmentQuestionResultAsync(
        Guid surveyAssignmentId,
        Guid questionId,
        CancellationToken cancellationToken)
    {
        var context = await GetAssignmentContextAsync(surveyAssignmentId, cancellationToken);

        if (context is null)
        {
            return ApplicationResult<SurveyQuestionResultsDto>.NotFound("Survey assignment was not found.");
        }

        var results = await BuildQuestionResultsAsync(context.SurveyId, surveyAssignmentId, null, cancellationToken);
        var result = results.SingleOrDefault(question => question.QuestionId == questionId);

        return result is null
            ? ApplicationResult<SurveyQuestionResultsDto>.NotFound("Question was not found.")
            : ApplicationResult<SurveyQuestionResultsDto>.Success(result);
    }

    public async Task<ApplicationResult<SurveyResultsSummaryDto>> GetSurveySessionSummaryAsync(
        Guid surveySessionId,
        CancellationToken cancellationToken)
    {
        var context = await _dbContext.SurveySessions
            .AsNoTracking()
            .Where(session => session.Id == surveySessionId)
            .Select(session => new ResultsContext(
                session.SurveyAssignmentId,
                session.SurveyAssignment.SurveyId,
                session.SurveyAssignment.Survey.Title,
                session.SurveyAssignment.CareerId,
                session.SurveyAssignment.Career.Name,
                session.SurveyAssignment.SubjectId,
                session.SurveyAssignment.Subject.Name,
                session.SurveyAssignment.AcademicCycleId,
                session.SurveyAssignment.AcademicCycle.Year,
                session.SurveyAssignment.AcademicCycle.Period.ToString(),
                session.SurveyAssignment.TeacherSubjectAssignment.TeacherId,
                session.SurveyAssignment.TeacherSubjectAssignment.Teacher.FirstName
                    + " "
                    + session.SurveyAssignment.TeacherSubjectAssignment.Teacher.LastName))
            .SingleOrDefaultAsync(cancellationToken);

        if (context is null)
        {
            return ApplicationResult<SurveyResultsSummaryDto>.NotFound("Survey session was not found.");
        }

        var summary = await BuildSummaryAsync(context, surveySessionId, cancellationToken);
        return ApplicationResult<SurveyResultsSummaryDto>.Success(summary);
    }

    private Task<ResultsContext?> GetAssignmentContextAsync(
        Guid surveyAssignmentId,
        CancellationToken cancellationToken)
    {
        return _dbContext.SurveyAssignments
            .AsNoTracking()
            .Where(assignment => assignment.Id == surveyAssignmentId)
            .Select(assignment => new ResultsContext(
                assignment.Id,
                assignment.SurveyId,
                assignment.Survey.Title,
                assignment.CareerId,
                assignment.Career.Name,
                assignment.SubjectId,
                assignment.Subject.Name,
                assignment.AcademicCycleId,
                assignment.AcademicCycle.Year,
                assignment.AcademicCycle.Period.ToString(),
                assignment.TeacherSubjectAssignment.TeacherId,
                assignment.TeacherSubjectAssignment.Teacher.FirstName
                    + " "
                    + assignment.TeacherSubjectAssignment.Teacher.LastName))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static IQueryable<Domain.Surveys.Entities.SurveyAssignment> ApplyScope(
        IQueryable<Domain.Surveys.Entities.SurveyAssignment> query,
        ResultsAccessScope accessScope)
    {
        return accessScope.Type == ResultsAccessScopeType.Career
            ? query.Where(assignment => accessScope.CareerIds.Contains(assignment.CareerId))
            : query;
    }

    private static IQueryable<Domain.Surveys.Entities.SurveyAssignment> ApplyFilter(
        IQueryable<Domain.Surveys.Entities.SurveyAssignment> query,
        SurveyAssignmentResultsFilter filter)
    {
        if (filter.SurveyId is not null)
        {
            query = query.Where(assignment => assignment.SurveyId == filter.SurveyId.Value);
        }

        if (filter.CareerId is not null)
        {
            query = query.Where(assignment => assignment.CareerId == filter.CareerId.Value);
        }

        if (filter.SubjectId is not null)
        {
            query = query.Where(assignment => assignment.SubjectId == filter.SubjectId.Value);
        }

        if (filter.AcademicCycleId is not null)
        {
            query = query.Where(assignment => assignment.AcademicCycleId == filter.AcademicCycleId.Value);
        }

        if (filter.TeacherId is not null)
        {
            query = query.Where(assignment =>
                assignment.TeacherSubjectAssignment.TeacherId == filter.TeacherId.Value);
        }

        return query;
    }

    private async Task<SurveyResultsSummaryDto> BuildSummaryAsync(
        ResultsContext context,
        Guid? sessionId,
        CancellationToken cancellationToken)
    {
        var responsesQuery = _dbContext.SurveyResponses
            .AsNoTracking()
            .Where(response => response.SurveySession.SurveyAssignmentId == context.SurveyAssignmentId)
            .Where(response => sessionId == null || response.SurveySessionId == sessionId.Value);

        var responseStats = await responsesQuery
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Count = group.Count(),
                FirstSubmittedAtUtc = group.Min(response => response.SubmittedAtUtc),
                LastSubmittedAtUtc = group.Max(response => response.SubmittedAtUtc)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var totalSessions = await _dbContext.SurveySessions
            .AsNoTracking()
            .Where(session => session.SurveyAssignmentId == context.SurveyAssignmentId)
            .Where(session => sessionId == null || session.Id == sessionId.Value)
            .CountAsync(cancellationToken);

        return new SurveyResultsSummaryDto(
            context.SurveyAssignmentId,
            context.SurveyId,
            context.SurveyTitle,
            context.CareerId,
            context.CareerName,
            context.SubjectId,
            context.SubjectName,
            context.AcademicCycleId,
            context.AcademicCycleYear,
            context.AcademicCyclePeriod,
            context.TeacherId,
            context.TeacherFullName,
            responseStats?.Count ?? 0,
            totalSessions,
            responseStats?.FirstSubmittedAtUtc,
            responseStats?.LastSubmittedAtUtc);
    }

    private async Task<IReadOnlyCollection<SurveyQuestionResultsDto>> BuildQuestionResultsAsync(
        Guid surveyId,
        Guid surveyAssignmentId,
        Guid? sessionId,
        CancellationToken cancellationToken)
    {
        var questions = await _dbContext.SurveyQuestions
            .AsNoTracking()
            .Where(question => question.SurveySection.SurveyId == surveyId)
            .OrderBy(question => question.SurveySection.Order)
            .ThenBy(question => question.Order)
            .Select(question => new SurveyResultsCalculator.QuestionTemplate(
                question.Id,
                question.Text,
                question.Type,
                question.AllowsComment,
                question.Order,
                question.Options
                    .OrderBy(option => option.Order)
                    .Select(option => new SurveyResultsCalculator.OptionTemplate(
                        option.Id,
                        option.Text,
                        option.Value,
                        option.Order))
                    .ToArray(),
                question.MatrixRows
                    .OrderBy(row => row.Order)
                    .Select(row => new SurveyResultsCalculator.MatrixRowTemplate(
                        row.Id,
                        row.Text,
                        row.Order))
                    .ToArray(),
                question.AllowsOtherOption,
                question.RatingMin,
                question.RatingMax))
            .ToArrayAsync(cancellationToken);

        var answers = await _dbContext.SurveyAnswers
            .AsNoTracking()
            .Where(answer => answer.SurveyResponse.SurveySession.SurveyAssignmentId == surveyAssignmentId)
            .Where(answer => sessionId == null || answer.SurveyResponse.SurveySessionId == sessionId.Value)
            .Select(answer => new SurveyResultsCalculator.AnswerRecord(
                answer.Id,
                answer.SurveyQuestionId,
                answer.TextValue,
                answer.NumericValue,
                answer.Comment,
                answer.OtherText))
            .ToArrayAsync(cancellationToken);

        var optionSelections = await _dbContext.SurveyAnswerOptions
            .AsNoTracking()
            .Where(selection => selection.SurveyAnswer.SurveyResponse.SurveySession.SurveyAssignmentId
                == surveyAssignmentId)
            .Where(selection => sessionId == null
                || selection.SurveyAnswer.SurveyResponse.SurveySessionId == sessionId.Value)
            .Select(selection => new SurveyResultsCalculator.OptionSelectionRecord(
                selection.SurveyAnswerId,
                selection.SurveyQuestionOptionId))
            .ToArrayAsync(cancellationToken);

        var matrixSelections = await _dbContext.SurveyMatrixAnswers
            .AsNoTracking()
            .Where(selection => selection.SurveyAnswer.SurveyResponse.SurveySession.SurveyAssignmentId
                == surveyAssignmentId)
            .Where(selection => sessionId == null
                || selection.SurveyAnswer.SurveyResponse.SurveySessionId == sessionId.Value)
            .Select(selection => new SurveyResultsCalculator.MatrixSelectionRecord(
                selection.SurveyAnswerId,
                selection.SurveyMatrixRowId,
                selection.SurveyQuestionOptionId))
            .ToArrayAsync(cancellationToken);

        return SurveyResultsCalculator.BuildQuestionResults(
            questions,
            answers,
            optionSelections,
            matrixSelections);
    }

    private sealed record ResultsContext(
        Guid SurveyAssignmentId,
        Guid SurveyId,
        string SurveyTitle,
        Guid CareerId,
        string CareerName,
        Guid SubjectId,
        string SubjectName,
        Guid AcademicCycleId,
        int AcademicCycleYear,
        string AcademicCyclePeriod,
        Guid TeacherId,
        string TeacherFullName);
}
