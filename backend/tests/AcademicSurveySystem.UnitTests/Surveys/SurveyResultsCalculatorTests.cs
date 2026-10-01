using AcademicSurveySystem.Domain.Surveys.Enums;
using AcademicSurveySystem.Infrastructure.Surveys.Results;

namespace AcademicSurveySystem.UnitTests.Surveys;

public sealed class SurveyResultsCalculatorTests
{
    [Fact]
    public void SummaryWithoutResponses_CanRepresentZeroTotals()
    {
        var result = SurveyResultsCalculator.BuildQuestionResults(
            [CreateChoiceQuestion()],
            [],
            [],
            []);

        var question = Assert.Single(result);
        Assert.Equal(0, question.ResponseCount);
        Assert.All(question.Choice!.Options, option =>
        {
            Assert.Equal(0, option.Count);
            Assert.Equal(0, option.Percentage);
        });
    }

    [Fact]
    public void SingleChoice_CountsOptionsAndPercentages()
    {
        var question = CreateChoiceQuestion();
        var answer1 = CreateAnswer(question.Id);
        var answer2 = CreateAnswer(question.Id);

        var result = SurveyResultsCalculator.BuildQuestionResult(
            question,
            [answer1, answer2],
            [
                new SurveyResultsCalculator.OptionSelectionRecord(answer1.Id, question.Options.First().Id),
                new SurveyResultsCalculator.OptionSelectionRecord(answer2.Id, question.Options.First().Id)
            ],
            []);

        var firstOption = result.Choice!.Options.First();
        var secondOption = result.Choice.Options.Last();

        Assert.Equal(2, firstOption.Count);
        Assert.Equal(100, firstOption.Percentage);
        Assert.Equal(0, secondOption.Count);
        Assert.Equal(0, secondOption.Percentage);
    }

    [Fact]
    public void SingleChoice_CountsOtherTextSeparately()
    {
        var question = CreateChoiceQuestion(allowsOtherOption: true);
        var optionAnswer = CreateAnswer(question.Id);
        var otherAnswer = CreateAnswer(question.Id, otherText: "Otra opcion");

        var result = SurveyResultsCalculator.BuildQuestionResult(
            question,
            [optionAnswer, otherAnswer],
            [
                new SurveyResultsCalculator.OptionSelectionRecord(optionAnswer.Id, question.Options.First().Id)
            ],
            []);

        Assert.Equal(2, result.ResponseCount);
        Assert.Equal(50, result.Choice!.Options.First().Percentage);
        Assert.Equal(1, result.Choice.Other!.Count);
        Assert.Equal(50, result.Choice.Other.Percentage);
        Assert.Equal(["Otra opcion"], result.Choice.Other.Values);
    }

    [Fact]
    public void MultipleChoice_UsesRespondentsAsPercentageDenominator()
    {
        var question = CreateChoiceQuestion(SurveyQuestionType.MultipleChoice);
        var answer1 = CreateAnswer(question.Id);
        var answer2 = CreateAnswer(question.Id);
        var optionA = question.Options.First();
        var optionB = question.Options.Last();

        var result = SurveyResultsCalculator.BuildQuestionResult(
            question,
            [answer1, answer2],
            [
                new SurveyResultsCalculator.OptionSelectionRecord(answer1.Id, optionA.Id),
                new SurveyResultsCalculator.OptionSelectionRecord(answer1.Id, optionB.Id),
                new SurveyResultsCalculator.OptionSelectionRecord(answer2.Id, optionA.Id)
            ],
            []);

        Assert.Equal(100, result.Choice!.Options.Single(option => option.OptionId == optionA.Id).Percentage);
        Assert.Equal(50, result.Choice.Options.Single(option => option.OptionId == optionB.Id).Percentage);
    }

