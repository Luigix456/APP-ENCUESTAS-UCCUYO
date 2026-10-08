using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Surveys.Results;
using AcademicSurveySystem.Domain.Surveys.Enums;
using AcademicSurveySystem.Infrastructure.Persistence;
using AcademicSurveySystem.Infrastructure.Surveys;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AcademicSurveySystem.Infrastructure.Surveys.Results;

public sealed class SurveyResultsService : ISurveyResultsService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ResultsPrivacyOptions _privacyOptions;

    public SurveyResultsService(ApplicationDbContext dbContext)
        : this(dbContext, Options.Create(new ResultsPrivacyOptions()))
    {
    }

    public SurveyResultsService(
        ApplicationDbContext dbContext,
        IOptions<ResultsPrivacyOptions> privacyOptions)
    {
        _dbContext = dbContext;
        _privacyOptions = privacyOptions.Value;
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
                    responseStats.LastSubmittedAtUtc,
                    assignment.ExpectedRespondentCount,
                    CalculateParticipationPercentage(
                        assignment.ExpectedRespondentCount,
                        responseStats.TotalResponses ?? 0),
                    assignment.Survey.VersionGroupId,
                    assignment.Survey.VersionNumber))
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

        var privacyResult = await EnsureDetailedResultsAvailableAsync(
            surveyAssignmentId,
            sessionId: null,
            cancellationToken);

        if (privacyResult is not null)
        {
            return privacyResult;
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

        var privacyResult = await EnsureDetailedResultsAvailableAsync(
            surveyAssignmentId,
            sessionId: null,
            cancellationToken);

        if (privacyResult is not null)
        {
            return new ApplicationResult<SurveyQuestionResultsDto>(
                privacyResult.Status,
                default,
                privacyResult.Errors);
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
                    + session.SurveyAssignment.TeacherSubjectAssignment.Teacher.LastName,
                session.SurveyAssignment.ExpectedRespondentCount))
            .SingleOrDefaultAsync(cancellationToken);

        if (context is null)
        {
            return ApplicationResult<SurveyResultsSummaryDto>.NotFound("Survey session was not found.");
        }

        var summary = await BuildSummaryAsync(context, surveySessionId, cancellationToken);
        return ApplicationResult<SurveyResultsSummaryDto>.Success(summary);
    }

    public async Task<ApplicationResult<SurveyHistoryDto>> GetSurveyHistoryAsync(
        ResultsAccessScope accessScope,
        SurveyHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateHistoryQuery(query);

        if (validationErrors.Count > 0)
        {
            return ApplicationResult<SurveyHistoryDto>.Validation(validationErrors);
        }

        if (!accessScope.IsAllowed)
        {
            return ApplicationResult<SurveyHistoryDto>.Validation([
                new ApplicationError("Results.Forbidden", "Results access is not allowed.")
            ]);
        }

        var assignments = await LoadHistoryAssignmentsAsync(accessScope, query, cancellationToken);

        if (assignments.Count == 0)
        {
            return ApplicationResult<SurveyHistoryDto>.NotFound("History was not found.");
        }

        var questions = await LoadHistoryQuestionsAsync(assignments.Select(item => item.SurveyId).Distinct().ToArray(), cancellationToken);
        var first = assignments[0];
        var latest = assignments[^1];

        return ApplicationResult<SurveyHistoryDto>.Success(new SurveyHistoryDto(
            first.CareerId,
            first.CareerName,
            first.SubjectId,
            first.SubjectName,
            first.TeacherId,
            first.TeacherName,
            first.SurveyVersionGroupId,
            latest.SurveyTitle,
            assignments.Select(MapHistoryPoint).ToArray(),
            questions));
    }

    public async Task<ApplicationResult<QuestionHistoryDto>> GetQuestionHistoryAsync(
        ResultsAccessScope accessScope,
        Guid questionLineageId,
        SurveyHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateHistoryQuery(query).ToList();

        if (questionLineageId == Guid.Empty)
        {
            validationErrors.Add(new ApplicationError(
                "History.QuestionLineageIdRequired",
                "QuestionLineageId is required."));
        }

        if (validationErrors.Count > 0)
        {
            return ApplicationResult<QuestionHistoryDto>.Validation(validationErrors);
        }

        if (!accessScope.IsAllowed)
        {
            return ApplicationResult<QuestionHistoryDto>.Validation([
                new ApplicationError("Results.Forbidden", "Results access is not allowed.")
            ]);
        }

        var assignments = await LoadHistoryAssignmentsAsync(accessScope, query, cancellationToken);

        if (assignments.Count == 0)
        {
            return ApplicationResult<QuestionHistoryDto>.NotFound("History was not found.");
        }

        var assignmentIds = assignments.Select(item => item.SurveyAssignmentId).ToArray();
        var surveyIds = assignments.Select(item => item.SurveyId).Distinct().ToArray();
        var questionTemplates = await _dbContext.SurveyQuestions
            .AsNoTracking()
            .Where(question => surveyIds.Contains(question.SurveySection.SurveyId))
            .Where(question => question.QuestionLineageId == questionLineageId)
            .Select(question => new HistoryQuestionTemplate(
                question.Id,
                question.QuestionLineageId,
                question.SurveySection.SurveyId,
                question.SurveySection.Survey.VersionNumber,
                question.Text,
                question.Type,
                question.Order,
                question.RatingMin,
                question.RatingMax,
                question.Options
                    .OrderBy(option => option.Order)
                    .Select(option => new HistoryQuestionOptionTemplate(
                        option.Id,
                        option.Text,
                        option.Value,
                        option.Order))
                    .ToArray()))
            .ToArrayAsync(cancellationToken);

        if (questionTemplates.Length == 0)
        {
            return ApplicationResult<QuestionHistoryDto>.Validation([
                new ApplicationError(
                    "History.QuestionNotComparable",
                    "Question lineage does not belong to the requested survey family.")
            ]);
        }

        var latestQuestion = questionTemplates
            .OrderByDescending(question => question.SurveyVersionNumber)
            .ThenBy(question => question.Order)
            .First();
        var distinctTexts = questionTemplates
            .Select(question => question.Text)
            .Distinct(StringComparer.Ordinal)
            .Count();
        var distinctTypes = questionTemplates
            .Select(question => question.Type)
            .Distinct()
            .ToArray();
        var comparisonSupported = distinctTypes.Length == 1
            && latestQuestion.Type is SurveyQuestionType.SingleChoice or SurveyQuestionType.RatingScale;
        var unsupportedReason = comparisonSupported
            ? null
            : distinctTypes.Length > 1
                ? "El tipo de pregunta cambió entre versiones."
                : "Este tipo de pregunta todavía no tiene comparación histórica detallada.";
        var questionIds = questionTemplates.Select(question => question.QuestionId).ToArray();
        var answerRecords = await _dbContext.SurveyAnswers
            .AsNoTracking()
            .Where(answer => questionIds.Contains(answer.SurveyQuestionId))
            .Where(answer => assignmentIds.Contains(answer.SurveyResponse.SurveySession.SurveyAssignmentId))
            .Select(answer => new HistoryQuestionAnswerRecord(
                answer.SurveyResponse.SurveySession.SurveyAssignmentId,
                answer.Id,
                answer.SurveyQuestionId,
                answer.NumericValue))
            .ToArrayAsync(cancellationToken);
        var answerIds = answerRecords.Select(answer => answer.AnswerId).ToArray();
        var optionSelections = latestQuestion.Type == SurveyQuestionType.SingleChoice && comparisonSupported
            ? await _dbContext.SurveyAnswerOptions
                .AsNoTracking()
                .Where(selection => answerIds.Contains(selection.SurveyAnswerId))
                .Select(selection => new HistoryQuestionOptionSelectionRecord(
                    selection.SurveyAnswer.SurveyResponse.SurveySession.SurveyAssignmentId,
                    selection.SurveyAnswerId,
                    selection.SurveyQuestionOptionId))
                .ToArrayAsync(cancellationToken)
            : [];
        var ratingScales = questionTemplates
            .Where(question => question.Type == SurveyQuestionType.RatingScale)
            .Select(question => new { question.RatingMin, question.RatingMax })
            .Distinct()
            .Count();
        var ratingScaleChanged = ratingScales > 1;
        var points = assignments
            .Select(assignment =>
            {
                var question = questionTemplates.SingleOrDefault(item => item.SurveyId == assignment.SurveyId);

                return question is null
                    ? null
                    : BuildQuestionHistoryPoint(
                        assignment,
                        question,
                        comparisonSupported,
                        ratingScaleChanged,
                        answerRecords,
                        optionSelections);
            })
            .Where(point => point is not null)
            .Select(point => point!)
            .ToArray();

        return ApplicationResult<QuestionHistoryDto>.Success(new QuestionHistoryDto(
            questionLineageId,
            latestQuestion.Text,
            distinctTexts > 1,
            latestQuestion.Type.ToString(),
            comparisonSupported,
            unsupportedReason,
            points));
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
                    + assignment.TeacherSubjectAssignment.Teacher.LastName,
                assignment.ExpectedRespondentCount))
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

        var responseCount = responseStats?.Count ?? 0;
        var progress = SurveyResponseProgressCalculator.Build(
            context.SurveyAssignmentId,
            context.ExpectedRespondentCount,
            responseCount);

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
            responseCount,
            totalSessions,
            responseStats?.FirstSubmittedAtUtc,
            responseStats?.LastSubmittedAtUtc,
            context.ExpectedRespondentCount,
            progress.RemainingCount,
            progress.ParticipationPercentage,
            responseCount >= _privacyOptions.MinimumResponsesForDetailedResults,
            _privacyOptions.MinimumResponsesForDetailedResults,
            Math.Max(0, _privacyOptions.MinimumResponsesForDetailedResults - responseCount));
    }

    private async Task<ApplicationResult<IReadOnlyCollection<SurveyQuestionResultsDto>>?>
        EnsureDetailedResultsAvailableAsync(
            Guid surveyAssignmentId,
            Guid? sessionId,
            CancellationToken cancellationToken)
    {
        var responseCount = await _dbContext.SurveyResponses
            .AsNoTracking()
            .Where(response => response.SurveySession.SurveyAssignmentId == surveyAssignmentId)
            .Where(response => sessionId == null || response.SurveySessionId == sessionId.Value)
            .CountAsync(cancellationToken);

        if (responseCount >= _privacyOptions.MinimumResponsesForDetailedResults)
        {
            return null;
        }

        return new ApplicationResult<IReadOnlyCollection<SurveyQuestionResultsDto>>(
            ApplicationResultStatus.Conflict,
            default,
            [
                new ApplicationError(
                    "Results.PrivacyThresholdNotReached",
                    $"Detailed results require at least {_privacyOptions.MinimumResponsesForDetailedResults} responses.")
            ]);
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

    private async Task<IReadOnlyList<HistoryAssignmentRecord>> LoadHistoryAssignmentsAsync(
        ResultsAccessScope accessScope,
        SurveyHistoryQuery query,
        CancellationToken cancellationToken)
    {
        if (accessScope.Type == ResultsAccessScopeType.Career && accessScope.CareerIds.Count == 0)
        {
            return [];
        }

        var sessionStatsQuery = _dbContext.SurveySessions
            .AsNoTracking()
            .GroupBy(session => session.SurveyAssignmentId)
            .Select(group => new
            {
                SurveyAssignmentId = group.Key,
                SessionCount = (int?)group.Count()
            });
        var responseStatsQuery = _dbContext.SurveyResponses
            .AsNoTracking()
            .GroupBy(response => response.SurveySession.SurveyAssignmentId)
            .Select(group => new
            {
                SurveyAssignmentId = group.Key,
                ResponseCount = (int?)group.Count()
            });
        var assignmentsQuery = ApplyScope(_dbContext.SurveyAssignments.AsNoTracking(), accessScope)
            .Where(assignment => assignment.CareerId == query.CareerId)
            .Where(assignment => assignment.SubjectId == query.SubjectId)
            .Where(assignment => assignment.TeacherSubjectAssignment.TeacherId == query.TeacherId)
            .Where(assignment => assignment.Survey.VersionGroupId == query.SurveyVersionGroupId)
            .Where(assignment => assignment.Survey.Target == SurveyTarget.Student);

        return await (
                from assignment in assignmentsQuery
                join sessionStats in sessionStatsQuery
                    on assignment.Id equals sessionStats.SurveyAssignmentId into sessionStatsGroup
                from sessionStats in sessionStatsGroup.DefaultIfEmpty()
                join responseStats in responseStatsQuery
                    on assignment.Id equals responseStats.SurveyAssignmentId into responseStatsGroup
                from responseStats in responseStatsGroup.DefaultIfEmpty()
                orderby assignment.AcademicCycle.Year,
                    assignment.AcademicCycle.StartDate,
                    assignment.Survey.VersionNumber,
                    assignment.Id
                select new HistoryAssignmentRecord(
                    assignment.Id,
                    assignment.CareerId,
                    assignment.Career.Name,
                    assignment.SubjectId,
                    assignment.Subject.Name,
                    assignment.TeacherSubjectAssignment.TeacherId,
                    assignment.TeacherSubjectAssignment.Teacher.FirstName
                        + " "
                        + assignment.TeacherSubjectAssignment.Teacher.LastName,
                    assignment.Survey.VersionGroupId,
                    assignment.Survey.Title,
                    assignment.SurveyId,
                    assignment.Survey.VersionNumber,
                    assignment.AcademicCycleId,
                    assignment.AcademicCycle.Year,
                    assignment.AcademicCycle.Period.ToString(),
                    assignment.ExpectedRespondentCount,
                    responseStats.ResponseCount ?? 0,
                    sessionStats.SessionCount ?? 0))
            .ToArrayAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<SurveyHistoryQuestionDto>> LoadHistoryQuestionsAsync(
        IReadOnlyCollection<Guid> surveyIds,
        CancellationToken cancellationToken)
    {
        var questions = await _dbContext.SurveyQuestions
            .AsNoTracking()
            .Where(question => surveyIds.Contains(question.SurveySection.SurveyId))
            .Select(question => new
            {
                question.QuestionLineageId,
                question.Text,
                question.Type,
                question.Order,
                question.SurveySection.Survey.VersionNumber
            })
            .ToArrayAsync(cancellationToken);

        return questions
            .GroupBy(question => question.QuestionLineageId)
            .Select(group =>
            {
                var latest = group
                    .OrderByDescending(question => question.VersionNumber)
                    .ThenBy(question => question.Order)
                    .First();
                var types = group.Select(question => question.Type).Distinct().ToArray();
                var supported = types.Length == 1
                    && latest.Type is SurveyQuestionType.SingleChoice or SurveyQuestionType.RatingScale;
                var unsupportedReason = supported
                    ? null
                    : types.Length > 1
                        ? "El tipo de pregunta cambió entre versiones."
                        : "Este tipo de pregunta todavía no tiene comparación histórica detallada.";

                return new SurveyHistoryQuestionDto(
                    group.Key,
                    latest.Text,
                    group.Select(question => question.Text).Distinct(StringComparer.Ordinal).Count() > 1,
                    latest.Type.ToString(),
                    supported,
                    unsupportedReason);
            })
            .OrderBy(question => question.LatestQuestionText)
            .ToArray();
    }

    private QuestionHistoryPointDto BuildQuestionHistoryPoint(
        HistoryAssignmentRecord assignment,
        HistoryQuestionTemplate question,
        bool comparisonSupported,
        bool ratingScaleChanged,
        IReadOnlyCollection<HistoryQuestionAnswerRecord> answers,
        IReadOnlyCollection<HistoryQuestionOptionSelectionRecord> optionSelections)
    {
        var detailedResultsAvailable = assignment.ResponseCount >= _privacyOptions.MinimumResponsesForDetailedResults;
        var responsesNeededToUnlock = Math.Max(
            0,
            _privacyOptions.MinimumResponsesForDetailedResults - assignment.ResponseCount);

        if (!comparisonSupported || !detailedResultsAvailable)
        {
            return new QuestionHistoryPointDto(
                assignment.SurveyAssignmentId,
                assignment.AcademicCycleId,
                FormatAcademicCycleName(assignment.AcademicCycleYear, assignment.AcademicCyclePeriod),
                assignment.AcademicCycleYear,
                assignment.SurveyVersionNumber,
                question.QuestionId,
                question.Text,
                assignment.ResponseCount,
                detailedResultsAvailable,
                _privacyOptions.MinimumResponsesForDetailedResults,
                responsesNeededToUnlock);
        }

        var questionAnswers = answers
            .Where(answer => answer.SurveyAssignmentId == assignment.SurveyAssignmentId
                && answer.QuestionId == question.QuestionId)
            .ToArray();

        if (question.Type == SurveyQuestionType.SingleChoice)
        {
            var answerIds = questionAnswers.Select(answer => answer.AnswerId).ToHashSet();
            var denominator = questionAnswers.Length;
            var distribution = question.Options
                .OrderBy(option => option.Order)
                .Select(option =>
                {
                    var count = optionSelections.Count(selection =>
                        selection.SurveyAssignmentId == assignment.SurveyAssignmentId
                        && answerIds.Contains(selection.AnswerId)
                        && selection.OptionId == option.OptionId);

                    return new QuestionHistoryOptionDistributionDto(
                        option.Text,
                        count,
                        CalculatePercentage(count, denominator));
                })
                .ToArray();

            return new QuestionHistoryPointDto(
                assignment.SurveyAssignmentId,
                assignment.AcademicCycleId,
                FormatAcademicCycleName(assignment.AcademicCycleYear, assignment.AcademicCyclePeriod),
                assignment.AcademicCycleYear,
                assignment.SurveyVersionNumber,
                question.QuestionId,
                question.Text,
                assignment.ResponseCount,
                detailedResultsAvailable,
                _privacyOptions.MinimumResponsesForDetailedResults,
                responsesNeededToUnlock,
                Distribution: distribution);
        }

        var values = questionAnswers
            .Where(answer => answer.NumericValue is not null)
            .Select(answer => answer.NumericValue!.Value)
            .ToArray();
        var valueCounts = values
            .GroupBy(value => value)
            .ToDictionary(group => group.Key, group => group.Count());
        var ratingValues = question.RatingMin is not null && question.RatingMax is not null
            ? Enumerable.Range(question.RatingMin.Value, question.RatingMax.Value - question.RatingMin.Value + 1)
            : valueCounts.Keys.Order();
        var ratingDistribution = ratingValues
            .Select(value =>
            {
                var count = valueCounts.TryGetValue(value, out var existingCount)
                    ? existingCount
                    : 0;

                return new QuestionHistoryOptionDistributionDto(
                    value.ToString(),
                    count,
                    CalculatePercentage(count, values.Length));
            })
            .ToArray();

        return new QuestionHistoryPointDto(
            assignment.SurveyAssignmentId,
            assignment.AcademicCycleId,
            FormatAcademicCycleName(assignment.AcademicCycleYear, assignment.AcademicCyclePeriod),
            assignment.AcademicCycleYear,
            assignment.SurveyVersionNumber,
            question.QuestionId,
            question.Text,
            assignment.ResponseCount,
            detailedResultsAvailable,
            _privacyOptions.MinimumResponsesForDetailedResults,
            responsesNeededToUnlock,
            Distribution: ratingDistribution,
            AverageRating: values.Length == 0 ? null : Math.Round((decimal)values.Average(), 2),
            MinRating: question.RatingMin,
            MaxRating: question.RatingMax,
            RatingScaleChanged: ratingScaleChanged);
    }

    private SurveyHistoryPointDto MapHistoryPoint(HistoryAssignmentRecord assignment)
    {
        var progress = SurveyResponseProgressCalculator.Build(
            assignment.SurveyAssignmentId,
            assignment.ExpectedRespondentCount,
            assignment.ResponseCount);

        return new SurveyHistoryPointDto(
            assignment.SurveyAssignmentId,
            assignment.AcademicCycleId,
            FormatAcademicCycleName(assignment.AcademicCycleYear, assignment.AcademicCyclePeriod),
            assignment.AcademicCycleYear,
            assignment.SurveyId,
            assignment.SurveyVersionNumber,
            assignment.ExpectedRespondentCount,
            assignment.ResponseCount,
            progress.RemainingCount,
            progress.ParticipationPercentage,
            assignment.SessionCount,
            assignment.ResponseCount >= _privacyOptions.MinimumResponsesForDetailedResults);
    }

    private static IReadOnlyCollection<ApplicationError> ValidateHistoryQuery(SurveyHistoryQuery query)
    {
        var errors = new List<ApplicationError>();

        AddRequiredGuidError(query.CareerId, "History.CareerIdRequired", "CareerId is required.", errors);
        AddRequiredGuidError(query.SubjectId, "History.SubjectIdRequired", "SubjectId is required.", errors);
        AddRequiredGuidError(query.TeacherId, "History.TeacherIdRequired", "TeacherId is required.", errors);
        AddRequiredGuidError(
            query.SurveyVersionGroupId,
            "History.SurveyVersionGroupIdRequired",
            "SurveyVersionGroupId is required.",
            errors);

        return errors;
    }

    private static void AddRequiredGuidError(
        Guid value,
        string code,
        string message,
        ICollection<ApplicationError> errors)
    {
        if (value == Guid.Empty)
        {
            errors.Add(new ApplicationError(code, message));
        }
    }

    private static string FormatAcademicCycleName(int year, string period)
    {
        var periodLabel = period switch
        {
            "Annual" => "Anual",
            "FirstSemester" => "Primer semestre",
            "SecondSemester" => "Segundo semestre",
            _ => period
        };

        return $"{year} · {periodLabel}";
    }

    private static decimal CalculatePercentage(int count, int denominator)
    {
        return denominator == 0
            ? 0
            : Math.Round(count * 100m / denominator, 2);
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
        string TeacherFullName,
        int? ExpectedRespondentCount);

    private sealed record HistoryAssignmentRecord(
        Guid SurveyAssignmentId,
        Guid CareerId,
        string CareerName,
        Guid SubjectId,
        string SubjectName,
        Guid TeacherId,
        string TeacherName,
        Guid SurveyVersionGroupId,
        string SurveyTitle,
        Guid SurveyId,
        int SurveyVersionNumber,
        Guid AcademicCycleId,
        int AcademicCycleYear,
        string AcademicCyclePeriod,
        int? ExpectedRespondentCount,
        int ResponseCount,
        int SessionCount);

    private sealed record HistoryQuestionTemplate(
        Guid QuestionId,
        Guid QuestionLineageId,
        Guid SurveyId,
        int SurveyVersionNumber,
        string Text,
        SurveyQuestionType Type,
        int Order,
        int? RatingMin,
        int? RatingMax,
        IReadOnlyCollection<HistoryQuestionOptionTemplate> Options);

    private sealed record HistoryQuestionOptionTemplate(
        Guid OptionId,
        string Text,
        string Value,
        int Order);

    private sealed record HistoryQuestionAnswerRecord(
        Guid SurveyAssignmentId,
        Guid AnswerId,
        Guid QuestionId,
        int? NumericValue);

    private sealed record HistoryQuestionOptionSelectionRecord(
        Guid SurveyAssignmentId,
        Guid AnswerId,
        Guid OptionId);

    private static decimal? CalculateParticipationPercentage(
        int? expectedRespondentCount,
        int responseCount)
    {
        return SurveyResponseProgressCalculator.Build(
            Guid.Empty,
            expectedRespondentCount,
            responseCount).ParticipationPercentage;
    }
}
