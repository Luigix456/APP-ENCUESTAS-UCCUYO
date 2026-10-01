using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Surveys.Responses;
using AcademicSurveySystem.Domain.Surveys.Entities;
using AcademicSurveySystem.Domain.Surveys.Enums;

namespace AcademicSurveySystem.Infrastructure.Surveys;

public static class SurveyResponseValidator
{
    public static IReadOnlyCollection<ApplicationError> ValidateSessionForSubmission(
        SurveySession session,
        DateTimeOffset nowUtc)
    {
        var errors = new List<ApplicationError>();

        if (!session.IsAvailable(nowUtc))
        {
            errors.Add(new ApplicationError(
                "SurveySession.NotAvailable",
                "Survey session is not available."));
        }

        var assignment = session.SurveyAssignment;
        var survey = assignment.Survey;

        if (!assignment.IsActive)
        {
            errors.Add(new ApplicationError(
                "SurveyResponse.AssignmentInactive",
                "Survey assignment is not active."));
        }

        if (!survey.IsActive)
        {
            errors.Add(new ApplicationError(
                "SurveyResponse.SurveyInactive",
                "Survey is not active."));
        }

        if (survey.Status != SurveyStatus.Published)
        {
            errors.Add(new ApplicationError(
                "SurveyResponse.SurveyNotPublished",
                "Survey must be published."));
        }

        return errors;
    }

    public static IReadOnlyCollection<ApplicationError> Validate(
        SubmitSurveyResponseRequest request,
        Survey survey)
    {
        var errors = request.Validate().ToList();

        if (errors.Count > 0)
        {
            return errors;
        }

        var answers = request.Answers ?? Array.Empty<SubmitSurveyAnswerRequest>();
        var activeQuestions = survey.Sections
            .Where(section => section.IsActive)
            .SelectMany(section => section.Questions)
            .Where(question => question.IsActive)
            .ToDictionary(question => question.Id);

        var allQuestions = survey.Sections
            .SelectMany(section => section.Questions)
            .ToDictionary(question => question.Id);

        foreach (var group in answers.GroupBy(answer => answer.QuestionId))
        {
            if (group.Key != Guid.Empty && group.Count() > 1)
            {
                errors.Add(new ApplicationError(
                    "SurveyResponse.DuplicateQuestion",
                    "Question cannot be answered more than once."));
            }
        }

        foreach (var question in activeQuestions.Values.Where(question => question.IsRequired))
        {
            if (!answers.Any(answer => answer.QuestionId == question.Id))
            {
                errors.Add(new ApplicationError(
                    "SurveyResponse.RequiredQuestionMissing",
                    "A required question is missing."));
            }
        }

        foreach (var answer in answers)
        {
            if (answer.QuestionId == Guid.Empty)
            {
                continue;
            }

            if (!allQuestions.TryGetValue(answer.QuestionId, out var question)
                || !activeQuestions.ContainsKey(answer.QuestionId))
            {
                errors.Add(new ApplicationError(
                    "SurveyResponse.InvalidQuestion",
                    "Question is invalid."));
                continue;
            }

            if (!string.IsNullOrWhiteSpace(answer.Comment) && !question.AllowsComment)
            {
                errors.Add(new ApplicationError(
                    "SurveyResponse.CommentNotAllowed",
                    "Comment is not allowed for this question."));
            }

            if (!string.IsNullOrWhiteSpace(answer.OtherText)
                && !question.AllowsOtherOption)
            {
                errors.Add(new ApplicationError(
                    "SurveyResponse.OtherTextNotAllowed",
                    "Other text is not allowed for this question."));
            }

            ValidateAnswerForQuestion(answer, question, errors);
        }

        return errors;
    }