    [Fact]
    public void MultipleChoice_CountsOtherTextUsingRespondentsAsDenominator()
    {
        var question = CreateChoiceQuestion(SurveyQuestionType.MultipleChoice, allowsOtherOption: true);
        var answer1 = CreateAnswer(question.Id);
        var answer2 = CreateAnswer(question.Id, otherText: "Otra opcion multiple");
        var optionA = question.Options.First();
        var optionB = question.Options.Last();

        var result = SurveyResultsCalculator.BuildQuestionResult(
            question,
            [answer1, answer2],
            [
                new SurveyResultsCalculator.OptionSelectionRecord(answer1.Id, optionA.Id),
                new SurveyResultsCalculator.OptionSelectionRecord(answer2.Id, optionB.Id)
            ],
            []);

        Assert.Equal(2, result.ResponseCount);
        Assert.Equal(50, result.Choice!.Other!.Percentage);
        Assert.Equal(["Otra opcion multiple"], result.Choice.Other.Values);
    }

    [Fact]
    public void ShortText_ReturnsValues()
    {
        var question = CreateTextQuestion(SurveyQuestionType.ShortText);

        var result = SurveyResultsCalculator.BuildQuestionResult(
            question,
            [CreateAnswer(question.Id, textValue: "Claro")],
            [],
            []);

        Assert.Equal(["Claro"], result.TextValues!.Values);
    }

    [Fact]
    public void LongText_ReturnsValues()
    {
        var question = CreateTextQuestion(SurveyQuestionType.LongText);

        var result = SurveyResultsCalculator.BuildQuestionResult(
            question,
            [CreateAnswer(question.Id, textValue: "Muy buen desempeno")],
            [],
            []);

        Assert.Equal(["Muy buen desempeno"], result.TextValues!.Values);
    }

    [Fact]
    public void RatingScale_CalculatesAverageMinMaxAndDistribution()
    {
        var question = CreateRatingQuestion();

        var result = SurveyResultsCalculator.BuildQuestionResult(
            question,
            [
                CreateAnswer(question.Id, numericValue: 3),
                CreateAnswer(question.Id, numericValue: 5),
                CreateAnswer(question.Id, numericValue: 5)
            ],
            [],
            []);

        Assert.Equal(3, result.Rating!.ResponseCount);
        Assert.Equal(4.33m, result.Rating.Average);
        Assert.Equal(1, result.Rating.ConfiguredMinimum);
        Assert.Equal(5, result.Rating.ConfiguredMaximum);
        Assert.Equal(3, result.Rating.MinimumObserved);
        Assert.Equal(5, result.Rating.MaximumObserved);
        Assert.Equal(5, result.Rating.Distribution.Count);
        Assert.Equal(0, result.Rating.Distribution.Single(item => item.Value == 1).Count);
        Assert.Equal(0, result.Rating.Distribution.Single(item => item.Value == 2).Count);
        Assert.Equal(66.67m, result.Rating.Distribution.Single(item => item.Value == 5).Percentage);
    }

    [Fact]
    public void RatingScale_WithoutConfiguredRange_UsesObservedValuesForLegacyQuestions()
    {
        var question = CreateRatingQuestion(ratingMin: null, ratingMax: null);

        var result = SurveyResultsCalculator.BuildQuestionResult(
            question,
            [
                CreateAnswer(question.Id, numericValue: 3),
                CreateAnswer(question.Id, numericValue: 5)
            ],
            [],
            []);

        Assert.Null(result.Rating!.ConfiguredMinimum);
        Assert.Null(result.Rating.ConfiguredMaximum);
        Assert.Equal([3, 5], result.Rating.Distribution.Select(item => item.Value).ToArray());
    }

    [Fact]
    public void MatrixSingleChoice_AggregatesByRowAndOption()
    {
        var question = CreateMatrixQuestion();
        var answer1 = CreateAnswer(question.Id);
        var answer2 = CreateAnswer(question.Id);
        var row = question.MatrixRows.First();
        var option = question.Options.First();

        var result = SurveyResultsCalculator.BuildQuestionResult(
            question,
            [answer1, answer2],
            [],
            [
                new SurveyResultsCalculator.MatrixSelectionRecord(answer1.Id, row.Id, option.Id),
                new SurveyResultsCalculator.MatrixSelectionRecord(answer2.Id, row.Id, option.Id)
            ]);

        var rowResult = result.Matrix!.Rows.First();

        Assert.Equal(2, rowResult.TotalResponses);
        Assert.Equal(2, rowResult.Options.First().Count);
        Assert.Equal(100, rowResult.Options.First().Percentage);
    }

