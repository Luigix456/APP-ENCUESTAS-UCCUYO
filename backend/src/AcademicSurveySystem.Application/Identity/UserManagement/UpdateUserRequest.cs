using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Identity.UserManagement;

public sealed record UpdateUserRequest(
    string? FirstName,
    string? LastName,
    string? Email)
{
    public IReadOnlyCollection<ApplicationError> Validate()
    {
        var errors = new List<ApplicationError>();

        ValidateRequiredText(FirstName, nameof(FirstName), 100, errors);
        ValidateRequiredText(LastName, nameof(LastName), 100, errors);
        ValidateRequiredText(Email, nameof(Email), 320, errors);

        return errors;
    }

    private static void ValidateRequiredText(
        string? value,
        string fieldName,
        int maxLength,
        ICollection<ApplicationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ApplicationError($"Identity.{fieldName}Required", $"{fieldName} is required."));
            return;
        }

        if (value.Trim().Length > maxLength)
        {
            errors.Add(new ApplicationError(
                $"Identity.{fieldName}TooLong",
                $"{fieldName} must be {maxLength} characters or fewer."));
        }
    }
}
