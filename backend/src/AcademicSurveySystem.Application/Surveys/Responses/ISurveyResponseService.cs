using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Surveys.Responses;

public interface ISurveyResponseService
{
    Task<ApplicationResult<SurveyResponseSubmissionDto>> SubmitResponseAsync(
        string accessCode,
        SubmitSurveyResponseRequest request,
        CancellationToken cancellationToken);
}
