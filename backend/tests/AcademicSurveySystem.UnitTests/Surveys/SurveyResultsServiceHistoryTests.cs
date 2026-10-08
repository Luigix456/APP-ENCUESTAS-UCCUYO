using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Surveys.Results;
using AcademicSurveySystem.Domain.Academic.Entities;
using AcademicSurveySystem.Domain.Academic.Enums;
using AcademicSurveySystem.Domain.Surveys.Entities;
using AcademicSurveySystem.Domain.Surveys.Enums;
using AcademicSurveySystem.Infrastructure.Persistence;
using AcademicSurveySystem.Infrastructure.Surveys.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AcademicSurveySystem.UnitTests.Surveys;

public sealed class SurveyResultsServiceHistoryTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetSurveyHistoryAsync_ReturnsOrderedAssignmentPointsWithoutAggregatingSameCycle()
    {
        using var context = CreateContext();
        var fixture = SeedHistoryFixture(context);
        var service = CreateService(context, minimumResponses: 2);

        var result = await service.GetSurveyHistoryAsync(
            ResultsAccessScope.All(),
            fixture.Query,
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.NotNull(result.Value);

        var points = result.Value!.Points.ToArray();

        Assert.Equal(fixture.SurveyVersionGroupId, result.Value.SurveyVersionGroupId);
        Assert.Equal("Encuesta version 2", result.Value.SurveyTitle);
        Assert.Equal(3, points.Length);
        Assert.Equal("2025 · Anual", points[0].AcademicCycleName);
        Assert.Equal("2026 · Anual", points[1].AcademicCycleName);
        Assert.Equal(
            [fixture.Assignment2025Id, fixture.Assignment2026LowId, fixture.Assignment2026DetailedId],
            points.Select(point => point.SurveyAssignmentId).ToArray());
        Assert.Equal(
            [4, 10, null],
            points.Select(point => point.ExpectedRespondentCount).ToArray());
        Assert.Equal(
            [50m, 10m, null],
            points.Select(point => point.ParticipationPercentage).ToArray());
        Assert.Equal(
            [true, false, true],
            points.Select(point => point.DetailedResultsAvailable).ToArray());
        Assert.Contains(
            result.Value.Questions,
            question => question.QuestionLineageId == fixture.ChoiceLineageId);
        Assert.Contains(
            result.Value.Questions,
            question => question.QuestionLineageId == fixture.RatingLineageId);
    }

    [Fact]
    public async Task GetSurveyHistoryAsync_WithCareerScope_DoesNotExpandUserCareerAccess()
    {
        using var context = CreateContext();
        var fixture = SeedHistoryFixture(context);
        var service = CreateService(context, minimumResponses: 2);

        var result = await service.GetSurveyHistoryAsync(
            ResultsAccessScope.Career([Guid.NewGuid()]),
            fixture.Query,
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task GetQuestionHistoryAsync_ForSingleChoice_AppliesPrivacyPerPointAndKeepsVersionOptionsSeparate()
    {
        using var context = CreateContext();
        var fixture = SeedHistoryFixture(context);
        var service = CreateService(context, minimumResponses: 2);

        var result = await service.GetQuestionHistoryAsync(
            ResultsAccessScope.All(),
            fixture.ChoiceLineageId,
            fixture.Query,
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.True(result.Value!.ComparisonSupported);
        Assert.Equal(3, result.Value.Points.Count);

        var v1Point = result.Value.Points.Single(point => point.SurveyAssignmentId == fixture.Assignment2025Id);
        Assert.True(v1Point.DetailedResultsAvailable);
        Assert.NotNull(v1Point.Distribution);
        Assert.Equal(
            [("Excelente v1", 2, 100m), ("Bueno v1", 0, 0m)],
            v1Point.Distribution!.Select(item => (item.Label, item.Count, item.Percentage)).ToArray());

        var protectedPoint = result.Value.Points.Single(point => point.SurveyAssignmentId == fixture.Assignment2026LowId);
        Assert.False(protectedPoint.DetailedResultsAvailable);
        Assert.Null(protectedPoint.Distribution);

        var v2Point = result.Value.Points.Single(point => point.SurveyAssignmentId == fixture.Assignment2026DetailedId);
        Assert.True(v2Point.DetailedResultsAvailable);
        Assert.Equal(
            [("Claro v2", 1, 33.33m), ("Confuso v2", 2, 66.67m)],
            v2Point.Distribution!.Select(item => (item.Label, item.Count, item.Percentage)).ToArray());
    }

    [Fact]
    public async Task GetQuestionHistoryAsync_ForRatingScale_MarksChangedScale()
    {
        using var context = CreateContext();
        var fixture = SeedHistoryFixture(context);
        var service = CreateService(context, minimumResponses: 2);

        var result = await service.GetQuestionHistoryAsync(
            ResultsAccessScope.All(),
            fixture.RatingLineageId,
            fixture.Query,
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.True(result.Value!.ComparisonSupported);
        Assert.All(result.Value.Points.Where(point => point.DetailedResultsAvailable), point =>
        {
            Assert.True(point.RatingScaleChanged);
            Assert.NotNull(point.Distribution);
            Assert.NotNull(point.AverageRating);
        });
        Assert.Equal(4m, result.Value.Points.Single(point => point.SurveyAssignmentId == fixture.Assignment2025Id).AverageRating);
    }

    [Fact]
    public async Task GetQuestionHistoryAsync_ForUnsupportedType_ReturnsMetadataWithoutDetailedComparison()
    {
        using var context = CreateContext();
        var fixture = SeedHistoryFixture(context);
        var service = CreateService(context, minimumResponses: 2);

        var result = await service.GetQuestionHistoryAsync(
            ResultsAccessScope.All(),
            fixture.ShortTextLineageId,
            fixture.Query,
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.False(result.Value!.ComparisonSupported);
        Assert.Equal("Este tipo de pregunta todavía no tiene comparación histórica detallada.", result.Value.UnsupportedReason);
        Assert.All(result.Value.Points, point =>
        {
            Assert.Null(point.Distribution);
            Assert.Null(point.AverageRating);
        });
    }

    [Fact]
    public async Task GetQuestionHistoryAsync_WhenLineageDoesNotBelongToContext_ReturnsValidation()
    {
        using var context = CreateContext();
        var fixture = SeedHistoryFixture(context);
        var service = CreateService(context, minimumResponses: 2);

        var result = await service.GetQuestionHistoryAsync(
            ResultsAccessScope.All(),
            Guid.NewGuid(),
            fixture.Query,
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Validation, result.Status);
        Assert.Contains(result.Errors, error => error.Code == "History.QuestionNotComparable");
    }

    private static SurveyResultsService CreateService(
        ApplicationDbContext context,
        int minimumResponses) =>
        new(context, Options.Create(new ResultsPrivacyOptions
        {
            MinimumResponsesForDetailedResults = minimumResponses
        }));

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static HistoryFixture SeedHistoryFixture(ApplicationDbContext context)
    {
        var userId = Guid.NewGuid();
        var academicUnit = new AcademicUnit(Guid.NewGuid(), "unit", "Unidad", Now);
        var career = new Career(Guid.NewGuid(), academicUnit.Id, "career", "Carrera", CareerType.Undergraduate, Now);
        var subject = new Subject(Guid.NewGuid(), career.Id, "subject", "Materia", 1, SubjectPeriod.Annual, Now);
        var cycle2025 = new AcademicCycle(
            Guid.NewGuid(),
            2025,
            AcademicCyclePeriod.Annual,
            new DateOnly(2025, 1, 1),
            new DateOnly(2025, 12, 31),
            Now);
        var cycle2026 = new AcademicCycle(
            Guid.NewGuid(),
            2026,
            AcademicCyclePeriod.Annual,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            Now);
        var teacher = new Teacher(Guid.NewGuid(), "Ada", "Lovelace", "ada@example.com", Now);
        var teacherAssignment2025 = new TeacherSubjectAssignment(
            Guid.NewGuid(),
            teacher.Id,
            subject.Id,
            cycle2025.Id,
            "Titular",
            Now);
        var teacherAssignment2026 = new TeacherSubjectAssignment(
            Guid.NewGuid(),
            teacher.Id,
            subject.Id,
            cycle2026.Id,
            "Titular",
            Now);

        var v1 = CreateSurveyVersion(
            userId,
            versionGroupId: null,
            versionNumber: 1,
            basedOnSurveyId: null,
            choiceLineageId: null,
            shortTextLineageId: null,
            ratingLineageId: null,
            choiceOptionLabels: ["Excelente v1", "Bueno v1"],
            ratingMax: 5);
        var v2 = CreateSurveyVersion(
            userId,
            v1.Survey.VersionGroupId,
            versionNumber: 2,
            basedOnSurveyId: v1.Survey.Id,
            v1.ChoiceQuestion.QuestionLineageId,
            v1.ShortTextQuestion.QuestionLineageId,
            v1.RatingQuestion.QuestionLineageId,
            choiceOptionLabels: ["Claro v2", "Confuso v2"],
            ratingMax: 7);

        var assignment2025 = new SurveyAssignment(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            v1.Survey.Id,
            career.Id,
            subject.Id,
            cycle2025.Id,
            teacherAssignment2025.Id,
            expectedRespondentCount: 4,
            Now);
        var assignment2026Low = new SurveyAssignment(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            v2.Survey.Id,
            career.Id,
            subject.Id,
            cycle2026.Id,
            teacherAssignment2026.Id,
            expectedRespondentCount: 10,
            Now);
        var assignment2026Detailed = new SurveyAssignment(
            Guid.Parse("00000000-0000-0000-0000-000000000003"),
            v2.Survey.Id,
            career.Id,
            subject.Id,
            cycle2026.Id,
            teacherAssignment2026.Id,
            expectedRespondentCount: null,
            Now);

        context.AddRange(
            academicUnit,
            career,
            subject,
            cycle2025,
            cycle2026,
            teacher,
            teacherAssignment2025,
            teacherAssignment2026,
            v1.Survey,
            v2.Survey,
            assignment2025,
            assignment2026Low,
            assignment2026Detailed);
        context.SaveChanges();

        AddResponse(context, assignment2025, v1, selectedOptionIndex: 0, ratingValue: 5, "Primer texto v1");
        AddResponse(context, assignment2025, v1, selectedOptionIndex: 0, ratingValue: 3, "Segundo texto v1");
        AddResponse(context, assignment2026Low, v2, selectedOptionIndex: 1, ratingValue: 6, "Texto protegido");
        AddResponse(context, assignment2026Detailed, v2, selectedOptionIndex: 0, ratingValue: 7, "Primer texto v2");
        AddResponse(context, assignment2026Detailed, v2, selectedOptionIndex: 1, ratingValue: 6, "Segundo texto v2");
        AddResponse(context, assignment2026Detailed, v2, selectedOptionIndex: 1, ratingValue: 5, "Tercer texto v2");
        context.SaveChanges();

        return new HistoryFixture(
            v1.Survey.VersionGroupId,
            v1.ChoiceQuestion.QuestionLineageId,
            v1.ShortTextQuestion.QuestionLineageId,
            v1.RatingQuestion.QuestionLineageId,
            assignment2025.Id,
            assignment2026Low.Id,
            assignment2026Detailed.Id,
            new SurveyHistoryQuery(career.Id, subject.Id, teacher.Id, v1.Survey.VersionGroupId));
    }

    private static SurveyVersionFixture CreateSurveyVersion(
        Guid userId,
        Guid? versionGroupId,
        int versionNumber,
        Guid? basedOnSurveyId,
        Guid? choiceLineageId,
        Guid? shortTextLineageId,
        Guid? ratingLineageId,
        IReadOnlyList<string> choiceOptionLabels,
        int ratingMax)
    {
        var survey = new Survey(
            Guid.NewGuid(),
            userId,
            $"Encuesta version {versionNumber}",
            null,
            SurveyTarget.Student,
            Now);

        if (versionGroupId is not null)
        {
            survey.SetVersionMetadata(versionGroupId.Value, versionNumber, basedOnSurveyId);
        }

        var section = new SurveySection(Guid.NewGuid(), survey.Id, "Seccion", null, 1, Now);
        var choiceQuestion = new SurveyQuestion(
            Guid.NewGuid(),
            section.Id,
            $"Claridad docente v{versionNumber}",
            SurveyQuestionType.SingleChoice,
            isRequired: true,
            allowsComment: false,
            allowsOtherOption: false,
            order: 1,
            Now,
            questionLineageId: choiceLineageId);
        var firstOption = new SurveyQuestionOption(
            Guid.NewGuid(),
            choiceQuestion.Id,
            choiceOptionLabels[0],
            "first",
            1,
            Now);
        var secondOption = new SurveyQuestionOption(
            Guid.NewGuid(),
            choiceQuestion.Id,
            choiceOptionLabels[1],
            "second",
            2,
            Now);
        choiceQuestion.AddOption(firstOption, Now);
        choiceQuestion.AddOption(secondOption, Now);

        var shortTextQuestion = new SurveyQuestion(
            Guid.NewGuid(),
            section.Id,
            $"Comentario breve v{versionNumber}",
            SurveyQuestionType.ShortText,
            isRequired: false,
            allowsComment: false,
            allowsOtherOption: false,
            order: 2,
            Now,
            questionLineageId: shortTextLineageId);
        var ratingQuestion = new SurveyQuestion(
            Guid.NewGuid(),
            section.Id,
            $"Valoracion v{versionNumber}",
            SurveyQuestionType.RatingScale,
            isRequired: true,
            allowsComment: false,
            allowsOtherOption: false,
            order: 3,
            Now,
            ratingMin: 1,
            ratingMax: ratingMax,
            questionLineageId: ratingLineageId);

        section.AddQuestion(choiceQuestion, Now);
        section.AddQuestion(shortTextQuestion, Now);
        section.AddQuestion(ratingQuestion, Now);
        survey.AddSection(section, Now);
        survey.Publish(Now);

        return new SurveyVersionFixture(
            survey,
            choiceQuestion,
            firstOption,
            secondOption,
            shortTextQuestion,
            ratingQuestion);
    }

    private static void AddResponse(
        ApplicationDbContext context,
        SurveyAssignment assignment,
        SurveyVersionFixture survey,
        int selectedOptionIndex,
        int ratingValue,
        string textValue)
    {
        var session = new SurveySession(
            Guid.NewGuid(),
            assignment.Id,
            survey.Survey.CreatedByUserId,
            Guid.NewGuid().ToString("N"),
            null,
            null,
            Now.AddDays(10),
            Now);
        var response = new SurveyResponse(Guid.NewGuid(), session.Id, survey.Survey.Id, Now.AddMinutes(1));
        var choiceAnswer = new SurveyAnswer(
            Guid.NewGuid(),
            response.Id,
            survey.ChoiceQuestion.Id,
            textValue: null,
            numericValue: null,
            comment: null);
        choiceAnswer.AddSelectedOption(new SurveyAnswerOption(
            choiceAnswer.Id,
            selectedOptionIndex == 0 ? survey.FirstOption.Id : survey.SecondOption.Id));
        response.AddAnswer(choiceAnswer);
        response.AddAnswer(new SurveyAnswer(
            Guid.NewGuid(),
            response.Id,
            survey.ShortTextQuestion.Id,
            textValue,
            numericValue: null,
            comment: null));
        response.AddAnswer(new SurveyAnswer(
            Guid.NewGuid(),
            response.Id,
            survey.RatingQuestion.Id,
            textValue: null,
            ratingValue,
            comment: null));

        context.AddRange(session, response);
    }

    private sealed record SurveyVersionFixture(
        Survey Survey,
        SurveyQuestion ChoiceQuestion,
        SurveyQuestionOption FirstOption,
        SurveyQuestionOption SecondOption,
        SurveyQuestion ShortTextQuestion,
        SurveyQuestion RatingQuestion);

    private sealed record HistoryFixture(
        Guid SurveyVersionGroupId,
        Guid ChoiceLineageId,
        Guid ShortTextLineageId,
        Guid RatingLineageId,
        Guid Assignment2025Id,
        Guid Assignment2026LowId,
        Guid Assignment2026DetailedId,
        SurveyHistoryQuery Query);
}
