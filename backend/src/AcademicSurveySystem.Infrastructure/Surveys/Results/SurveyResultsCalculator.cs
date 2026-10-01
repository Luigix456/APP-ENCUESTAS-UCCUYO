using AcademicSurveySystem.Application.Surveys.Results;
using AcademicSurveySystem.Domain.Surveys.Enums;

namespace AcademicSurveySystem.Infrastructure.Surveys.Results;

public static class SurveyResultsCalculator
{
    public sealed record QuestionTemplate(
        Guid Id,
        string Text,
        SurveyQuestionType Type,
        bool AllowsComment,
        int Order,
        IReadOnlyCollection<OptionTemplate> Options,
        IReadOnlyCollection<MatrixRowTemplate> MatrixRows,
        bool AllowsOtherOption = false,
        int? RatingMin = null,
        int? RatingMax = null);

    public sealed record OptionTemplate(
        Guid Id,
        string Text,
        string Value,
        int Order);

    public sealed record MatrixRowTemplate(
        Guid Id,
        string Text,
        int Order);

    public sealed record AnswerRecord(
        Guid Id,
        Guid QuestionId,
        string? TextValue,
        int? NumericValue,
        string? Comment,
        string? OtherText = null);

    public sealed record OptionSelectionRecord(
        Guid AnswerId,
        Guid OptionId);

    public sealed record MatrixSelectionRecord(
        Guid AnswerId,
        Guid RowId,
        Guid OptionId);

    public static IReadOnlyCollection<SurveyQuestionResultsDto> BuildQuestionResults(
        IReadOnlyCollection<QuestionTemplate> questions,
        IReadOnlyCollection<AnswerRecord> answers,
        IReadOnlyCollection<OptionSelectionRecord> optionSelections,
        IReadOnlyCollection<MatrixSelectionRecord> matrixSelections)
    {
        return questions
            .OrderBy(question => question.Order)
            .Select(question => BuildQuestionResult(question, answers, optionSelections, matrixSelections))
            .ToArray();
    }

    public static SurveyQuestionResultsDto BuildQuestionResult(
        QuestionTemplate question,
        IReadOnlyCollection<AnswerRecord> answers,
        IReadOnlyCollection<OptionSelectionRecord> optionSelections,
        IReadOnlyCollection<MatrixSelectionRecord> matrixSelections)
    {
        var questionAnswers = answers
            .Where(answer => answer.QuestionId == question.Id)
            .ToArray();

        var comments = question.AllowsComment
            ? questionAnswers
                .Where(answer => !string.IsNullOrWhiteSpace(answer.Comment))
                .Select(answer => new SurveyQuestionCommentDto(answer.Id, answer.Comment!.Trim()))
                .ToArray()
            : [];

        return question.Type switch
        {
            SurveyQuestionType.SingleChoice or SurveyQuestionType.MultipleChoice =>
                BuildChoiceQuestionResult(question, questionAnswers, optionSelections, comments),

            SurveyQuestionType.ShortText or SurveyQuestionType.LongText =>
                BuildTextQuestionResult(question, questionAnswers, comments),

            SurveyQuestionType.RatingScale =>
                BuildRatingQuestionResult(question, questionAnswers, comments),

            SurveyQuestionType.MatrixSingleChoice =>
                BuildMatrixQuestionResult(question, questionAnswers, matrixSelections, comments),

            _ => new SurveyQuestionResultsDto(
                question.Id,
                question.Text,
                question.Type.ToString(),
                question.Order,
                questionAnswers.Length,
                null,
                null,
                null,
                null,
                comments)
        };
    }

    private static SurveyQuestionResultsDto BuildChoiceQuestionResult(
        QuestionTemplate question,
        IReadOnlyCollection<AnswerRecord> questionAnswers,
        IReadOnlyCollection<OptionSelectionRecord> optionSelections,
        IReadOnlyCollection<SurveyQuestionCommentDto> comments)
    {
        var answerIds = questionAnswers.Select(answer => answer.Id).ToHashSet();
        var selections = optionSelections
            .Where(selection => answerIds.Contains(selection.AnswerId))
            .ToArray();
        var denominator = questionAnswers.Count;

        var options = question.Options
            .OrderBy(option => option.Order)
            .Select(option =>
            {
                var count = selections.Count(selection => selection.OptionId == option.Id);
                return new SurveyOptionDistributionDto(
                    option.Id,
                    option.Text,
                    option.Value,
                    count,
                    CalculatePercentage(count, denominator));
            })
            .ToArray();

        var otherValues = question.AllowsOtherOption
            ? questionAnswers
                .Where(answer => !string.IsNullOrWhiteSpace(answer.OtherText))
                .Select(answer => answer.OtherText!.Trim())
                .ToArray()
            : [];

        var other = question.AllowsOtherOption
            ? new SurveyOtherOptionResultsDto(
                otherValues.Length,
                CalculatePercentage(otherValues.Length, denominator),
                otherValues)
            : null;

        return new SurveyQuestionResultsDto(
            question.Id,
            question.Text,
            question.Type.ToString(),
            question.Order,
            denominator,
            new SurveyChoiceResultsDto(options, other),
            null,
            null,
            null,
            comments);
    }

