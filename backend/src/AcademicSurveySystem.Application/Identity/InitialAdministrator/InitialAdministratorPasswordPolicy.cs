using System.Text;

namespace AcademicSurveySystem.Application.Identity.InitialAdministrator;

public static class InitialAdministratorPasswordPolicy
{
    public const int MinimumLength = 12;
    public const int MaximumLength = 64;
    public const int MaximumUtf8Bytes = 72;

    public static InitialAdministratorPasswordValidationResult Validate(string? password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Password is required.");
            return InitialAdministratorPasswordValidationResult.Invalid(errors);
        }

        if (password.Length < MinimumLength)
        {
            errors.Add($"Password must be at least {MinimumLength} characters.");
        }

        if (password.Length > MaximumLength)
        {
            errors.Add($"Password must be {MaximumLength} characters or fewer.");
        }

        if (Encoding.UTF8.GetByteCount(password) > MaximumUtf8Bytes)
        {
            errors.Add($"Password must be {MaximumUtf8Bytes} UTF-8 bytes or fewer.");
        }

        if (!password.Any(char.IsUpper))
        {
            errors.Add("Password must contain at least one uppercase letter.");
        }

        if (!password.Any(char.IsLower))
        {
            errors.Add("Password must contain at least one lowercase letter.");
        }

        if (!password.Any(char.IsNumber))
        {
            errors.Add("Password must contain at least one number.");
        }

        if (!password.Any(character => !char.IsLetterOrDigit(character)))
        {
            errors.Add("Password must contain at least one non-alphanumeric character.");
        }

        return errors.Count == 0
            ? InitialAdministratorPasswordValidationResult.Valid()
            : InitialAdministratorPasswordValidationResult.Invalid(errors);
    }
}
