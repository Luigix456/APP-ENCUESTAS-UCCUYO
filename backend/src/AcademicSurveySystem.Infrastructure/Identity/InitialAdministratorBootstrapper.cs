using AcademicSurveySystem.Application.Common.Security;
using AcademicSurveySystem.Application.Identity.InitialAdministrator;
using AcademicSurveySystem.Domain.Identity;
using AcademicSurveySystem.Domain.Identity.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AcademicSurveySystem.Infrastructure.Identity;

public sealed class InitialAdministratorBootstrapper : IInitialAdministratorBootstrapper
{
    private readonly IInitialAdministratorStore _store;
    private readonly IPasswordHasher _passwordHasher;
    private readonly InitialAdministratorOptions _options;
    private readonly ILogger<InitialAdministratorBootstrapper> _logger;

    public InitialAdministratorBootstrapper(
        IInitialAdministratorStore store,
        IPasswordHasher passwordHasher,
        IOptions<InitialAdministratorOptions> options,
        ILogger<InitialAdministratorBootstrapper> logger)
    {
        _store = store;
        _passwordHasher = passwordHasher;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<InitialAdministratorBootstrapResult> BootstrapAsync(
        CancellationToken cancellationToken = default)
    {
        var configuration = ValidateConfiguration();

        if (!configuration.IsValid)
        {
            return InitialAdministratorBootstrapResult.Failed(configuration.Message);
        }

        var passwordValidation = InitialAdministratorPasswordPolicy.Validate(configuration.Password);

        if (!passwordValidation.IsValid)
        {
            return InitialAdministratorBootstrapResult.Failed(
                "Initial administrator password is invalid. "
                + string.Join(' ', passwordValidation.Errors));
        }

        if (!await _store.AdministratorRoleExistsAsync(cancellationToken))
        {
            return InitialAdministratorBootstrapResult.Failed(
                "Administrator role was not found. Apply database migrations before running bootstrap.");
        }

        if (await _store.AnyAdministratorExistsAsync(cancellationToken))
        {
            return InitialAdministratorBootstrapResult.AlreadyExists();
        }

        var normalizedEmail = configuration.Email.Trim().ToUpperInvariant();

        if (await _store.UserExistsWithNormalizedEmailAsync(normalizedEmail, cancellationToken))
        {
            return InitialAdministratorBootstrapResult.Failed(
                "A user with the configured email already exists and will not be elevated to administrator.");
        }

        try
        {
            var now = DateTimeOffset.UtcNow;
            var passwordHash = _passwordHasher.Hash(configuration.Password);
            var user = new User(
                Guid.NewGuid(),
                configuration.FirstName,
                configuration.LastName,
                configuration.Email,
                passwordHash,
                now);

            user.AssignRole(IdentityCatalog.RoleIds.AdministratorId, now);

            await _store.ExecuteInTransactionAsync(
                token => _store.AddUserAsync(user, token),
                cancellationToken);

            return InitialAdministratorBootstrapResult.Created();
        }
        catch (Exception exception)
        {
            _logger.LogError(
                "Initial administrator bootstrap failed with error type {ErrorType}.",
                exception.GetType().Name);

            return InitialAdministratorBootstrapResult.Failed(
                "Initial administrator bootstrap failed. No credentials were written to output.");
        }
    }

    private ValidatedInitialAdministratorConfiguration ValidateConfiguration()
    {
        var missingFields = new List<string>();

        var firstName = GetRequiredConfigurationValue(_options.FirstName, nameof(_options.FirstName), missingFields);
        var lastName = GetRequiredConfigurationValue(_options.LastName, nameof(_options.LastName), missingFields);
        var email = GetRequiredConfigurationValue(_options.Email, nameof(_options.Email), missingFields);
        var password = GetRequiredConfigurationValue(_options.Password, nameof(_options.Password), missingFields);

        return missingFields.Count == 0
            ? ValidatedInitialAdministratorConfiguration.Valid(firstName!, lastName!, email!, password!)
            : ValidatedInitialAdministratorConfiguration.Invalid(
                "InitialAdmin configuration is incomplete. Missing: "
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

    private sealed record ValidatedInitialAdministratorConfiguration(
        bool IsValid,
        string Message,
        string FirstName,
        string LastName,
        string Email,
        string Password)
    {
        public static ValidatedInitialAdministratorConfiguration Valid(
            string firstName,
            string lastName,
            string email,
            string password) =>
            new(true, string.Empty, firstName, lastName, email, password);

        public static ValidatedInitialAdministratorConfiguration Invalid(string message) =>
            new(false, message, string.Empty, string.Empty, string.Empty, string.Empty);
    }
}
