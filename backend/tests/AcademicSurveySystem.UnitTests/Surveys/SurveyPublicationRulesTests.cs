using AcademicSurveySystem.Domain.Common;
using AcademicSurveySystem.Domain.Surveys.Entities;
using AcademicSurveySystem.Domain.Surveys.Enums;

namespace AcademicSurveySystem.UnitTests.Surveys;

public sealed class SurveyPublicationRulesTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset UpdatedAtUtc =
        new(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Publish_RejectsSurveyWithoutSections()
    {
        var survey = CreateSurvey();

        Assert.Throws<DomainException>(() => survey.Publish(UpdatedAtUtc));
    }

    [Fact]
    public void Publish_RejectsSectionWithoutQuestions()
    {
        var survey = CreateSurvey();
        survey.AddSection(CreateSection(survey.Id, order: 1), UpdatedAtUtc);

        Assert.Throws<DomainException>(() => survey.Publish(UpdatedAtUtc));
    }

    [Fact]
    public void Publish_RejectsSingleChoiceWithFewerThanTwoOptions()
    {
        var survey = CreateSurvey();
        var section = CreateSection(survey.Id, order: 1);
        var question = CreateQuestion(section.Id, SurveyQuestionType.SingleChoice, order: 1);
        question.AddOption(CreateOption(question.Id, order: 1), UpdatedAtUtc);
        section.AddQuestion(question, UpdatedAtUtc);
        survey.AddSection(section, UpdatedAtUtc);

        Assert.Throws<DomainException>(() => survey.Publish(UpdatedAtUtc));
    }

    [Fact]
    public void Publish_RejectsMatrixSingleChoiceWithoutRows()
    {
        var survey = CreateSurvey();
        var section = CreateSection(survey.Id, order: 1);
        var question = CreateQuestion(section.Id, SurveyQuestionType.MatrixSingleChoice, order: 1);
        question.AddOption(CreateOption(question.Id, order: 1), UpdatedAtUtc);
        question.AddOption(CreateOption(question.Id, order: 2), UpdatedAtUtc);
        section.AddQuestion(question, UpdatedAtUtc);
        survey.AddSection(section, UpdatedAtUtc);

        Assert.Throws<DomainException>(() => survey.Publish(UpdatedAtUtc));
    }

    [Fact]
    public void Publish_AllowsValidSurveyTemplate()
    {
        var survey = CreateSurvey();
        var section = CreateSection(survey.Id, order: 1);
        var question = CreateQuestion(section.Id, SurveyQuestionType.ShortText, order: 1);
        section.AddQuestion(question, UpdatedAtUtc);
        survey.AddSection(section, UpdatedAtUtc);

        survey.Publish(UpdatedAtUtc);

        Assert.Equal(SurveyStatus.Published, survey.Status);
    }

    private static Survey CreateSurvey()
    {
        return new Survey(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Encuesta",
            null,
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

    private static SurveyQuestion CreateQuestion(
        Guid sectionId,
        SurveyQuestionType type,
        int order)
    {
        return new SurveyQuestion(
            Guid.NewGuid(),
            sectionId,
            "Pregunta",
            type,
            isRequired: true,
            allowsComment: false,
            allowsOtherOption: false,
            order,
            CreatedAtUtc);
    }

    private static SurveyQuestionOption CreateOption(Guid questionId, int order)
    {
        return new SurveyQuestionOption(
            Guid.NewGuid(),
            questionId,
            $"Opcion {order}",
            $"opcion-{order}",
            order,
            CreatedAtUtc);
    }
}
