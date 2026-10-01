using AcademicSurveySystem.Domain.Common;
using AcademicSurveySystem.Domain.Surveys.Entities;
using AcademicSurveySystem.Domain.Surveys.Enums;

namespace AcademicSurveySystem.UnitTests.Surveys;

public sealed class SurveyTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset UpdatedAtUtc =
        new(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CreatesSurvey_WhenValuesAreValid()
    {
        var survey = CreateSurvey();

        Assert.Equal("Encuesta inicial", survey.Title);
        Assert.Equal("Descripcion", survey.Description);
        Assert.Equal(SurveyTarget.Student, survey.Target);
        Assert.Equal(SurveyStatus.Draft, survey.Status);
        Assert.True(survey.IsAnonymous);
        Assert.True(survey.IsActive);
        Assert.Equal(CreatedAtUtc, survey.CreatedAtUtc);
        Assert.Equal(CreatedAtUtc, survey.UpdatedAtUtc);
    }

    [Fact]
    public void Constructor_RejectsEmptyCreatedByUserId()
    {
        Assert.Throws<DomainException>(() => CreateSurvey(createdByUserId: Guid.Empty));
    }

    [Fact]
    public void Constructor_RejectsEmptyTitle()
    {
        Assert.Throws<DomainException>(() => CreateSurvey(title: " "));
    }

    [Fact]
    public void Constructor_SetsInitialStatusToDraft()
    {
        var survey = CreateSurvey();

        Assert.Equal(SurveyStatus.Draft, survey.Status);
    }

    [Fact]
    public void Publish_ChangesStatus()
    {
        var survey = CreateSurvey();
        AddPublishableShortTextQuestion(survey);

        survey.Publish(UpdatedAtUtc);

        Assert.Equal(SurveyStatus.Published, survey.Status);
        Assert.Equal(UpdatedAtUtc, survey.UpdatedAtUtc);
    }

    [Fact]
    public void Archive_ChangesStatus()
    {
        var survey = CreateSurvey();
        AddPublishableShortTextQuestion(survey);
        survey.Publish(UpdatedAtUtc);

        survey.Archive(UpdatedAtUtc.AddDays(1));

        Assert.Equal(SurveyStatus.Archived, survey.Status);
    }

    [Fact]
    public void ActivateAndDeactivate_UpdateState()
    {
        var survey = CreateSurvey();

        survey.Deactivate(UpdatedAtUtc);
        Assert.False(survey.IsActive);

        survey.Activate(UpdatedAtUtc.AddDays(1));
        Assert.True(survey.IsActive);
    }

    [Fact]
    public void AddSection_AddsSectionAndUpdatesTimestamp()
    {
        var survey = CreateSurvey();
        var section = CreateSection(survey.Id, order: 1);

        survey.AddSection(section, UpdatedAtUtc);

        Assert.Single(survey.Sections);
        Assert.Equal(UpdatedAtUtc, survey.UpdatedAtUtc);
    }

    [Fact]
    public void AddSection_RejectsDuplicatedOrder()
    {
        var survey = CreateSurvey();

        survey.AddSection(CreateSection(survey.Id, order: 1), UpdatedAtUtc);

        Assert.Throws<DomainException>(() =>
            survey.AddSection(CreateSection(survey.Id, order: 1), UpdatedAtUtc.AddDays(1)));
    }

    [Fact]
    public void Update_UpdatesUpdatedAtUtc()
    {
        var survey = CreateSurvey();

        survey.Update(
            "Encuesta actualizada",
            null,
            SurveyTarget.Institutional,
            isAnonymous: false,
            UpdatedAtUtc);

        Assert.Equal("Encuesta actualizada", survey.Title);
        Assert.Null(survey.Description);
        Assert.Equal(SurveyTarget.Institutional, survey.Target);
        Assert.False(survey.IsAnonymous);
        Assert.Equal(UpdatedAtUtc, survey.UpdatedAtUtc);
    }

    private static Survey CreateSurvey(
        Guid? createdByUserId = null,
        string title = "Encuesta inicial")
    {
        return new Survey(
            Guid.NewGuid(),
            createdByUserId ?? Guid.NewGuid(),
            title,
            "Descripcion",
            SurveyTarget.Student,
            CreatedAtUtc);
    }

    private static SurveySection CreateSection(Guid surveyId, int order)
    {
        return new SurveySection(
            Guid.NewGuid(),
            surveyId,
            "Seccion",
            null,
            order,
            CreatedAtUtc);
    }

    private static void AddPublishableShortTextQuestion(Survey survey)
    {
        var section = CreateSection(survey.Id, order: 1);
        var question = new SurveyQuestion(
            Guid.NewGuid(),
            section.Id,
            "Pregunta abierta",
            SurveyQuestionType.ShortText,
            isRequired: true,
            allowsComment: false,
            allowsOtherOption: false,
            order: 1,
            CreatedAtUtc);

        section.AddQuestion(question, UpdatedAtUtc);
        survey.AddSection(section, UpdatedAtUtc);
    }
}
