using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Surveys.Responses;

public sealed record SubmitSurveyAnswerRequest(
    Guid QuestionId,
    IReadOnlyCollection<Guid>? OptionIds,
    string? TextValue,
    int? NumericValue,
    string? Comment,
    IReadOnlyCollection<SubmitSurveyMatrixAnswerRequest>? MatrixAnswers,
    string? OtherText = null)
{
    public IReadOnlyCollection<ApplicationError> Validate()
    {
        var errors = new List<ApplicationError>();

        if (QuestionId == Guid.Empty)
        {
            errors.Add(new ApplicationError(
                "SurveyResponse.QuestionRequired",
                "Question id is required."));
        }

        if (TextValue is not null && TextValue.Trim().Length > 4000)
        {
            errors.Add(new ApplicationError(
                "SurveyResponse.TextTooLong",
                "Text value must be 4000 characters or fewer."));
        }

        if (Comment is not null && Comment.Trim().Length > 1000)
        {
            errors.Add(new ApplicationError(
                "SurveyResponse.CommentTooLong",
                "Comment must be 1000 characters or fewer."));
        }

        if (OtherText is not null && OtherText.Trim().Length > 1000)
        {
            errors.Add(new ApplicationError(
                "SurveyResponse.OtherTextTooLong",
                "Other text must be 1000 characters or fewer."));
        }

        foreach (var matrixAnswer in MatrixAnswers ?? Array.Empty<SubmitSurveyMatrixAnswerRequest>())
        {
            foreach (var error in matrixAnswer.Validate())
            {
                errors.Add(error);
            }
        }

        return errors;
    }
}
