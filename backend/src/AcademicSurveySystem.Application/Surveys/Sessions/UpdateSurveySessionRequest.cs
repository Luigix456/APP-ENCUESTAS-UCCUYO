using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Surveys.Sessions;

/// <summary>
/// Data required to update a temporary survey session.
/// </summary>
public sealed record UpdateSurveySessionRequest(
    string? Title,
    string? Location,
    DateTimeOffset ExpiresAtUtc)
{
    public IReadOnlyCollection<ApplicationError> Validate(DateTimeOffset? nowUtc = null)
    {
        var now = nowUtc ?? DateTimeOffset.UtcNow;
        var errors = new List<ApplicationError>();

        CreateSurveySessionRequest.ValidateOptionalText(
            Title,
            "SurveySession.TitleTooLong",
            "Title must be 200 characters or fewer.",
            errors);
        CreateSurveySessionRequest.ValidateOptionalText(
            Location,
            "SurveySession.LocationTooLong",
            "Location must be 200 characters or fewer.",
            errors);
        CreateSurveySessionRequest.ValidateExpiration(ExpiresAtUtc, now, errors);

        return errors;
    }
}
