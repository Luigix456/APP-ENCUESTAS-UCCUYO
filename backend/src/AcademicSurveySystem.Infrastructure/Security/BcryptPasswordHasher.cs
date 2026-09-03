using System.Text;
using System.Text.RegularExpressions;
using AcademicSurveySystem.Application.Common.Security;
using Microsoft.Extensions.Options;

namespace AcademicSurveySystem.Infrastructure.Security;

public sealed partial class BcryptPasswordHasher : IPasswordHasher
{
    public const int MinimumPasswordLength = 12;
    public const int MaximumPasswordLength = 64;
    public const int MaximumPasswordBytes = 72;

    private readonly int _workFactor;

    public BcryptPasswordHasher(IOptions<PasswordHashingOptions> options)
    {
        var passwordHashingOptions = options.Value;
        passwordHashingOptions.Validate();

        _workFactor = passwordHashingOptions.WorkFactor;
    }

    public string Hash(string password)
    {
        ValidatePassword(password);

        return BCrypt.Net.BCrypt.HashPassword(password, _workFactor);
    }

    public PasswordVerificationResult Verify(string password, string passwordHash)
    {
        ValidatePassword(password);

        if (!TryGetWorkFactor(passwordHash, out var hashWorkFactor))
        {
            return PasswordVerificationResult.Failed;
        }

        try
        {
            var verified = BCrypt.Net.BCrypt.Verify(password, passwordHash);

            if (!verified)
            {
                return PasswordVerificationResult.Failed;
            }

            return hashWorkFactor < _workFactor
                ? PasswordVerificationResult.SuccessRehashNeeded
                : PasswordVerificationResult.Success;
        }
        catch
        {
            return PasswordVerificationResult.Failed;
        }
    }

    private static void ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password is required.", nameof(password));
        }

        if (password.Length < MinimumPasswordLength)
        {
            throw new ArgumentException(
                $"Password must be at least {MinimumPasswordLength} characters.",
                nameof(password));
        }

        if (password.Length > MaximumPasswordLength)
        {
            throw new ArgumentException(
                $"Password must be {MaximumPasswordLength} characters or fewer.",
                nameof(password));
        }

        if (Encoding.UTF8.GetByteCount(password) > MaximumPasswordBytes)
        {
            throw new ArgumentException(
                $"Password must be {MaximumPasswordBytes} UTF-8 bytes or fewer.",
                nameof(password));
        }
    }

    private static bool TryGetWorkFactor(string? passwordHash, out int workFactor)
    {
        workFactor = 0;

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        var match = BcryptHashRegex().Match(passwordHash);

        return match.Success
            && int.TryParse(match.Groups["workFactor"].Value, out workFactor);
    }

    [GeneratedRegex("^\\$2[aby]\\$(?<workFactor>\\d{2})\\$[./A-Za-z0-9]{53}$")]
    private static partial Regex BcryptHashRegex();
}
