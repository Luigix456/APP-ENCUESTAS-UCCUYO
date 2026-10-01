using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Identity.UserManagement;

public sealed record CreateUserRequest(
    string? FirstName,
    string? LastName,
    string? Email,
    string? Password,
    IReadOnlyCollection<Guid>? RoleIds)
{
    public IReadOnlyCollection<ApplicationError> Validate()
    {
        var errors = new List<ApplicationError>();

        ValidateRequiredText(FirstName, nameof(FirstName), 100, errors);
        ValidateRequiredText(LastName, nameof(LastName), 100, errors);
        ValidateRequiredText(Email, nameof(Email), 320, errors);

        if (RoleIds is not null && RoleIds.Any(roleId => roleId == Guid.Empty))
        {
            errors.Add(new ApplicationError("Identity.RoleIdRequired", "Role ids must not be empty."));
        }

        if (RoleIds is not null && RoleIds.Count != RoleIds.Distinct().Count())
        {
            errors.Add(new ApplicationError("Identity.DuplicateRoleId", "Role ids must not contain duplicates."));
        }

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
