using AcademicSurveySystem.Application.Surveys.Responses;

namespace AcademicSurveySystem.Infrastructure.Surveys;

internal static class SurveyResponseProgressCalculator
{
    public static SurveyResponseProgressDto Build(
        Guid surveyAssignmentId,
        int? expectedRespondentCount,
        int responseCount)
    {
        if (expectedRespondentCount is null)
        {
            return new SurveyResponseProgressDto(
                surveyAssignmentId,
                null,
                responseCount,
                null,
                null);
        }

        var remainingCount = Math.Max(0, expectedRespondentCount.Value - responseCount);
        var percentage = expectedRespondentCount.Value > 0
            ? Math.Min(
                100m,
                decimal.Round(responseCount * 100m / expectedRespondentCount.Value, 2))
            : 0m;

        return new SurveyResponseProgressDto(
            surveyAssignmentId,
            expectedRespondentCount,
            responseCount,
            remainingCount,
            percentage);
    }
}
