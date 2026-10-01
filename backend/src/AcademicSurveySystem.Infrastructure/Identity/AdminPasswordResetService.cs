using AcademicSurveySystem.Application.Common.Security;
using AcademicSurveySystem.Application.Identity.AdminPasswordReset;
using AcademicSurveySystem.Application.Identity.InitialAdministrator;
using AcademicSurveySystem.Domain.Common;
using AcademicSurveySystem.Domain.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AcademicSurveySystem.Infrastructure.Identity;

public sealed class AdminPasswordResetService : IAdminPasswordResetService
{
    private readonly IAdminPasswordResetStore _store;
    private readonly IPasswordHasher _passwordHasher;
    private readonly AdminPasswordResetOptions _options;
    private readonly ILogger<AdminPasswordResetService> _logger;

    public AdminPasswordResetService(
        IAdminPasswordResetStore store,
        IPasswordHasher passwordHasher,
        IOptions<AdminPasswordResetOptions> options,
        ILogger<AdminPasswordResetService> logger)
    {
        _store = store;
        _passwordHasher = passwordHasher;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AdminPasswordResetResult> ResetAsync(
        CancellationToken cancellationToken = default)
    {
        var configuration = ValidateConfiguration();

        if (!configuration.IsValid)
        {
            return AdminPasswordResetResult.Failed(configuration.Message);
        }

        var passwordValidation = InitialAdministratorPasswordPolicy.Validate(configuration.NewPassword);

        if (!passwordValidation.IsValid)
        {
            return AdminPasswordResetResult.Failed(
                "Administrator password reset password is invalid. "
                + string.Join(' ', passwordValidation.Errors));
        }

        var normalizedEmail = configuration.Email.Trim().ToUpperInvariant();
        var user = await _store.FindUserByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        if (user is null)
        {
            return AdminPasswordResetResult.Failed(
                "Administrator password reset failed. User was not found.");
        }

        if (!user.UserRoles.Any(userRole =>
            userRole.RoleId == IdentityCatalog.RoleIds.AdministratorId))
        {
            return AdminPasswordResetResult.Failed(
                "Administrator password reset failed. User is not an administrator.");
        }

        try
        {
            var passwordHash = _passwordHasher.Hash(configuration.NewPassword);
            user.ChangePasswordHash(passwordHash, DateTimeOffset.UtcNow);

            await _store.ExecuteInTransactionAsync(
                token => _store.SaveChangesAsync(token),
                cancellationToken);

            return AdminPasswordResetResult.Updated();
        }
        catch (DomainException exception)
        {
            _logger.LogWarning(
                "Administrator password reset validation failed with error type {ErrorType}.",
                exception.GetType().Name);

            return AdminPasswordResetResult.Failed(
                "Administrator password reset failed. User data is invalid.");
        }
        catch (Exception exception)
        {
            _logger.LogError(
                "Administrator password reset failed with error type {ErrorType}.",
                exception.GetType().Name);

            return AdminPasswordResetResult.Failed(
                "Administrator password reset failed. No credentials were written to output.");
        }
    }

    private ValidatedAdminPasswordResetConfiguration ValidateConfiguration()
    {
        var missingFields = new List<string>();
        var email = GetRequiredConfigurationValue(_options.Email, nameof(_options.Email), missingFields);
        var newPassword = GetRequiredConfigurationValue(
            _options.NewPassword,
            nameof(_options.NewPassword),
            missingFields);

        return missingFields.Count == 0
            ? ValidatedAdminPasswordResetConfiguration.Valid(email!, newPassword!)
            : ValidatedAdminPasswordResetConfiguration.Invalid(
                "AdminPasswordReset configuration is incomplete. Missing: "
                + string.Join(", ", missingFields)
                + ".");
    }

    private static string? GetRequiredConfigurationValue(
        string? value,
        string fieldName,
        ICollection<string> missingFields)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            missingFields.Add(fieldName);
            return null;
        }

        return value.Trim();
    }

    private sealed record ValidatedAdminPasswordResetConfiguration(
        bool IsValid,
        string Message,
        string Email,
        string NewPassword)
    {
        public static ValidatedAdminPasswordResetConfiguration Valid(
            string email,
            string newPassword) =>
            new(true, string.Empty, email, newPassword);

        public static ValidatedAdminPasswordResetConfiguration Invalid(string message) =>
            new(false, message, string.Empty, string.Empty);
    }
}
