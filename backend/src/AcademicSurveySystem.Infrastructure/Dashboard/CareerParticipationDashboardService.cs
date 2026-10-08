using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Dashboard;
using AcademicSurveySystem.Application.Surveys.Results;
using AcademicSurveySystem.Domain.Surveys.Enums;
using AcademicSurveySystem.Infrastructure.Persistence;
using AcademicSurveySystem.Infrastructure.Surveys;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AcademicSurveySystem.Infrastructure.Dashboard;

public sealed class CareerParticipationDashboardService : ICareerParticipationDashboardService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly DashboardOptions _dashboardOptions;
    private readonly ResultsPrivacyOptions _resultsPrivacyOptions;

    public CareerParticipationDashboardService(
        ApplicationDbContext dbContext,
        IOptions<DashboardOptions>? dashboardOptions = null,
        IOptions<ResultsPrivacyOptions>? resultsPrivacyOptions = null)
    {
        _dbContext = dbContext;
        _dashboardOptions = dashboardOptions?.Value ?? new DashboardOptions();
        _resultsPrivacyOptions = resultsPrivacyOptions?.Value ?? new ResultsPrivacyOptions();
    }

    public async Task<ApplicationResult<CareerParticipationDashboardDto>> GetCareerParticipationAsync(
        Guid careerId,
        Guid academicCycleId,
        CancellationToken cancellationToken)
    {
        var career = await _dbContext.Careers
            .AsNoTracking()
            .Where(item => item.Id == careerId)
            .Select(item => new
            {
                item.Id,
                item.Name
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (career is null)
        {
            return ApplicationResult<CareerParticipationDashboardDto>.NotFound("Career was not found.");
        }

        var academicCycle = await _dbContext.AcademicCycles
            .AsNoTracking()
            .Where(item => item.Id == academicCycleId)
            .Select(item => new
            {
                item.Id,
                item.Year,
                item.Period
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (academicCycle is null)
        {
            return ApplicationResult<CareerParticipationDashboardDto>.NotFound("Academic cycle was not found.");
        }

        var totalSubjects = await _dbContext.Subjects
            .AsNoTracking()
            .CountAsync(
                subject => subject.CareerId == careerId && subject.IsActive,
                cancellationToken);

        var assignments = await _dbContext.SurveyAssignments
            .AsNoTracking()
            .Where(assignment => assignment.CareerId == careerId
                && assignment.AcademicCycleId == academicCycleId)
            .Select(assignment => new DashboardAssignmentProjection(
                assignment.Id,
                assignment.SubjectId,
                assignment.Subject.Name,
                assignment.TeacherSubjectAssignment.TeacherId,
                assignment.TeacherSubjectAssignment.Teacher.FirstName + " "
                    + assignment.TeacherSubjectAssignment.Teacher.LastName,
                assignment.SurveyId,
                assignment.Survey.Title,
                assignment.Survey.VersionNumber,
                assignment.Survey.Target,
                assignment.ExpectedRespondentCount))
            .OrderBy(item => item.SubjectName)
            .ThenBy(item => item.TeacherName)
            .ThenBy(item => item.SurveyTitle)
            .ThenBy(item => item.SurveyVersionNumber)
            .ToArrayAsync(cancellationToken);

        var assignmentIds = assignments.Select(assignment => assignment.SurveyAssignmentId).ToArray();
        var responseCountsByAssignment = await LoadResponseCountsByAssignmentAsync(assignmentIds, cancellationToken);

        var subjectsWithResponses = assignments
            .Where(assignment => responseCountsByAssignment.GetValueOrDefault(assignment.SurveyAssignmentId) > 0)
            .Select(assignment => assignment.SubjectId)
            .Distinct()
            .Count();

        var openSessionsCount = await LoadOpenSessionsCountAsync(assignmentIds, cancellationToken);

        var items = assignments
            .Where(assignment => assignment.SurveyTarget == SurveyTarget.Student)
            .Select(assignment =>
            {
                var responseCount = responseCountsByAssignment.GetValueOrDefault(assignment.SurveyAssignmentId);
                var progress = SurveyResponseProgressCalculator.Build(
                    assignment.SurveyAssignmentId,
                    assignment.ExpectedRespondentCount,
                    responseCount);
                var isLowParticipation = progress.ParticipationPercentage is not null
                    && progress.ParticipationPercentage.Value < _dashboardOptions.LowParticipationThresholdPercentage;

                return new CareerParticipationDashboardItemDto(
                    assignment.SurveyAssignmentId,
                    assignment.SubjectId,
                    assignment.SubjectName,
                    assignment.TeacherId,
                    assignment.TeacherName,
                    assignment.SurveyId,
                    assignment.SurveyTitle,
                    assignment.SurveyVersionNumber,
                    progress.ExpectedRespondentCount,
                    progress.ResponseCount,
                    progress.RemainingCount,
                    progress.ParticipationPercentage,
                    responseCount > 0,
                    responseCount >= _resultsPrivacyOptions.MinimumResponsesForDetailedResults,
                    isLowParticipation);
            })
            .ToArray();

        var participations = items
            .Where(item => item.ParticipationPercentage is not null)
            .Select(item => item.ParticipationPercentage!.Value)
            .ToArray();
        var averageParticipationPercentage = participations.Length == 0
            ? (decimal?)null
            : decimal.Round(participations.Average(), 2);

        var dashboard = new CareerParticipationDashboardDto(
            career.Id,
            career.Name,
            academicCycle.Id,
            FormatAcademicCycleLabel(academicCycle.Year, academicCycle.Period.ToString()),
            totalSubjects,
            subjectsWithResponses,
            items.Length,
            openSessionsCount,
            averageParticipationPercentage,
            items.Count(item => item.IsLowParticipation),
            _dashboardOptions.LowParticipationThresholdPercentage,
            items);

        return ApplicationResult<CareerParticipationDashboardDto>.Success(dashboard);
    }

    private static string FormatAcademicCycleLabel(int year, string period)
    {
        var periodLabel = period switch
        {
            "Annual" => "Anual",
            "FirstSemester" => "Primer semestre",
            "SecondSemester" => "Segundo semestre",
            _ => period
        };

        return $"{year} - {periodLabel}";
    }

    private async Task<Dictionary<Guid, int>> LoadResponseCountsByAssignmentAsync(
        IReadOnlyCollection<Guid> assignmentIds,
        CancellationToken cancellationToken)
    {
        if (assignmentIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.SurveyResponses
            .AsNoTracking()
            .Where(response => assignmentIds.Contains(response.SurveySession.SurveyAssignmentId))
            .GroupBy(response => response.SurveySession.SurveyAssignmentId)
            .Select(group => new
            {
                SurveyAssignmentId = group.Key,
                ResponseCount = group.Count()
            })
            .ToDictionaryAsync(
                item => item.SurveyAssignmentId,
                item => item.ResponseCount,
                cancellationToken);
    }

    private async Task<int> LoadOpenSessionsCountAsync(
        IReadOnlyCollection<Guid> assignmentIds,
        CancellationToken cancellationToken)
    {
        if (assignmentIds.Count == 0)
        {
            return 0;
        }

        return await _dbContext.SurveySessions
            .AsNoTracking()
            .CountAsync(
                session => assignmentIds.Contains(session.SurveyAssignmentId)
                    && session.Status == SurveySessionStatus.Open,
                cancellationToken);
    }

    private sealed record DashboardAssignmentProjection(
        Guid SurveyAssignmentId,
        Guid SubjectId,
        string SubjectName,
        Guid TeacherId,
        string TeacherName,
        Guid SurveyId,
        string SurveyTitle,
        int SurveyVersionNumber,
        SurveyTarget SurveyTarget,
        int? ExpectedRespondentCount);
}
