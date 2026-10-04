using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Surveys.Reports;
using AcademicSurveySystem.Application.Surveys.Results;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AcademicSurveySystem.Infrastructure.Reports;

public sealed class ReportService : IReportService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ISurveyResultsService _surveyResultsService;
    private readonly ReportOptions _options;

    public ReportService(
        ApplicationDbContext dbContext,
        ISurveyResultsService surveyResultsService,
        IOptions<ReportOptions> options)
    {
        _dbContext = dbContext;
        _surveyResultsService = surveyResultsService;
        _options = options.Value;
    }

    public async Task<ApplicationResult<SurveyReportDto>> BuildSurveyAssignmentReportAsync(
        Guid surveyAssignmentId,
        CancellationToken cancellationToken)
    {
        var metadata = await _dbContext.SurveyAssignments
            .AsNoTracking()
            .Where(assignment => assignment.Id == surveyAssignmentId)
            .Select(assignment => new ReportMetadata(
                assignment.Id,
                assignment.SurveyId,
                assignment.Survey.Title,
                assignment.Survey.VersionNumber,
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
                assignment.TeacherSubjectAssignment.TeachingRole))
            .SingleOrDefaultAsync(cancellationToken);

        if (metadata is null)
        {
            return ApplicationResult<SurveyReportDto>.NotFound("Survey assignment was not found.");
        }

        var summaryResult = await _surveyResultsService.GetSurveyAssignmentSummaryAsync(
            surveyAssignmentId,
            cancellationToken);

        if (!summaryResult.Succeeded || summaryResult.Value is null)
        {
            return ToReportResult(summaryResult);
        }

        var questionsResult = await _surveyResultsService.GetSurveyAssignmentQuestionResultsAsync(
            surveyAssignmentId,
            cancellationToken);

        if (!questionsResult.Succeeded || questionsResult.Value is null)
        {
            return ToReportResult(questionsResult);
        }

        var questions = questionsResult.Value
            .OrderBy(question => question.Order)
            .ToArray();

        var report = new SurveyReportDto(
            new ReportInstitutionDto(
                _options.InstitutionName,
                _options.SystemName,
                _options.FacultyName),
            DateTimeOffset.UtcNow,
            metadata.SurveyAssignmentId,
            metadata.SurveyId,
            metadata.SurveyTitle,
            metadata.SurveyVersionNumber,
            metadata.CareerId,
            metadata.CareerName,
            metadata.SubjectId,
            metadata.SubjectName,
            metadata.AcademicCycleId,
            metadata.AcademicCycleYear,
            metadata.AcademicCyclePeriod,
            metadata.TeacherId,
            metadata.TeacherFullName,
            metadata.TeachingRole,
            summaryResult.Value.TotalResponses,
            summaryResult.Value.TotalSessions,
            summaryResult.Value.FirstSubmittedAtUtc,
            summaryResult.Value.LastSubmittedAtUtc,
            questions,
            summaryResult.Value.ExpectedRespondentCount,
            summaryResult.Value.RemainingCount,
            summaryResult.Value.ParticipationPercentage);

        return ApplicationResult<SurveyReportDto>.Success(report);
    }

    private static ApplicationResult<SurveyReportDto> ToReportResult<T>(ApplicationResult<T> result)
    {
        return result.Status switch
        {
            ApplicationResultStatus.Validation => ApplicationResult<SurveyReportDto>.Validation(result.Errors),
            ApplicationResultStatus.NotFound => ApplicationResult<SurveyReportDto>.NotFound(
                result.Errors.FirstOrDefault()?.Message ?? "Report data was not found."),
            ApplicationResultStatus.Conflict => ApplicationResult<SurveyReportDto>.Conflict(
                result.Errors.FirstOrDefault()?.Message ?? "Report data is in conflict."),
            _ => ApplicationResult<SurveyReportDto>.Failure(
                result.Errors.FirstOrDefault()?.Message ?? "The report could not be built.")
        };
    }

    private sealed record ReportMetadata(
        Guid SurveyAssignmentId,
        Guid SurveyId,
        string SurveyTitle,
        int SurveyVersionNumber,
        Guid CareerId,
        string CareerName,
        Guid SubjectId,
        string SubjectName,
        Guid AcademicCycleId,
        int AcademicCycleYear,
        string AcademicCyclePeriod,
        Guid TeacherId,
        string TeacherFullName,
        string TeachingRole);
}
