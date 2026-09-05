using System.Net.Mail;
using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Academic.Teachers;

/// <summary>
/// Datos para crear un docente.
/// </summary>
/// <param name="FirstName">Nombre del docente.</param>
/// <param name="LastName">Apellido del docente.</param>
/// <param name="Email">Email institucional opcional.</param>
public sealed record CreateTeacherRequest(
    string? FirstName,
    string? LastName,
    string? Email)
{
    public IReadOnlyCollection<ApplicationError> Validate()
    {
        var errors = new List<ApplicationError>();

        ValidateName(FirstName, "Teacher.FirstNameRequired", "FirstName is required.", errors);
        ValidateName(LastName, "Teacher.LastNameRequired", "LastName is required.", errors);
        ValidateEmail(Email, errors);

        return errors;
    }

    private static void ValidateName(
        string? value,
        string code,
        string message,
        ICollection<ApplicationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ApplicationError(code, message));
        }
    }

    private static void ValidateEmail(string? email, ICollection<ApplicationError> errors)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var normalized = email.Trim().ToLowerInvariant();

        try
        {
            var address = new MailAddress(normalized);

            if (address.Address != normalized)
            {
                errors.Add(new ApplicationError("Teacher.EmailInvalid", "Email is invalid."));
            }
        }
        catch (FormatException)
        {
            errors.Add(new ApplicationError("Teacher.EmailInvalid", "Email is invalid."));
        }
    }
}