    private static void ValidateAnswerForQuestion(
        SubmitSurveyAnswerRequest answer,
        SurveyQuestion question,
        ICollection<ApplicationError> errors)
    {
        switch (question.Type)
        {
            case SurveyQuestionType.SingleChoice:
                ValidateChoiceAnswer(answer, question, requireExactlyOne: true, errors);
                ValidateNoTextNumericOrMatrix(answer, errors);
                break;

            case SurveyQuestionType.MultipleChoice:
                ValidateChoiceAnswer(answer, question, requireExactlyOne: false, errors);
                ValidateNoTextNumericOrMatrix(answer, errors);
                break;

            case SurveyQuestionType.ShortText:
                ValidateTextAnswer(answer, maxLength: 500, errors);
                ValidateNoOptionsNumericOrMatrix(answer, errors);
                break;

            case SurveyQuestionType.LongText:
                ValidateTextAnswer(answer, maxLength: 4000, errors);
                ValidateNoOptionsNumericOrMatrix(answer, errors);
                break;

            case SurveyQuestionType.RatingScale:
                ValidateRatingAnswer(answer, question, errors);
                ValidateNoOptionsTextOrMatrix(answer, errors);
                break;

            case SurveyQuestionType.MatrixSingleChoice:
                ValidateMatrixAnswer(answer, question, errors);
                ValidateNoOptionsTextOrNumeric(answer, errors);
                break;

            default:
                errors.Add(new ApplicationError(
                    "SurveyResponse.InvalidAnswer",
                    "Answer is invalid."));
                break;
        }
    }

    private static void ValidateChoiceAnswer(
        SubmitSurveyAnswerRequest answer,
        SurveyQuestion question,
        bool requireExactlyOne,
        ICollection<ApplicationError> errors)
    {
        var optionIds = answer.OptionIds ?? Array.Empty<Guid>();
        var hasOtherText = !string.IsNullOrWhiteSpace(answer.OtherText);

        if (requireExactlyOne && optionIds.Count + (hasOtherText ? 1 : 0) != 1)
        {
            errors.Add(new ApplicationError(
                "SurveyResponse.InvalidAnswer",
                "Single choice questions require exactly one selection."));
            return;
        }

        if (!requireExactlyOne && optionIds.Count == 0 && !hasOtherText)
        {
            errors.Add(new ApplicationError(
                "SurveyResponse.InvalidAnswer",
                "Multiple choice questions require at least one selection."));
            return;
        }

        if (optionIds.Distinct().Count() != optionIds.Count)
        {
            errors.Add(new ApplicationError(
                "SurveyResponse.InvalidOption",
                "Options cannot be duplicated."));
        }

        foreach (var optionId in optionIds)
        {
            var option = question.Options.SingleOrDefault(item => item.Id == optionId);

            if (option is null || !option.IsActive)
            {
                errors.Add(new ApplicationError(
                    "SurveyResponse.InvalidOption",
                    "Option is invalid."));
            }
        }
    }

    private static void ValidateTextAnswer(
        SubmitSurveyAnswerRequest answer,
        int maxLength,
        ICollection<ApplicationError> errors)
    {
        if (string.IsNullOrWhiteSpace(answer.TextValue))
        {
            errors.Add(new ApplicationError(
                "SurveyResponse.InvalidAnswer",
                "Text value is required."));
            return;
        }

        if (answer.TextValue.Trim().Length > maxLength)
        {
            errors.Add(new ApplicationError(
                "SurveyResponse.TextTooLong",
                $"Text value must be {maxLength} characters or fewer."));
        }
    }

    private static void ValidateRatingAnswer(
        SubmitSurveyAnswerRequest answer,
        SurveyQuestion question,
        ICollection<ApplicationError> errors)
    {
        if (answer.NumericValue is null)
        {
            errors.Add(new ApplicationError(
                "SurveyResponse.InvalidAnswer",
                "Numeric value is required."));
            return;
        }

        if (question.RatingMin is null || question.RatingMax is null)
        {
            if (answer.NumericValue <= 0)
            {
                errors.Add(new ApplicationError(
                    "SurveyResponse.InvalidAnswer",
                    "Numeric value must be greater than zero."));
            }

            return;
        }

        if (answer.NumericValue < question.RatingMin.Value
            || answer.NumericValue > question.RatingMax.Value)
        {
            errors.Add(new ApplicationError(
                "SurveyResponse.InvalidRatingValue",
                "Numeric value must be within the configured rating range."));
        }
    }

