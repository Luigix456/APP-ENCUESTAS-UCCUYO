using AcademicSurveySystem.Application.Identity.InitialAdministrator;

namespace AcademicSurveySystem.UnitTests.Identity;

public sealed class InitialAdministratorPasswordPolicyTests
{
    [Fact]
    public void Validate_AcceptsValidPassword()
    {
        var result = InitialAdministratorPasswordPolicy.Validate("ValidPassword1!");

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_RejectsPasswordWithLessThan12Characters()
    {
        var result = InitialAdministratorPasswordPolicy.Validate("Short1!");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("at least 12"));
    }

    [Fact]
    public void Validate_RejectsPasswordLongerThan64Characters()
    {
        var result = InitialAdministratorPasswordPolicy.Validate(new string('A', 62) + "a1!");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("64 characters"));
    }

    [Fact]
    public void Validate_RejectsPasswordLongerThan72Utf8Bytes()
    {
        var result = InitialAdministratorPasswordPolicy.Validate("Aa1!" + new string('á', 35));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("72 UTF-8 bytes"));
    }

    [Fact]
    public void Validate_RejectsPasswordWithoutUppercaseLetter()
    {
        var result = InitialAdministratorPasswordPolicy.Validate("validpassword1!");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("uppercase"));
    }

    [Fact]
    public void Validate_RejectsPasswordWithoutLowercaseLetter()
    {
        var result = InitialAdministratorPasswordPolicy.Validate("VALIDPASSWORD1!");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("lowercase"));
    }

    [Fact]
    public void Validate_RejectsPasswordWithoutNumber()
    {
        var result = InitialAdministratorPasswordPolicy.Validate("ValidPassword!");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("number"));
    }

    [Fact]
    public void Validate_RejectsPasswordWithoutSpecialCharacter()
    {
        var result = InitialAdministratorPasswordPolicy.Validate("ValidPassword1");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("non-alphanumeric"));
    }

    [Fact]
    public void Validate_RejectsEmptyPassword()
    {
        var result = InitialAdministratorPasswordPolicy.Validate(string.Empty);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("required"));
    }

    [Fact]
    public void Validate_RejectsWhitespacePassword()
    {
        var result = InitialAdministratorPasswordPolicy.Validate("   ");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("required"));
    }
}
