using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Surveys.Assignments;
using AcademicSurveySystem.Application.Surveys.Requests;
using AcademicSurveySystem.Application.Surveys.Sessions;
using AcademicSurveySystem.Domain.Academic.Entities;
using AcademicSurveySystem.Domain.Academic.Enums;
using AcademicSurveySystem.Domain.Surveys.Entities;
using AcademicSurveySystem.Domain.Surveys.Enums;
using AcademicSurveySystem.Infrastructure.Persistence;
using AcademicSurveySystem.Infrastructure.Surveys;
using AcademicSurveySystem.Infrastructure.Surveys.Results;
using Microsoft.EntityFrameworkCore;

namespace AcademicSurveySystem.UnitTests.Surveys;

public sealed class SurveyTemplateVersioningTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetOrCreateEditableVersionAsync_WithDraft_ReturnsSameSurvey()
    {
        using var context = CreateContext();
        var fixture = SeedSurvey(context, SurveyStatus.Draft);
        var service = new SurveyTemplateService(context);

        var result = await service.GetOrCreateEditableVersionAsync(
            fixture.Survey.Id,
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.False(result.Value!.CreatedNewVersion);
        Assert.Equal(fixture.Survey.Id, result.Value.Survey.Id);
        Assert.Equal(fixture.Survey.VersionGroupId, result.Value.VersionGroupId);
        Assert.Equal(1, result.Value.VersionNumber);
    }

    [Theory]
    [InlineData(SurveyStatus.Published)]
    [InlineData(SurveyStatus.Archived)]
    public async Task GetOrCreateEditableVersionAsync_WithImmutableSurvey_CreatesDraftDeepClone(
        SurveyStatus sourceStatus)
    {
        using var context = CreateContext();
        var fixture = SeedSurvey(context, sourceStatus);
        var service = new SurveyTemplateService(context);
        var requesterId = Guid.NewGuid();

        var result = await service.GetOrCreateEditableVersionAsync(
            fixture.Survey.Id,
            requesterId,
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.True(result.Value!.CreatedNewVersion);
        Assert.Equal(fixture.Survey.Id, result.Value.SourceSurveyId);
        Assert.Equal(fixture.Survey.VersionGroupId, result.Value.VersionGroupId);
        Assert.Equal(2, result.Value.VersionNumber);

        var clone = result.Value.Survey;
        Assert.NotEqual(fixture.Survey.Id, clone.Id);
        Assert.Equal(requesterId, clone.CreatedByUserId);
        Assert.Equal("Draft", clone.Status);
        Assert.Equal(fixture.Survey.Id, clone.BasedOnSurveyId);
        Assert.Equal(fixture.Survey.Title, clone.Title);
        Assert.Equal(fixture.Survey.Description, clone.Description);
        Assert.Equal(fixture.Survey.Target.ToString(), clone.Target);
        Assert.Equal(fixture.Survey.IsAnonymous, clone.IsAnonymous);
        Assert.Equal(fixture.Survey.IsActive, clone.IsActive);

        AssertDeepClone(fixture, clone);
        AssertOriginalSurveyWasNotChanged(context, fixture.Survey.Id, sourceStatus);
    }

    [Fact]
    public async Task GetOrCreateEditableVersionAsync_ReusesExistingDraftInVersionGroup()
    {
        using var context = CreateContext();
        var fixture = SeedSurvey(context, SurveyStatus.Published);
        var service = new SurveyTemplateService(context);

        var first = await service.GetOrCreateEditableVersionAsync(
            fixture.Survey.Id,
            Guid.NewGuid(),
            CancellationToken.None);
        var secondFromV1 = await service.GetOrCreateEditableVersionAsync(
            fixture.Survey.Id,
            Guid.NewGuid(),
            CancellationToken.None);
        var secondFromDraft = await service.GetOrCreateEditableVersionAsync(
            first.Value!.Survey.Id,
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(first.Value!.CreatedNewVersion);
        Assert.False(secondFromV1.Value!.CreatedNewVersion);
        Assert.False(secondFromDraft.Value!.CreatedNewVersion);
        Assert.Equal(first.Value.Survey.Id, secondFromV1.Value.Survey.Id);
        Assert.Equal(first.Value.Survey.Id, secondFromDraft.Value.Survey.Id);
        Assert.Equal(2, secondFromV1.Value.VersionNumber);
    }

    [Fact]
    public async Task GetOrCreateEditableVersionAsync_IncrementsVersionNumberFromLatestPublishedVersion()
    {
        using var context = CreateContext();
        var fixture = SeedSurvey(context, SurveyStatus.Published);
        var service = new SurveyTemplateService(context);

        var v2 = await service.GetOrCreateEditableVersionAsync(
            fixture.Survey.Id,
            Guid.NewGuid(),
            CancellationToken.None);
        var publishV2 = await service.PublishSurveyAsync(v2.Value!.Survey.Id, CancellationToken.None);
        var v3 = await service.GetOrCreateEditableVersionAsync(
            v2.Value.Survey.Id,
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(publishV2.Succeeded);
        Assert.True(v3.Value!.CreatedNewVersion);
        Assert.Equal(3, v3.Value.VersionNumber);
        Assert.Equal(v2.Value.Survey.Id, v3.Value.Survey.BasedOnSurveyId);
        Assert.Equal(fixture.Survey.VersionGroupId, v3.Value.VersionGroupId);
    }

    [Fact]
    public async Task Versioning_DoesNotChangeHistoricalResultsForOriginalSurvey()
    {
        using var context = CreateContext();
        var fixture = SeedSurvey(context, SurveyStatus.Published);
        var academic = SeedAcademicContext(context);
        var assignment = SeedAssignmentSessionAndResponse(context, fixture, academic);
        var resultsService = new SurveyResultsService(context);
        var templateService = new SurveyTemplateService(context);

        var before = await resultsService.GetSurveyAssignmentQuestionResultsAsync(
            assignment.Id,
            CancellationToken.None);
        var editable = await templateService.GetOrCreateEditableVersionAsync(
            fixture.Survey.Id,
            Guid.NewGuid(),
            CancellationToken.None);
        await templateService.UpdateQuestionAsync(
            editable.Value!.Survey.Id,
            editable.Value.Survey.Sections.First().Id,
            editable.Value.Survey.Sections.First().Questions.First().Id,
            new UpdateSurveyQuestionRequest(
                "Texto v2",
                "SingleChoice",
                IsRequired: true,
                AllowsComment: true,
                AllowsOtherOption: true,
                Order: 1,
                RatingMin: null,
                RatingMax: null),
            CancellationToken.None);
        await templateService.UpdateOptionAsync(
            editable.Value.Survey.Id,
            editable.Value.Survey.Sections.First().Id,
            editable.Value.Survey.Sections.First().Questions.First().Id,
            editable.Value.Survey.Sections.First().Questions.First().Options.First().Id,
            new UpdateSurveyQuestionOptionRequest("Opcion v2", "option-v2", 1),
            CancellationToken.None);

        var after = await resultsService.GetSurveyAssignmentQuestionResultsAsync(
            assignment.Id,
            CancellationToken.None);

        var beforeQuestion = before.Value!.Single(question => question.QuestionId == fixture.ChoiceQuestion.Id);
        var afterQuestion = after.Value!.Single(question => question.QuestionId == fixture.ChoiceQuestion.Id);
        Assert.Equal(beforeQuestion.Text, afterQuestion.Text);
        Assert.Equal(beforeQuestion.Type, afterQuestion.Type);
        Assert.Equal(beforeQuestion.ResponseCount, afterQuestion.ResponseCount);
        Assert.Equal(
            beforeQuestion.Choice!.Options.Select(option => (option.OptionId, option.Text, option.Count)).ToArray(),
            afterQuestion.Choice!.Options.Select(option => (option.OptionId, option.Text, option.Count)).ToArray());
        Assert.DoesNotContain(after.Value!, question => question.Text == "Texto v2");
    }

    [Fact]
    public async Task Versioning_DoesNotBreakExistingAssignmentSessionsOrPublishingV2()
    {
        using var context = CreateContext();
        var fixture = SeedSurvey(context, SurveyStatus.Published);
        var academic = SeedAcademicContext(context);
        var assignment = new SurveyAssignment(
            Guid.NewGuid(),
            fixture.Survey.Id,
            academic.Career.Id,
            academic.Subject.Id,
            academic.Cycle.Id,
            academic.TeacherAssignment.Id,
            CreatedAtUtc);
        context.Add(assignment);
        await context.SaveChangesAsync();
        var templateService = new SurveyTemplateService(context);
        var sessionService = new SurveySessionService(context, new FixedAccessCodeGenerator());

        var editable = await templateService.GetOrCreateEditableVersionAsync(
            fixture.Survey.Id,
            Guid.NewGuid(),
            CancellationToken.None);
        var v1Session = await sessionService.CreateSessionAsync(
            new CreateSurveySessionRequest(
                assignment.Id,
                "Sesion v1",
                null,
                DateTimeOffset.UtcNow.AddHours(1)),
            Guid.NewGuid(),
            CancellationToken.None);
        var publishV2 = await templateService.PublishSurveyAsync(
            editable.Value!.Survey.Id,
            CancellationToken.None);
        var v2Assignment = await new SurveyAssignmentService(context).CreateAssignmentAsync(
            new CreateSurveyAssignmentRequest(
                editable.Value.Survey.Id,
                academic.Career.Id,
                academic.Subject.Id,
                academic.Cycle.Id,
                academic.TeacherAssignment.Id),
            CancellationToken.None);

        var v1 = await context.Surveys.SingleAsync(survey => survey.Id == fixture.Survey.Id);
        Assert.True(v1Session.Succeeded);
        Assert.True(publishV2.Succeeded);
        Assert.True(v2Assignment.Succeeded);
        Assert.Equal(SurveyStatus.Published, v1.Status);
        Assert.True(v1.IsActive);
        Assert.Equal(fixture.Survey.Id, assignment.SurveyId);
        Assert.Equal(editable.Value.Survey.Id, v2Assignment.Value!.SurveyId);
    }

    private static void AssertDeepClone(SurveyFixture source, Application.Surveys.Dtos.SurveyDetailDto clone)
    {
        Assert.Equal(source.Survey.Sections.Count, clone.Sections.Count);
        Assert.DoesNotContain(clone.Sections, section => section.Id == source.Section.Id);

        var clonedSection = clone.Sections.Single(section => section.Order == source.Section.Order);
        Assert.Equal(source.Section.Title, clonedSection.Title);
        Assert.Equal(source.Section.Description, clonedSection.Description);
        Assert.Equal(source.Section.IsActive, clonedSection.IsActive);

        var clonedChoice = clonedSection.Questions.Single(question => question.Order == source.ChoiceQuestion.Order);
        Assert.NotEqual(source.ChoiceQuestion.Id, clonedChoice.Id);
        Assert.Equal(source.ChoiceQuestion.Text, clonedChoice.Text);
        Assert.Equal(source.ChoiceQuestion.Type.ToString(), clonedChoice.Type);
        Assert.Equal(source.ChoiceQuestion.IsRequired, clonedChoice.IsRequired);
        Assert.Equal(source.ChoiceQuestion.AllowsComment, clonedChoice.AllowsComment);
        Assert.Equal(source.ChoiceQuestion.AllowsOtherOption, clonedChoice.AllowsOtherOption);
        Assert.Equal(source.ChoiceQuestion.IsActive, clonedChoice.IsActive);
        Assert.Equal(source.ChoiceQuestion.Options.Count, clonedChoice.Options.Count);
        Assert.DoesNotContain(clonedChoice.Options, option => option.Id == source.ActiveOption.Id);
        Assert.Contains(clonedChoice.Options, option =>
            option.Text == source.InactiveOption.Text
            && option.Value == source.InactiveOption.Value
            && option.IsActive == source.InactiveOption.IsActive);

        var clonedMatrix = clonedSection.Questions.Single(question => question.Order == source.MatrixQuestion.Order);
        Assert.NotEqual(source.MatrixQuestion.Id, clonedMatrix.Id);
        Assert.Equal(source.MatrixQuestion.MatrixRows.Count, clonedMatrix.MatrixRows.Count);
        Assert.DoesNotContain(clonedMatrix.MatrixRows, row => row.Id == source.MatrixRow.Id);
        Assert.Contains(clonedMatrix.MatrixRows, row =>
            row.Text == source.InactiveMatrixRow.Text
            && row.IsActive == source.InactiveMatrixRow.IsActive);

        var clonedRating = clonedSection.Questions.Single(question => question.Order == source.RatingQuestion.Order);
        Assert.Equal(source.RatingQuestion.RatingMin, clonedRating.RatingMin);
        Assert.Equal(source.RatingQuestion.RatingMax, clonedRating.RatingMax);
    }

    private static void AssertOriginalSurveyWasNotChanged(
        ApplicationDbContext context,
        Guid surveyId,
        SurveyStatus expectedStatus)
    {
        var original = context.Surveys
            .AsNoTracking()
            .Single(survey => survey.Id == surveyId);

        Assert.Equal(expectedStatus, original.Status);
        Assert.Equal(1, original.VersionNumber);
        Assert.Null(original.BasedOnSurveyId);
    }

    private static SurveyAssignment SeedAssignmentSessionAndResponse(
        ApplicationDbContext context,
        SurveyFixture survey,
        AcademicFixture academic)
    {
        var assignment = new SurveyAssignment(
            Guid.NewGuid(),
            survey.Survey.Id,
            academic.Career.Id,
            academic.Subject.Id,
            academic.Cycle.Id,
            academic.TeacherAssignment.Id,
            CreatedAtUtc);
        var session = new SurveySession(
            Guid.NewGuid(),
            assignment.Id,
            survey.Survey.CreatedByUserId,
            "history-session",
            "Historica",
            null,
            CreatedAtUtc.AddHours(2),
            CreatedAtUtc);
        var response = new SurveyResponse(Guid.NewGuid(), session.Id, survey.Survey.Id, CreatedAtUtc.AddMinutes(30));
        var answer = new SurveyAnswer(
            Guid.NewGuid(),
            response.Id,
            survey.ChoiceQuestion.Id,
            textValue: null,
            numericValue: null,
            comment: null,
            otherText: null);
        answer.AddSelectedOption(new SurveyAnswerOption(answer.Id, survey.ActiveOption.Id));
        response.AddAnswer(answer);
        context.AddRange(assignment, session, response);
        context.SaveChanges();

        return assignment;
    }

    private static AcademicFixture SeedAcademicContext(ApplicationDbContext context)
    {
        var career = new Career(Guid.NewGuid(), "career", "Career", CareerType.Undergraduate, CreatedAtUtc);
        var subject = new Subject(
            Guid.NewGuid(),
            career.Id,
            "subject",
            "Subject",
            1,
            SubjectPeriod.Annual,
            CreatedAtUtc);
        var cycle = new AcademicCycle(
            Guid.NewGuid(),
            2026,
            AcademicCyclePeriod.Annual,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            CreatedAtUtc);
        var teacher = new Teacher(Guid.NewGuid(), "Ada", "Lovelace", "ada@example.com", CreatedAtUtc);
        var teacherAssignment = new TeacherSubjectAssignment(
            Guid.NewGuid(),
            teacher.Id,
            subject.Id,
            cycle.Id,
            "Titular",
            CreatedAtUtc);

        context.AddRange(career, subject, cycle, teacher, teacherAssignment);
        context.SaveChanges();

        return new AcademicFixture(career, subject, cycle, teacherAssignment);
    }

    private static SurveyFixture SeedSurvey(ApplicationDbContext context, SurveyStatus status)
    {
        var userId = Guid.NewGuid();
        var survey = new Survey(
            Guid.NewGuid(),
            userId,
            "Encuesta v1",
            "Descripcion v1",
            SurveyTarget.Student,
            CreatedAtUtc);
        survey.Update(survey.Title, survey.Description, survey.Target, isAnonymous: false, CreatedAtUtc.AddMinutes(1));

        var section = new SurveySection(
            Guid.NewGuid(),
            survey.Id,
            "Seccion activa",
            "Descripcion seccion",
            1,
            CreatedAtUtc);
        var inactiveSection = new SurveySection(
            Guid.NewGuid(),
            survey.Id,
            "Seccion inactiva",
            null,
            2,
            CreatedAtUtc);
        inactiveSection.Deactivate(CreatedAtUtc);

        var choiceQuestion = new SurveyQuestion(
            Guid.NewGuid(),
            section.Id,
            "Pregunta opcion",
            SurveyQuestionType.SingleChoice,
            isRequired: true,
            allowsComment: true,
            allowsOtherOption: true,
            order: 1,
            CreatedAtUtc);
        var activeOption = new SurveyQuestionOption(
            Guid.NewGuid(),
            choiceQuestion.Id,
            "Excelente",
            "excellent",
            1,
            CreatedAtUtc);
        var secondActiveOption = new SurveyQuestionOption(
            Guid.NewGuid(),
            choiceQuestion.Id,
            "Bueno",
            "good",
            2,
            CreatedAtUtc);
        var inactiveOption = new SurveyQuestionOption(
            Guid.NewGuid(),
            choiceQuestion.Id,
            "Regular",
            "regular",
            3,
            CreatedAtUtc);
        inactiveOption.Deactivate(CreatedAtUtc);
        choiceQuestion.AddOption(activeOption, CreatedAtUtc);
        choiceQuestion.AddOption(secondActiveOption, CreatedAtUtc);
        choiceQuestion.AddOption(inactiveOption, CreatedAtUtc);

        var matrixQuestion = new SurveyQuestion(
            Guid.NewGuid(),
            section.Id,
            "Pregunta matriz",
            SurveyQuestionType.MatrixSingleChoice,
            isRequired: true,
            allowsComment: false,
            allowsOtherOption: false,
            order: 2,
            CreatedAtUtc);
        matrixQuestion.AddOption(
            new SurveyQuestionOption(Guid.NewGuid(), matrixQuestion.Id, "Si", "yes", 1, CreatedAtUtc),
            CreatedAtUtc);
        matrixQuestion.AddOption(
            new SurveyQuestionOption(Guid.NewGuid(), matrixQuestion.Id, "No", "no", 2, CreatedAtUtc),
            CreatedAtUtc);
        var matrixRow = new SurveyMatrixRow(Guid.NewGuid(), matrixQuestion.Id, "Claridad", 1, CreatedAtUtc);
        var inactiveMatrixRow = new SurveyMatrixRow(Guid.NewGuid(), matrixQuestion.Id, "Organizacion", 2, CreatedAtUtc);
        inactiveMatrixRow.Deactivate(CreatedAtUtc);
        matrixQuestion.AddMatrixRow(matrixRow, CreatedAtUtc);
        matrixQuestion.AddMatrixRow(inactiveMatrixRow, CreatedAtUtc);

        var ratingQuestion = new SurveyQuestion(
            Guid.NewGuid(),
            section.Id,
            "Pregunta rating",
            SurveyQuestionType.RatingScale,
            isRequired: true,
            allowsComment: false,
            allowsOtherOption: false,
            order: 3,
            CreatedAtUtc,
            ratingMin: 1,
            ratingMax: 5);

        section.AddQuestion(choiceQuestion, CreatedAtUtc);
        section.AddQuestion(matrixQuestion, CreatedAtUtc);
        section.AddQuestion(ratingQuestion, CreatedAtUtc);
        survey.AddSection(section, CreatedAtUtc);
        survey.AddSection(inactiveSection, CreatedAtUtc);

        if (status is SurveyStatus.Published or SurveyStatus.Archived)
        {
            survey.Publish(CreatedAtUtc.AddMinutes(2));
        }

        if (status == SurveyStatus.Archived)
        {
            survey.Archive(CreatedAtUtc.AddMinutes(3));
        }

        context.Add(survey);
        context.SaveChanges();

        return new SurveyFixture(
            survey,
            section,
            choiceQuestion,
            activeOption,
            inactiveOption,
            matrixQuestion,
            matrixRow,
            inactiveMatrixRow,
            ratingQuestion);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private sealed class FixedAccessCodeGenerator : ISurveySessionAccessCodeGenerator
    {
        public string Generate() => Guid.NewGuid().ToString("N");
    }

    private sealed record SurveyFixture(
        Survey Survey,
        SurveySection Section,
        SurveyQuestion ChoiceQuestion,
        SurveyQuestionOption ActiveOption,
        SurveyQuestionOption InactiveOption,
        SurveyQuestion MatrixQuestion,
        SurveyMatrixRow MatrixRow,
        SurveyMatrixRow InactiveMatrixRow,
        SurveyQuestion RatingQuestion);

    private sealed record AcademicFixture(
        Career Career,
        Subject Subject,
        AcademicCycle Cycle,
        TeacherSubjectAssignment TeacherAssignment);
}
