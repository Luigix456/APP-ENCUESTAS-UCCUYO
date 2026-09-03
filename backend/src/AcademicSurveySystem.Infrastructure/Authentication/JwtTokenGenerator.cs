using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AcademicSurveySystem.Application.Common.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AcademicSurveySystem.Infrastructure.Authentication;

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    public const string NameIdentifierClaimType = "nameidentifier";
    public const string RoleClaimType = "role";
    public const string PermissionClaimType = "permission";

    private readonly JwtOptions _options;

    public JwtTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        _options.Validate();
    }

    public JwtTokenResult GenerateToken(AuthenticatedUser user)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAtUtc = now.AddMinutes(_options.AccessTokenExpirationMinutes);
        var claims = BuildClaims(user);
        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: signingCredentials);

        return new JwtTokenResult(
            new JwtSecurityTokenHandler().WriteToken(token),
            "Bearer",
            expiresAtUtc);
    }

    private static IReadOnlyCollection<Claim> BuildClaims(AuthenticatedUser user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(NameIdentifierClaimType, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName)
        };

        claims.AddRange(user.Roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(role => new Claim(RoleClaimType, role)));

        claims.AddRange(user.Permissions
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(permission => new Claim(PermissionClaimType, permission)));

        return claims;
    }
}
