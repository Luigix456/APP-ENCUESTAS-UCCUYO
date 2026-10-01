using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Domain.Surveys.Enums;

namespace AcademicSurveySystem.Application.Surveys.Requests;

internal static class SurveyRequestValidation
{
    public static void ValidateSurveyText(
        string? title,
        string? description,
        string? target,
        ICollection<ApplicationError> errors)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            errors.Add(new ApplicationError("Survey.TitleRequired", "Title is required."));
        }

        if (description is not null && description.Trim().Length > 1000)
        {
            errors.Add(new ApplicationError(
                "Survey.DescriptionTooLong",
                "Description must be 1000 characters or fewer."));
        }

        if (string.IsNullOrWhiteSpace(target))
        {
            errors.Add(new ApplicationError("Survey.TargetRequired", "Target is required."));
        }
        else if (!Enum.TryParse<SurveyTarget>(target.Trim(), ignoreCase: true, out _))
        {
            errors.Add(new ApplicationError("Survey.TargetInvalid", "Target is invalid."));
        }
    }

    public static void ValidateSection(
        string? title,
        string? description,
        int? order,
        ICollection<ApplicationError> errors)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            errors.Add(new ApplicationError("SurveySection.TitleRequired", "Title is required."));
        }

        if (description is not null && description.Trim().Length > 1000)
        {
            errors.Add(new ApplicationError(
                "SurveySection.DescriptionTooLong",
                "Description must be 1000 characters or fewer."));
        }

        ValidateOrder(order, "SurveySection.OrderRequired", errors);
    }

    public static void ValidateQuestion(
        string? text,
        string? type,
        bool allowsOtherOption,
        int? ratingMin,
        int? ratingMax,
        int? order,
        ICollection<ApplicationError> errors)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            errors.Add(new ApplicationError("SurveyQuestion.TextRequired", "Text is required."));
        }

        SurveyQuestionType? parsedType = null;

        if (string.IsNullOrWhiteSpace(type))
        {
            errors.Add(new ApplicationError("SurveyQuestion.TypeRequired", "Type is required."));
        }
        else if (!Enum.TryParse<SurveyQuestionType>(type.Trim(), ignoreCase: true, out var value))
        {
            errors.Add(new ApplicationError("SurveyQuestion.TypeInvalid", "Type is invalid."));
        }
        else
        {
            parsedType = value;
        }

        if (allowsOtherOption
            && parsedType is not null
            && parsedType.Value is not SurveyQuestionType.SingleChoice
                and not SurveyQuestionType.MultipleChoice)
        {
            errors.Add(new ApplicationError(
                "SurveyQuestion.AllowsOtherOptionInvalid",
                "AllowsOtherOption is only valid for single choice or multiple choice questions."));
        }

        ValidateRatingBounds(parsedType, ratingMin, ratingMax, errors);

        ValidateOrder(order, "SurveyQuestion.OrderRequired", errors);
    }

    private static void ValidateRatingBounds(
        SurveyQuestionType? type,
        int? ratingMin,
        int? ratingMax,
        ICollection<ApplicationError> errors)
    {
        if (type is null)
        {
            return;
        }

        if (type.Value != SurveyQuestionType.RatingScale)
        {
            if (ratingMin is not null || ratingMax is not null)
            {
                errors.Add(new ApplicationError(
                    "SurveyQuestion.RatingBoundsInvalid",
                    "Rating bounds are only valid for rating scale questions."));
            }

            return;
        }

        if (ratingMin is null)
        {
            errors.Add(new ApplicationError(
                "SurveyQuestion.RatingMinRequired",
                "RatingMin is required for rating scale questions."));
        }

        if (ratingMax is null)
        {
            errors.Add(new ApplicationError(
                "SurveyQuestion.RatingMaxRequired",
                "RatingMax is required for rating scale questions."));
        }

        if (ratingMin is not null && ratingMax is not null && ratingMin.Value >= ratingMax.Value)
        {
            errors.Add(new ApplicationError(
                "SurveyQuestion.RatingRangeInvalid",
                "RatingMin must be less than RatingMax."));
        }
    }

    public static void ValidateOrder(
        int? order,
        string requiredCode,
        ICollection<ApplicationError> errors)
    {
        if (order is null)
        {
            errors.Add(new ApplicationError(requiredCode, "Order is required."));
        }
        else if (order <= 0)
        {
            errors.Add(new ApplicationError("Order.Invalid", "Order must be greater than zero."));
        }
    }
}
