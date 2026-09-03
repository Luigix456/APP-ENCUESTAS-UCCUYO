namespace AcademicSurveySystem.Application.Identity.Authentication;

public sealed record AuthenticationResult(
    bool Succeeded,
    LoginResponse? Response,
    AuthenticationFailureReason FailureReason,
    IReadOnlyCollection<string> Errors)
{
    public static AuthenticationResult Success(LoginResponse response) =>
        new(true, response, AuthenticationFailureReason.None, Array.Empty<string>());

    public static AuthenticationResult InvalidRequest(IReadOnlyCollection<string> errors) =>
        new(false, null, AuthenticationFailureReason.InvalidRequest, errors);

    public static AuthenticationResult InvalidCredentials() =>
        new(false, null, AuthenticationFailureReason.InvalidCredentials, Array.Empty<string>());

    public static AuthenticationResult UserInactive() =>
        new(false, null, AuthenticationFailureReason.UserInactive, Array.Empty<string>());

    public static AuthenticationResult UserBlocked() =>
        new(false, null, AuthenticationFailureReason.UserBlocked, Array.Empty<string>());

    public static AuthenticationResult Failed(string message) =>
        new(false, null, AuthenticationFailureReason.Failed, [message]);
}
