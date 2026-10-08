namespace AcademicSurveySystem.Application.Surveys.Results;

public sealed class ResultsPrivacyOptions
{
    public const int MinimumAllowedResponses = 1;

    public int MinimumResponsesForDetailedResults { get; set; } = 5;

    public bool IsValid()
    {
        return MinimumResponsesForDetailedResults >= MinimumAllowedResponses;
    }
}
