namespace AcademicSurveySystem.Application.Identity.Authentication;

public interface IAuthenticationUserStore
{
    Task<AuthenticationUserSnapshot?> FindByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken);

    Task UpdatePasswordHashAsync(
        Guid userId,
        string passwordHash,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken);
}
