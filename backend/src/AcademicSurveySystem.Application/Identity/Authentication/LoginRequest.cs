using System.Net.Mail;

namespace AcademicSurveySystem.Application.Identity.Authentication;

/// <summary>
/// Credenciales usadas para solicitar un token JWT.
/// </summary>
/// <param name="Email">Email del usuario interno.</param>
/// <param name="Password">Contraseña del usuario interno.</param>
public sealed record LoginRequest(string Email, string Password)
{
    public IReadOnlyCollection<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Email))
        {
            errors.Add("Email is required.");
        }
        else if (!IsValidEmail(Email.Trim()))
        {
            errors.Add("Email format is invalid.");
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            errors.Add("Password is required.");
        }

        return errors;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new MailAddress(email);
            return address.Address == email;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