    private static void ValidateMatrixAnswer(
        SubmitSurveyAnswerRequest answer,
        SurveyQuestion question,
        ICollection<ApplicationError> errors)
    {
        var matrixAnswers = answer.MatrixAnswers ?? Array.Empty<SubmitSurveyMatrixAnswerRequest>();

        if (matrixAnswers.Count == 0)
        {
            errors.Add(new ApplicationError(
                "SurveyResponse.InvalidAnswer",
                "Matrix questions require at least one row answer."));
            return;
        }

        if (matrixAnswers.Select(item => item.RowId).Distinct().Count() != matrixAnswers.Count)
        {
            errors.Add(new ApplicationError(
                "SurveyResponse.InvalidMatrixRow",
                "Matrix row cannot be duplicated."));
        }

        if (question.IsRequired)
        {
            var activeRowIds = question.MatrixRows
                .Where(row => row.IsActive)
                .Select(row => row.Id)
                .ToHashSet();

            if (!activeRowIds.SetEquals(matrixAnswers.Select(item => item.RowId)))
            {
                errors.Add(new ApplicationError(
                    "SurveyResponse.RequiredQuestionMissing",
                    "All active matrix rows must be answered."));
            }
        }

        foreach (var matrixAnswer in matrixAnswers)
        {
            var row = question.MatrixRows.SingleOrDefault(item => item.Id == matrixAnswer.RowId);

            if (row is null || !row.IsActive)
            {
                errors.Add(new ApplicationError(
                    "SurveyResponse.InvalidMatrixRow",
                    "Matrix row is invalid."));
            }

            var option = question.Options.SingleOrDefault(item => item.Id == matrixAnswer.OptionId);

            if (option is null || !option.IsActive)
            {
                errors.Add(new ApplicationError(
                    "SurveyResponse.InvalidOption",
                    "Option is invalid."));
            }
        }
    }

    private static void ValidateNoTextNumericOrMatrix(
        SubmitSurveyAnswerRequest answer,
        ICollection<ApplicationError> errors)
    {
        if (!string.IsNullOrWhiteSpace(answer.TextValue)
            || answer.NumericValue is not null
            || (answer.MatrixAnswers?.Count ?? 0) > 0)
        {
            errors.Add(new ApplicationError(
                "SurveyResponse.InvalidAnswer",
                "Answer contains values that are not allowed for this question type."));
        }
    }

    private static void ValidateNoOptionsNumericOrMatrix(
        SubmitSurveyAnswerRequest answer,
        ICollection<ApplicationError> errors)
    {
        if ((answer.OptionIds?.Count ?? 0) > 0
            || answer.NumericValue is not null
            || (answer.MatrixAnswers?.Count ?? 0) > 0
            || !string.IsNullOrWhiteSpace(answer.OtherText))
        {
            errors.Add(new ApplicationError(
                "SurveyResponse.InvalidAnswer",
                "Answer contains values that are not allowed for this question type."));
        }
    }

    private static void ValidateNoOptionsTextOrMatrix(
        SubmitSurveyAnswerRequest answer,
        ICollection<ApplicationError> errors)
    {
        if ((answer.OptionIds?.Count ?? 0) > 0
            || !string.IsNullOrWhiteSpace(answer.TextValue)
            || (answer.MatrixAnswers?.Count ?? 0) > 0
            || !string.IsNullOrWhiteSpace(answer.OtherText))
        {
            errors.Add(new ApplicationError(
                "SurveyResponse.InvalidAnswer",
                "Answer contains values that are not allowed for this question type."));
        }
    }

    private static void ValidateNoOptionsTextOrNumeric(
        SubmitSurveyAnswerRequest answer,
        ICollection<ApplicationError> errors)
    {
        if ((answer.OptionIds?.Count ?? 0) > 0
            || !string.IsNullOrWhiteSpace(answer.TextValue)
            || answer.NumericValue is not null
            || !string.IsNullOrWhiteSpace(answer.OtherText))
        {
            errors.Add(new ApplicationError(
                "SurveyResponse.InvalidAnswer",
                "Answer contains values that are not allowed for this question type."));
        }
    }
}
