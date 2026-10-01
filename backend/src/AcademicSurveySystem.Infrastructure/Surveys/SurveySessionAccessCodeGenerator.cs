using System.Security.Cryptography;
using AcademicSurveySystem.Application.Surveys.Sessions;

namespace AcademicSurveySystem.Infrastructure.Surveys;

public sealed class SurveySessionAccessCodeGenerator : ISurveySessionAccessCodeGenerator
{
    public string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);

        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
