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

    [Fact]
    public void Constructor_CreatesRatingScaleQuestionWithBounds()
    {
        var question = CreateQuestion(type: SurveyQuestionType.RatingScale);

        Assert.Equal(1, question.RatingMin);
        Assert.Equal(5, question.RatingMax);
        Assert.True(question.HasValidRatingBounds());
    }

    [Fact]
    public void Constructor_RejectsRatingScaleWithoutBounds()
    {
        Assert.Throws<DomainException>(() => new SurveyQuestion(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Pregunta",
            SurveyQuestionType.RatingScale,
            isRequired: true,
            allowsComment: false,
            allowsOtherOption: false,
            order: 1,
            CreatedAtUtc));
    }

    [Fact]
    public void Constructor_RejectsRatingBoundsForOtherQuestionTypes()
    {
        Assert.Throws<DomainException>(() => new SurveyQuestion(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Pregunta",
            SurveyQuestionType.ShortText,
            isRequired: true,
            allowsComment: false,
            allowsOtherOption: false,
            order: 1,
            CreatedAtUtc,
            ratingMin: 1,
            ratingMax: 5));
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

    [Fact]
    public void UpdateOption_UpdatesExistingOption()
    {
        var question = CreateQuestion(type: SurveyQuestionType.SingleChoice);
        var option = CreateOption(question.Id, order: 1);
        question.AddOption(option, UpdatedAtUtc);

        question.UpdateOption(
            option.Id,
            "Nueva opcion",
            "NUEVA",
            order: 2,
            UpdatedAtUtc.AddDays(1));

        Assert.Equal("Nueva opcion", option.Text);
        Assert.Equal("nueva", option.Value);
        Assert.Equal(2, option.Order);
    }

    [Fact]
    public void UpdateOption_RejectsDuplicatedOrder()
    {
        var question = CreateQuestion(type: SurveyQuestionType.SingleChoice);
        var option = CreateOption(question.Id, order: 1);
        question.AddOption(option, UpdatedAtUtc);
        question.AddOption(CreateOption(question.Id, order: 2), UpdatedAtUtc);

        Assert.Throws<DomainException>(() =>
            question.UpdateOption(
                option.Id,
                "Nueva opcion",
                "nueva",
                order: 2,
                UpdatedAtUtc.AddDays(1)));
    }

    [Fact]
    public void UpdateOption_RejectsQuestionTypesWithoutManualOptions()
    {
        var question = CreateQuestion(type: SurveyQuestionType.ShortText);

        Assert.Throws<DomainException>(() =>
            question.UpdateOption(
                Guid.NewGuid(),
                "Nueva opcion",
                "nueva",
                order: 1,
                UpdatedAtUtc.AddDays(1)));
    }

    [Fact]
    public void UpdateMatrixRow_UpdatesExistingRow()
    {
        var question = CreateQuestion(type: SurveyQuestionType.MatrixSingleChoice);
        var matrixRow = CreateMatrixRow(question.Id, order: 1);
        question.AddMatrixRow(matrixRow, UpdatedAtUtc);

        question.UpdateMatrixRow(
            matrixRow.Id,
            "Nueva fila",
            order: 2,
            UpdatedAtUtc.AddDays(1));

        Assert.Equal("Nueva fila", matrixRow.Text);
        Assert.Equal(2, matrixRow.Order);
    }

    [Fact]
    public void UpdateMatrixRow_RejectsDuplicatedOrder()
    {
        var question = CreateQuestion(type: SurveyQuestionType.MatrixSingleChoice);
        var matrixRow = CreateMatrixRow(question.Id, order: 1);
        question.AddMatrixRow(matrixRow, UpdatedAtUtc);
        question.AddMatrixRow(CreateMatrixRow(question.Id, order: 2), UpdatedAtUtc);

        Assert.Throws<DomainException>(() =>
            question.UpdateMatrixRow(
                matrixRow.Id,
                "Nueva fila",
                order: 2,
                UpdatedAtUtc.AddDays(1)));
    }

    [Fact]
    public void UpdateMatrixRow_RejectsNonMatrixQuestion()
    {
        var question = CreateQuestion(type: SurveyQuestionType.SingleChoice);

        Assert.Throws<DomainException>(() =>
            question.UpdateMatrixRow(
                Guid.NewGuid(),
                "Nueva fila",
                order: 1,
                UpdatedAtUtc.AddDays(1)));
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
            CreatedAtUtc,
            type == SurveyQuestionType.RatingScale ? 1 : null,
            type == SurveyQuestionType.RatingScale ? 5 : null);
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
