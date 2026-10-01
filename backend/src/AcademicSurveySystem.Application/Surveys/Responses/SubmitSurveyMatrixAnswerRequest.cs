using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Surveys.Responses;

public sealed record SubmitSurveyMatrixAnswerRequest(
    Guid RowId,
    Guid OptionId)
{
    public IReadOnlyCollection<ApplicationError> Validate()
    {
        var errors = new List<ApplicationError>();

        if (RowId == Guid.Empty)
        {
            errors.Add(new ApplicationError(
                "SurveyResponse.MatrixRowRequired",
                "Matrix row id is required."));
        }

        if (OptionId == Guid.Empty)
        {
            errors.Add(new ApplicationError(
                "SurveyResponse.OptionRequired",
                "Option id is required."));
        }

        return errors;
    }
}