    private static SurveyQuestionResultsDto BuildTextQuestionResult(
        QuestionTemplate question,
        IReadOnlyCollection<AnswerRecord> questionAnswers,
        IReadOnlyCollection<SurveyQuestionCommentDto> comments)
    {
        var values = questionAnswers
            .Where(answer => !string.IsNullOrWhiteSpace(answer.TextValue))
            .Select(answer => answer.TextValue!.Trim())
            .ToArray();

        return new SurveyQuestionResultsDto(
            question.Id,
            question.Text,
            question.Type.ToString(),
            question.Order,
            values.Length,
            null,
            new SurveyTextResultsDto(values.Length, values),
            null,
            null,
            comments);
    }

    private static SurveyQuestionResultsDto BuildRatingQuestionResult(
        QuestionTemplate question,
        IReadOnlyCollection<AnswerRecord> questionAnswers,
        IReadOnlyCollection<SurveyQuestionCommentDto> comments)
    {
        var values = questionAnswers
            .Where(answer => answer.NumericValue is not null)
            .Select(answer => answer.NumericValue!.Value)
            .ToArray();

        var valueCounts = values
            .GroupBy(value => value)
            .ToDictionary(group => group.Key, group => group.Count());

        var distributionValues = question.RatingMin is not null
            && question.RatingMax is not null
            && question.RatingMin.Value <= question.RatingMax.Value
                ? Enumerable.Range(
                    question.RatingMin.Value,
                    question.RatingMax.Value - question.RatingMin.Value + 1)
                    .ToArray()
                : valueCounts.Keys.Order().ToArray();

        var distribution = distributionValues
            .Select(value =>
            {
                var count = valueCounts.TryGetValue(value, out var existingCount)
                    ? existingCount
                    : 0;

                return new SurveyRatingDistributionDto(
                    value,
                    count,
                    CalculatePercentage(count, values.Length));
            })
            .ToArray();

        return new SurveyQuestionResultsDto(
            question.Id,
            question.Text,
            question.Type.ToString(),
            question.Order,
            values.Length,
            null,
            null,
            new SurveyRatingResultsDto(
                values.Length,
                values.Length == 0 ? null : Math.Round((decimal)values.Average(), 2),
                values.Length == 0 ? null : values.Min(),
                values.Length == 0 ? null : values.Max(),
                distribution,
                question.RatingMin,
                question.RatingMax),
            null,
            comments);
    }

    private static SurveyQuestionResultsDto BuildMatrixQuestionResult(
        QuestionTemplate question,
        IReadOnlyCollection<AnswerRecord> questionAnswers,
        IReadOnlyCollection<MatrixSelectionRecord> matrixSelections,
        IReadOnlyCollection<SurveyQuestionCommentDto> comments)
    {
        var answerIds = questionAnswers.Select(answer => answer.Id).ToHashSet();
        var selections = matrixSelections
            .Where(selection => answerIds.Contains(selection.AnswerId))
            .ToArray();

        var rows = question.MatrixRows
            .OrderBy(row => row.Order)
            .Select(row =>
            {
                var rowSelections = selections
                    .Where(selection => selection.RowId == row.Id)
                    .ToArray();

                var options = question.Options
                    .OrderBy(option => option.Order)
                    .Select(option =>
                    {
                        var count = rowSelections.Count(selection => selection.OptionId == option.Id);
                        return new SurveyOptionDistributionDto(
                            option.Id,
                            option.Text,
                            option.Value,
                            count,
                            CalculatePercentage(count, rowSelections.Length));
                    })
                    .ToArray();

                return new SurveyMatrixRowResultsDto(
                    row.Id,
                    row.Text,
                    rowSelections.Length,
                    options);
            })
            .ToArray();

        var responseCount = questionAnswers.Count;

        return new SurveyQuestionResultsDto(
            question.Id,
            question.Text,
            question.Type.ToString(),
            question.Order,
            responseCount,
            null,
            null,
            null,
            new SurveyMatrixResultsDto(rows),
            comments);
    }

    private static decimal CalculatePercentage(int numerator, int denominator)
    {
        return denominator == 0
            ? 0
            : Math.Round((decimal)numerator / denominator * 100, 2);
    }
}
