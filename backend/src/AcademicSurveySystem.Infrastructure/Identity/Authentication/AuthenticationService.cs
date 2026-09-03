using AcademicSurveySystem.Application.Common.Authentication;
using AcademicSurveySystem.Application.Common.Security;
using AcademicSurveySystem.Application.Identity.Authentication;
using AcademicSurveySystem.Domain.Identity.Enums;

namespace AcademicSurveySystem.Infrastructure.Identity.Authentication;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IAuthenticationUserStore _store;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthenticationService(
        IAuthenticationUserStore store,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _store = store;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthenticationResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate();

        if (validationErrors.Count > 0)
        {
            return AuthenticationResult.InvalidRequest(validationErrors);
        }

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await _store.FindByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        if (user is null)
        {
            return AuthenticationResult.InvalidCredentials();
        }

        var verificationResult = VerifyPassword(request.Password, user.PasswordHash);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return AuthenticationResult.InvalidCredentials();
        }

        if (user.Status == UserStatus.Inactive)
        {
            return AuthenticationResult.UserInactive();
        }

        if (user.Status == UserStatus.Blocked)
        {
            return AuthenticationResult.UserBlocked();
        }

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            var updatedPasswordHash = _passwordHasher.Hash(request.Password);
            await _store.UpdatePasswordHashAsync(
                user.Id,
                updatedPasswordHash,
                DateTimeOffset.UtcNow,
                cancellationToken);
        }

        var authenticatedUser = new AuthenticatedUser(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Roles.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray(),
            user.Permissions.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray());

        var token = _jwtTokenGenerator.GenerateToken(authenticatedUser);

        return AuthenticationResult.Success(new LoginResponse(
            token.AccessToken,
            token.TokenType,
            token.ExpiresAtUtc,
            authenticatedUser));
    }

    private PasswordVerificationResult VerifyPassword(string password, string passwordHash)
    {
        try
        {
            return _passwordHasher.Verify(password, passwordHash);
        }
        catch (ArgumentException)
        {
            return PasswordVerificationResult.Failed;
        }
    }
}
