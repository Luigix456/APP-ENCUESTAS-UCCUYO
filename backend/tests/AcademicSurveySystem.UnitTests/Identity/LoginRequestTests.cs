using AcademicSurveySystem.Application.Identity.Authentication;

namespace AcademicSurveySystem.UnitTests.Identity;

public sealed class LoginRequestTests
{
    [Fact]
    public void Validate_RejectsEmptyEmail()
    {
        var errors = new LoginRequest(string.Empty, "ValidPassword1!").Validate();

        Assert.Contains(errors, error => error.Contains("Email"));
    }

    [Fact]
    public void Validate_RejectsInvalidEmail()
    {
        var errors = new LoginRequest("invalid-email", "ValidPassword1!").Validate();

        Assert.Contains(errors, error => error.Contains("format"));
    }

    [Fact]
    public void Validate_RejectsEmptyPassword()
    {
        var errors = new LoginRequest("admin@institucion.edu.ar", string.Empty).Validate();

        Assert.Contains(errors, error => error.Contains("Password"));
    }

    [Fact]
    public void Validate_RejectsWhitespacePassword()
    {
        var errors = new LoginRequest("admin@institucion.edu.ar", "   ").Validate();

        Assert.Contains(errors, error => error.Contains("Password"));
    }
}
