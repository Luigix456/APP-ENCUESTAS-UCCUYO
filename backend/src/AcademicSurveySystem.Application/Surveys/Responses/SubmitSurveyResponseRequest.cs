using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Surveys.Responses;

public sealed record SubmitSurveyResponseRequest(
    IReadOnlyCollection<SubmitSurveyAnswerRequest>? Answers)
{
    public IReadOnlyCollection<ApplicationError> Validate()
    {
        var errors = new List<ApplicationError>();

        if (Answers is null)
        {
            errors.Add(new ApplicationError(
                "SurveyResponse.AnswersRequired",
                "Answers are required."));

            return errors;
        }

        foreach (var answer in Answers)
        {
            foreach (var error in answer.Validate())
            {
                errors.Add(error);
            }
        }

        return errors;
    }
}
