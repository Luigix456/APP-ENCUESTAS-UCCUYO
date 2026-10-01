using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Surveys.Sessions;

/// <summary>
/// Data required to create a temporary survey session.
/// </summary>
public sealed record CreateSurveySessionRequest(
    Guid SurveyAssignmentId,
    string? Title,
    string? Location,
    DateTimeOffset ExpiresAtUtc)
{
    public IReadOnlyCollection<ApplicationError> Validate(DateTimeOffset? nowUtc = null)
    {
        var now = nowUtc ?? DateTimeOffset.UtcNow;
        var errors = new List<ApplicationError>();

        if (SurveyAssignmentId == Guid.Empty)
        {
            errors.Add(new ApplicationError(
                "SurveySession.SurveyAssignmentIdRequired",
                "SurveyAssignmentId is required."));
        }

        ValidateOptionalText(Title, "SurveySession.TitleTooLong", "Title must be 200 characters or fewer.", errors);
        ValidateOptionalText(Location, "SurveySession.LocationTooLong", "Location must be 200 characters or fewer.", errors);
        ValidateExpiration(ExpiresAtUtc, now, errors);

        return errors;
    }

    internal static void ValidateExpiration(
        DateTimeOffset expiresAtUtc,
        DateTimeOffset nowUtc,
        ICollection<ApplicationError> errors)
    {
        if (expiresAtUtc.Offset != TimeSpan.Zero)
        {
            errors.Add(new ApplicationError(
                "SurveySession.ExpiresAtUtcMustBeUtc",
                "ExpiresAtUtc must be UTC."));
            return;
        }

        if (expiresAtUtc <= nowUtc)
        {
            errors.Add(new ApplicationError(
                "SurveySession.ExpiresAtUtcMustBeFuture",
                "ExpiresAtUtc must be in the future."));
        }

        if (expiresAtUtc > nowUtc.AddHours(24))
        {
            errors.Add(new ApplicationError(
                "SurveySession.ExpiresAtUtcTooFar",
                "Survey sessions cannot last more than 24 hours."));
        }
    }

    internal static void ValidateOptionalText(
        string? value,
        string code,
        string message,
        ICollection<ApplicationError> errors)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > 200)
        {
            errors.Add(new ApplicationError(code, message));
        }
    }
}