    [Fact]
    public void Comments_ReturnsOnlyNonEmptyCommentsWhenAllowed()
    {
        var question = CreateChoiceQuestion(allowsComment: true);

        var result = SurveyResultsCalculator.BuildQuestionResult(
            question,
            [
                CreateAnswer(question.Id, comment: "Buen curso"),
                CreateAnswer(question.Id, comment: " ")
            ],
            [],
            []);

        var comment = Assert.Single(result.Comments);
        Assert.Equal("Buen curso", comment.Comment);
    }

    [Fact]
    public void Comments_AreNotReturnedWhenQuestionDoesNotAllowComments()
    {
        var question = CreateChoiceQuestion(allowsComment: false);

        var result = SurveyResultsCalculator.BuildQuestionResult(
            question,
            [CreateAnswer(question.Id, comment: "No deberia aparecer")],
            [],
            []);

        Assert.Empty(result.Comments);
    }

    private static SurveyResultsCalculator.QuestionTemplate CreateChoiceQuestion(
        SurveyQuestionType type = SurveyQuestionType.SingleChoice,
        bool allowsComment = false,
        bool allowsOtherOption = false)
    {
        return new SurveyResultsCalculator.QuestionTemplate(
            Guid.NewGuid(),
            "Pregunta",
            type,
            allowsComment,
            1,
            [
                new SurveyResultsCalculator.OptionTemplate(Guid.NewGuid(), "A", "a", 1),
                new SurveyResultsCalculator.OptionTemplate(Guid.NewGuid(), "B", "b", 2)
            ],
            [],
            AllowsOtherOption: allowsOtherOption);
    }

    private static SurveyResultsCalculator.QuestionTemplate CreateTextQuestion(SurveyQuestionType type)
    {
        return new SurveyResultsCalculator.QuestionTemplate(
            Guid.NewGuid(),
            "Pregunta texto",
            type,
            false,
            1,
            [],
            []);
    }

    private static SurveyResultsCalculator.QuestionTemplate CreateRatingQuestion(
        int? ratingMin = 1,
        int? ratingMax = 5)
    {
        return new SurveyResultsCalculator.QuestionTemplate(
            Guid.NewGuid(),
            "Rating",
            SurveyQuestionType.RatingScale,
            false,
            1,
            [],
            [],
            RatingMin: ratingMin,
            RatingMax: ratingMax);
    }

    private static SurveyResultsCalculator.QuestionTemplate CreateMatrixQuestion()
    {
        return new SurveyResultsCalculator.QuestionTemplate(
            Guid.NewGuid(),
            "Matriz",
            SurveyQuestionType.MatrixSingleChoice,
            false,
            1,
            [
                new SurveyResultsCalculator.OptionTemplate(Guid.NewGuid(), "Bueno", "good", 1),
                new SurveyResultsCalculator.OptionTemplate(Guid.NewGuid(), "Excelente", "excellent", 2)
            ],
            [
                new SurveyResultsCalculator.MatrixRowTemplate(Guid.NewGuid(), "Claridad", 1),
                new SurveyResultsCalculator.MatrixRowTemplate(Guid.NewGuid(), "Organizacion", 2)
            ]);
    }

    private static SurveyResultsCalculator.AnswerRecord CreateAnswer(
        Guid questionId,
        string? textValue = null,
        int? numericValue = null,
        string? comment = null,
        string? otherText = null)
    {
        return new SurveyResultsCalculator.AnswerRecord(
            Guid.NewGuid(),
            questionId,
            textValue,
            numericValue,
            comment,
            otherText);
    }
}
