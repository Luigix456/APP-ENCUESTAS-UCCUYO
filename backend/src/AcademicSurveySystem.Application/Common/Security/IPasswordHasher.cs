namespace AcademicSurveySystem.Application.Common.Security;

public interface IPasswordHasher
{
    string Hash(string password);

    PasswordVerificationResult Verify(string password, string passwordHash);
}
