using System.IdentityModel.Tokens.Jwt;
using AcademicSurveySystem.Application.Common.Authentication;
using AcademicSurveySystem.Infrastructure.Authentication;
using Microsoft.Extensions.Options;

namespace AcademicSurveySystem.UnitTests.Security;

public sealed class JwtTokenGeneratorTests
{
    [Fact]
    public void GenerateToken_ReturnsNonEmptyBearerToken()
    {
        var result = CreateGenerator().GenerateToken(CreateUser());

        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.Equal("Bearer", result.TokenType);
    }

    [Fact]
    public void GenerateToken_ContainsIssuerAndAudience()
    {
        var result = CreateGenerator().GenerateToken(CreateUser());
        var token = ReadToken(result.AccessToken);

        Assert.Equal("TestIssuer", token.Issuer);
        Assert.Contains("TestAudience", token.Audiences);
    }

    [Fact]
    public void GenerateToken_ContainsSubjectAndEmail()
    {
        var user = CreateUser();
        var result = CreateGenerator().GenerateToken(user);
        var token = ReadToken(result.AccessToken);

        Assert.Contains(token.Claims, claim =>
            claim.Type == JwtRegisteredClaimNames.Sub && claim.Value == user.Id.ToString());
        Assert.Contains(token.Claims, claim =>
            claim.Type == JwtRegisteredClaimNames.Email && claim.Value == user.Email);
    }

    [Fact]
    public void GenerateToken_ContainsRolesAndPermissions()
    {
        var result = CreateGenerator().GenerateToken(CreateUser());
        var token = ReadToken(result.AccessToken);

        Assert.Contains(token.Claims, claim =>
            claim.Type == JwtTokenGenerator.RoleClaimType && claim.Value == "administrator");
        Assert.Contains(token.Claims, claim =>
            claim.Type == JwtTokenGenerator.PermissionClaimType && claim.Value == "identity.users.read");
    }

    [Fact]
    public void GenerateToken_DoesNotContainPasswordHash()
    {
        var result = CreateGenerator().GenerateToken(CreateUser());
        var token = ReadToken(result.AccessToken);

        Assert.DoesNotContain(token.Claims, claim =>
            claim.Type.Contains("password", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.AccessToken, "password_hash", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateToken_ExpiresAccordingToConfiguration()
    {
        var before = DateTimeOffset.UtcNow.AddMinutes(29);
        var result = CreateGenerator(expirationMinutes: 30).GenerateToken(CreateUser());
        var after = DateTimeOffset.UtcNow.AddMinutes(31);

        Assert.InRange(result.ExpiresAtUtc, before, after);
    }

    private static JwtTokenGenerator CreateGenerator(int expirationMinutes = 60)
    {
        return new JwtTokenGenerator(
            Options.Create(new JwtOptions
            {
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                SigningKey = "TEST_SIGNING_KEY_WITH_AT_LEAST_32_CHARS",
                AccessTokenExpirationMinutes = expirationMinutes
            }));
    }

    private static AuthenticatedUser CreateUser()
    {
        return new AuthenticatedUser(
            Guid.NewGuid(),
            "Initial",
            "Admin",
            "admin@institucion.edu.ar",
            ["administrator"],
            ["identity.users.read"]);
    }

    private static JwtSecurityToken ReadToken(string accessToken) =>
        new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
}
