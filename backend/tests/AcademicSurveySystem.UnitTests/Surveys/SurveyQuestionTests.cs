using AcademicSurveySystem.Domain.Common;
using AcademicSurveySystem.Domain.Surveys.Entities;
using AcademicSurveySystem.Domain.Surveys.Enums;

namespace AcademicSurveySystem.UnitTests.Surveys;

public sealed class SurveyQuestionTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset UpdatedAtUtc =
        new(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CreatesQuestion_WhenValuesAreValid()
    {
        var question = CreateQuestion();

        Assert.Equal("Pregunta", question.Text);
        Assert.Equal(SurveyQuestionType.SingleChoice, question.Type);
        Assert.True(question.IsRequired);
        Assert.False(question.AllowsComment);
        Assert.False(question.AllowsOtherOption);
        Assert.Equal(1, question.Order);
        Assert.True(question.IsActive);
    }

    [Fact]
    public void Constructor_RejectsEmptySurveySectionId()
    {
        Assert.Throws<DomainException>(() => CreateQuestion(sectionId: Guid.Empty));
    }

    [Fact]
    public void Constructor_RejectsEmptyText()
    {
        Assert.Throws<DomainException>(() => CreateQuestion(text: " "));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsInvalidOrder(int order)
    {
        Assert.Throws<DomainException>(() => CreateQuestion(order: order));
    }

    [Theory]
    [InlineData(SurveyQuestionType.SingleChoice)]
    [InlineData(SurveyQuestionType.MultipleChoice)]
    public void AddOption_AllowsChoiceQuestions(SurveyQuestionType type)
    {
        var question = CreateQuestion(type: type);

        question.AddOption(CreateOption(question.Id, order: 1), UpdatedAtUtc);

        Assert.Single(question.Options);
    }

    [Theory]
    [InlineData(SurveyQuestionType.ShortText)]
    [InlineData(SurveyQuestionType.LongText)]
    [InlineData(SurveyQuestionType.RatingScale)]
    public void AddOption_RejectsQuestionTypesWithoutManualOptions(SurveyQuestionType type)
    {
        var question = CreateQuestion(type: type);

        Assert.Throws<DomainException>(() =>
            question.AddOption(CreateOption(question.Id, order: 1), UpdatedAtUtc));
    }

    [Fact]
    public void AddMatrixRow_AllowsMatrixSingleChoice()
    {
        var question = CreateQuestion(type: SurveyQuestionType.MatrixSingleChoice);

        question.AddMatrixRow(CreateMatrixRow(question.Id, order: 1), UpdatedAtUtc);

        Assert.Single(question.MatrixRows);
    }

    [Fact]
    public void AddOption_RejectsDuplicatedOrder()
    {
        var question = CreateQuestion();

        question.AddOption(CreateOption(question.Id, order: 1), UpdatedAtUtc);

        Assert.Throws<DomainException>(() =>
            question.AddOption(CreateOption(question.Id, order: 1), UpdatedAtUtc.AddDays(1)));
    }

    [Fact]
    public void AddMatrixRow_RejectsDuplicatedOrder()
    {
        var question = CreateQuestion(type: SurveyQuestionType.MatrixSingleChoice);

        question.AddMatrixRow(CreateMatrixRow(question.Id, order: 1), UpdatedAtUtc);

        Assert.Throws<DomainException>(() =>
            question.AddMatrixRow(CreateMatrixRow(question.Id, order: 1), UpdatedAtUtc.AddDays(1)));
    }

    private static SurveyQuestion CreateQuestion(
        Guid? sectionId = null,
        string text = "Pregunta",
        SurveyQuestionType type = SurveyQuestionType.SingleChoice,
        int order = 1)
    {
        return new SurveyQuestion(
            Guid.NewGuid(),
            sectionId ?? Guid.NewGuid(),
            text,
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
            "Opcion",
            "opcion",
            order,
            CreatedAtUtc);
    }

    private static SurveyMatrixRow CreateMatrixRow(Guid questionId, int order)
    {
        return new SurveyMatrixRow(
            Guid.NewGuid(),
            questionId,
            "Fila",
            order,
            CreatedAtUtc);
    }
}
