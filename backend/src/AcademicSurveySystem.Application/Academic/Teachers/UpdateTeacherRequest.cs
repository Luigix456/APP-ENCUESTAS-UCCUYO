using System.Net.Mail;
using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Academic.Teachers;

public sealed record UpdateTeacherRequest(
    string? FirstName,
    string? LastName,
    string? Email)
{
    public IReadOnlyCollection<ApplicationError> Validate()
    {
        var errors = new List<ApplicationError>();

        if (string.IsNullOrWhiteSpace(FirstName))
        {
            errors.Add(new ApplicationError("Teacher.FirstNameRequired", "FirstName is required."));
        }

        if (string.IsNullOrWhiteSpace(LastName))
        {
            errors.Add(new ApplicationError("Teacher.LastNameRequired", "LastName is required."));
        }

        if (!string.IsNullOrWhiteSpace(Email))
        {
            var normalized = Email.Trim().ToLowerInvariant();

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

        return errors;
    }
}
