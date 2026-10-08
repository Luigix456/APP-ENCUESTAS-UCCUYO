using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Surveys.Reports;
using AcademicSurveySystem.Application.Surveys.Results;
using AcademicSurveySystem.Domain.Academic.Entities;
using AcademicSurveySystem.Domain.Academic.Enums;
using AcademicSurveySystem.Domain.Surveys.Entities;
using AcademicSurveySystem.Domain.Surveys.Enums;
using AcademicSurveySystem.Infrastructure.Persistence;
using AcademicSurveySystem.Infrastructure.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AcademicSurveySystem.UnitTests.Surveys;

public sealed class SurveyReportServiceTests
{
    [Fact]
    public async Task BuildSurveyAssignmentReportAsync_ComposesInstitutionMetadataAndQuestionResults()
    {
        using var context = CreateContext();
        var fixture = SeedReportMetadata(context, surveyVersionNumber: 2);
        var resultsService = new FakeSurveyResultsService(fixture.AssignmentId);
        var service = CreateService(context, resultsService);

        var result = await service.BuildSurveyAssignmentReportAsync(
            fixture.AssignmentId,
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        var report = result.Value!;

        Assert.Equal("Universidad Católica de Cuyo", report.Institution.InstitutionName);
        Assert.Equal("Sistema Web de Gestión de Encuestas Académicas", report.Institution.SystemName);
        Assert.Equal("Facultad de Ciencias Económicas y Empresariales", report.Institution.FacultyName);
        Assert.Equal(fixture.AssignmentId, report.SurveyAssignmentId);
        Assert.Equal(fixture.SurveyId, report.SurveyId);
        Assert.Equal("Evaluación docente", report.SurveyTitle);
        Assert.Equal(2, report.SurveyVersionNumber);
        Assert.Equal(fixture.CareerId, report.CareerId);
        Assert.Equal("Contador Público", report.CareerName);
        Assert.Equal(fixture.SubjectId, report.SubjectId);
        Assert.Equal("Programación I", report.SubjectName);
        Assert.Equal(fixture.TeacherId, report.TeacherId);
        Assert.Equal("Ada Lovelace", report.TeacherFullName);
        Assert.Equal("Titular", report.TeachingRole);
        Assert.Equal(3, report.TotalResponses);
        Assert.True(report.DetailedResultsAvailable);
        Assert.Equal(2, report.TotalSessions);
        Assert.Equal(resultsService.FirstSubmittedAtUtc, report.FirstSubmittedAtUtc);
        Assert.Equal(resultsService.LastSubmittedAtUtc, report.LastSubmittedAtUtc);

        var questions = report.Questions.ToArray();
        Assert.Equal([1, 2, 3, 4, 5, 6], questions.Select(question => question.Order).ToArray());

        var singleChoice = questions.Single(question => question.Type == "SingleChoice");
        Assert.Equal(3, singleChoice.ResponseCount);
        Assert.Equal(2, singleChoice.Choice!.Options.First().Count);
        Assert.Equal(66.67m, singleChoice.Choice.Options.First().Percentage);

        var multipleChoice = questions.Single(question => question.Type == "MultipleChoice");
        Assert.Equal(100, multipleChoice.Choice!.Options.First().Percentage);
        Assert.Equal("Otra opción múltiple", multipleChoice.Choice.Other!.Values.Single());

        var shortText = questions.Single(question => question.Type == "ShortText");
        Assert.Equal(["Respuesta corta"], shortText.TextValues!.Values);

        var longText = questions.Single(question => question.Type == "LongText");
        Assert.Equal(["Respuesta larga"], longText.TextValues!.Values);
        Assert.Equal(["Comentario uno", "Comentario tres"], longText.Comments.Select(comment => comment.Comment));

        var rating = questions.Single(question => question.Type == "RatingScale");
        Assert.Equal(4.33m, rating.Rating!.Average);
        Assert.Equal(1, rating.Rating.ConfiguredMinimum);
        Assert.Equal(5, rating.Rating.ConfiguredMaximum);

        var matrix = questions.Single(question => question.Type == "MatrixSingleChoice");
        var matrixRow = Assert.Single(matrix.Matrix!.Rows);
        Assert.Equal("Claridad", matrixRow.RowText);
        Assert.Equal(3, matrixRow.TotalResponses);
        Assert.Equal(66.67m, matrixRow.Options.First().Percentage);
    }

    [Fact]
    public async Task BuildSurveyAssignmentReportAsync_WithNoResponses_ReturnsReportWithZeroTotals()
    {
        using var context = CreateContext();
        var fixture = SeedReportMetadata(context, surveyVersionNumber: 1);
        var service = CreateService(context, new FakeSurveyResultsService(fixture.AssignmentId, totalResponses: 0));

        var result = await service.BuildSurveyAssignmentReportAsync(
            fixture.AssignmentId,
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.Equal(0, result.Value!.TotalResponses);
        Assert.All(result.Value.Questions, question => Assert.Equal(0, question.ResponseCount));
    }

    [Fact]
    public async Task BuildSurveyAssignmentReportAsync_WhenPrivacyThresholdIsNotReached_OmitsQuestionDetails()
    {
        using var context = CreateContext();
        var fixture = SeedReportMetadata(context, surveyVersionNumber: 1);
        var service = CreateService(
            context,
            new FakeSurveyResultsService(
                fixture.AssignmentId,
                totalResponses: 4,
                detailedResultsAvailable: false,
                minimumResponsesRequired: 5));

        var result = await service.BuildSurveyAssignmentReportAsync(
            fixture.AssignmentId,
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.Equal(4, result.Value!.TotalResponses);
        Assert.False(result.Value.DetailedResultsAvailable);
        Assert.Equal(5, result.Value.MinimumResponsesRequired);
        Assert.Equal(1, result.Value.ResponsesNeededToUnlock);
        Assert.Empty(result.Value.Questions);
    }

    [Fact]
    public async Task BuildSurveyAssignmentReportAsync_UsesHistoricalSurveyVersionFromAssignment()
    {
        using var context = CreateContext();
        var fixtureV1 = SeedReportMetadata(context, surveyVersionNumber: 1);
        var fixtureV2 = SeedReportMetadata(
            context,
            surveyVersionNumber: 2,
            surveyTitle: "Evaluación docente actualizada",
            assignmentId: Guid.NewGuid());
        var service = CreateService(context, new FakeSurveyResultsService(fixtureV1.AssignmentId));

        var result = await service.BuildSurveyAssignmentReportAsync(
            fixtureV1.AssignmentId,
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.Equal(1, result.Value!.SurveyVersionNumber);
        Assert.Equal(fixtureV1.SurveyId, result.Value.SurveyId);
        Assert.NotEqual(fixtureV2.SurveyId, result.Value.SurveyId);
        Assert.Equal("Evaluación docente", result.Value.SurveyTitle);
    }

    private static ReportService CreateService(
        ApplicationDbContext context,
        ISurveyResultsService resultsService)
    {
        return new ReportService(
            context,
            resultsService,
            Options.Create(new ReportOptions()));
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static ReportFixture SeedReportMetadata(
        ApplicationDbContext context,
        int surveyVersionNumber,
        string surveyTitle = "Evaluación docente",
        Guid? assignmentId = null)
    {
        var now = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
        var userId = Guid.NewGuid();
        var academicUnit = new AcademicUnit(Guid.NewGuid(), $"unit-{Guid.NewGuid():N}", "Academic Unit", now);
        var career = new Career(
            Guid.NewGuid(),
            academicUnit.Id,
            $"career-{Guid.NewGuid():N}",
            "Contador Público",
            CareerType.Undergraduate,
            now);
        var subject = new Subject(Guid.NewGuid(), career.Id, $"subject-{Guid.NewGuid():N}", "Programación I", 1, SubjectPeriod.Annual, now);
        var cycle = new AcademicCycle(
            Guid.NewGuid(),
            2026,
            AcademicCyclePeriod.Annual,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            now);
        var teacher = new Teacher(Guid.NewGuid(), "Ada", "Lovelace", "ada@example.com", now);
        var teacherAssignment = new TeacherSubjectAssignment(
            Guid.NewGuid(),
            teacher.Id,
            subject.Id,
            cycle.Id,
            "Titular",
            now);
        var survey = new Survey(Guid.NewGuid(), userId, surveyTitle, null, SurveyTarget.Student, now);
        survey.SetVersionMetadata(survey.Id, surveyVersionNumber, surveyVersionNumber == 1 ? null : Guid.NewGuid());
        var assignment = new SurveyAssignment(
            assignmentId ?? Guid.NewGuid(),
            survey.Id,
            career.Id,
            subject.Id,
            cycle.Id,
            teacherAssignment.Id,
            now);

        context.AddRange(academicUnit, career, subject, cycle, teacher, teacherAssignment, survey, assignment);
        context.SaveChanges();

        return new ReportFixture(
            assignment.Id,
            survey.Id,
            career.Id,
            subject.Id,
            teacher.Id);
    }

    private sealed class FakeSurveyResultsService : ISurveyResultsService
    {
        private readonly int _totalResponses;
        private readonly bool _detailedResultsAvailable;
        private readonly int _minimumResponsesRequired;

        public FakeSurveyResultsService(
            Guid assignmentId,
            int totalResponses = 3,
            bool detailedResultsAvailable = true,
            int minimumResponsesRequired = 5)
        {
            _totalResponses = totalResponses;
            _detailedResultsAvailable = detailedResultsAvailable;
            _minimumResponsesRequired = minimumResponsesRequired;
        }

        public DateTimeOffset FirstSubmittedAtUtc { get; } =
            new(2026, 10, 1, 13, 0, 0, TimeSpan.Zero);

        public DateTimeOffset LastSubmittedAtUtc { get; } =
            new(2026, 10, 1, 15, 0, 0, TimeSpan.Zero);

        public Task<ApplicationResult<SurveyResultsSummaryDto>> GetSurveyAssignmentSummaryAsync(
            Guid surveyAssignmentId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(ApplicationResult<SurveyResultsSummaryDto>.Success(new SurveyResultsSummaryDto(
                surveyAssignmentId,
                Guid.NewGuid(),
                "Evaluación docente",
                Guid.NewGuid(),
                "Contador Público",
                Guid.NewGuid(),
                "Programación I",
                Guid.NewGuid(),
                2026,
                "Annual",
                Guid.NewGuid(),
                "Ada Lovelace",
                _totalResponses,
                2,
                _totalResponses == 0 ? null : FirstSubmittedAtUtc,
                _totalResponses == 0 ? null : LastSubmittedAtUtc,
                DetailedResultsAvailable: _detailedResultsAvailable,
                MinimumResponsesRequired: _minimumResponsesRequired,
                ResponsesNeededToUnlock: Math.Max(0, _minimumResponsesRequired - _totalResponses))));
        }

        public Task<ApplicationResult<IReadOnlyCollection<SurveyQuestionResultsDto>>> GetSurveyAssignmentQuestionResultsAsync(
            Guid surveyAssignmentId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                ApplicationResult<IReadOnlyCollection<SurveyQuestionResultsDto>>.Success(
                    _totalResponses == 0 ? CreateNoResponseQuestions() : CreateQuestions()));
        }

        public Task<ApplicationResult<IReadOnlyCollection<SurveyAssignmentResultListItemDto>>> GetSurveyAssignmentResultsAsync(
            ResultsAccessScope accessScope,
            SurveyAssignmentResultsFilter filter,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<SurveyQuestionResultsDto>> GetSurveyAssignmentQuestionResultAsync(
            Guid surveyAssignmentId,
            Guid questionId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<SurveyResultsSummaryDto>> GetSurveySessionSummaryAsync(
            Guid surveySessionId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<SurveyHistoryDto>> GetSurveyHistoryAsync(
            ResultsAccessScope accessScope,
            SurveyHistoryQuery query,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<QuestionHistoryDto>> GetQuestionHistoryAsync(
            ResultsAccessScope accessScope,
            Guid questionLineageId,
            SurveyHistoryQuery query,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        private static IReadOnlyCollection<SurveyQuestionResultsDto> CreateNoResponseQuestions()
        {
            return CreateQuestions()
                .Select(question => question with { ResponseCount = 0 })
                .ToArray();
        }

        private static IReadOnlyCollection<SurveyQuestionResultsDto> CreateQuestions()
        {
            return
            [
                new SurveyQuestionResultsDto(
                    Guid.NewGuid(),
                    "Claridad",
                    "SingleChoice",
                    1,
                    3,
                    new SurveyChoiceResultsDto(
                        [
                            new SurveyOptionDistributionDto(Guid.NewGuid(), "Sí", "yes", 2, 66.67m),
                            new SurveyOptionDistributionDto(Guid.NewGuid(), "No", "no", 1, 33.33m)
                        ]),
                    null,
                    null,
                    null,
                    []),
                new SurveyQuestionResultsDto(
                    Guid.NewGuid(),
                    "Herramientas usadas",
                    "MultipleChoice",
                    2,
                    3,
                    new SurveyChoiceResultsDto(
                        [
                            new SurveyOptionDistributionDto(Guid.NewGuid(), "Ejercicios", "exercises", 3, 100m),
                            new SurveyOptionDistributionDto(Guid.NewGuid(), "Videos", "videos", 1, 33.33m)
                        ],
                        new SurveyOtherOptionResultsDto(1, 33.33m, ["Otra opción múltiple"])),
                    null,
                    null,
                    null,
                    []),
                new SurveyQuestionResultsDto(
                    Guid.NewGuid(),
                    "Texto corto",
                    "ShortText",
                    3,
                    1,
                    null,
                    new SurveyTextResultsDto(1, ["Respuesta corta"]),
                    null,
                    null,
                    []),
                new SurveyQuestionResultsDto(
                    Guid.NewGuid(),
                    "Texto largo",
                    "LongText",
                    4,
                    1,
                    null,
                    new SurveyTextResultsDto(1, ["Respuesta larga"]),
                    null,
                    null,
                    [
                        new SurveyQuestionCommentDto(Guid.NewGuid(), "Comentario uno"),
                        new SurveyQuestionCommentDto(Guid.NewGuid(), "Comentario tres")
                    ]),
                new SurveyQuestionResultsDto(
                    Guid.NewGuid(),
                    "Valoración",
                    "RatingScale",
                    5,
                    3,
                    null,
                    null,
                    new SurveyRatingResultsDto(
                        3,
                        4.33m,
                        3,
                        5,
                        [
                            new SurveyRatingDistributionDto(1, 0, 0),
                            new SurveyRatingDistributionDto(2, 0, 0),
                            new SurveyRatingDistributionDto(3, 1, 33.33m),
                            new SurveyRatingDistributionDto(4, 0, 0),
                            new SurveyRatingDistributionDto(5, 2, 66.67m)
                        ],
                        1,
                        5),
                    null,
                    []),
                new SurveyQuestionResultsDto(
                    Guid.NewGuid(),
                    "Matriz",
                    "MatrixSingleChoice",
                    6,
                    3,
                    null,
                    null,
                    null,
                    new SurveyMatrixResultsDto(
                        [
                            new SurveyMatrixRowResultsDto(
                                Guid.NewGuid(),
                                "Claridad",
                                3,
                                [
                                    new SurveyOptionDistributionDto(Guid.NewGuid(), "Bueno", "good", 2, 66.67m),
                                    new SurveyOptionDistributionDto(Guid.NewGuid(), "Regular", "regular", 1, 33.33m)
                                ])
                        ]),
                    [])
            ];
        }
    }

    private sealed record ReportFixture(
        Guid AssignmentId,
        Guid SurveyId,
        Guid CareerId,
        Guid SubjectId,
        Guid TeacherId);
}
