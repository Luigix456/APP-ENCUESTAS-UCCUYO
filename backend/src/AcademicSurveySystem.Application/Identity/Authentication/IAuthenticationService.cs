namespace AcademicSurveySystem.Application.Identity.Authentication;

public interface IAuthenticationService
{
    Task<AuthenticationResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken);
}
