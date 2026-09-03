namespace AcademicSurveySystem.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const int MinimumSigningKeyLength = 32;
    public const int MinimumAccessTokenExpirationMinutes = 5;
    public const int MaximumAccessTokenExpirationMinutes = 1440;

    public string Issuer { get; set; } = "AcademicSurveySystem";
    public string Audience { get; set; } = "AcademicSurveySystem.Web";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 60;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer))
        {
            throw new InvalidOperationException("Jwt Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("Jwt Audience is required.");
        }

        if (string.IsNullOrWhiteSpace(SigningKey))
        {
            throw new InvalidOperationException("Jwt SigningKey is required.");
        }

        if (SigningKey.Length < MinimumSigningKeyLength)
        {
            throw new InvalidOperationException(
                $"Jwt SigningKey must be at least {MinimumSigningKeyLength} characters.");
        }

        if (AccessTokenExpirationMinutes is < MinimumAccessTokenExpirationMinutes
            or > MaximumAccessTokenExpirationMinutes)
        {
            throw new InvalidOperationException(
                $"Jwt AccessTokenExpirationMinutes must be between {MinimumAccessTokenExpirationMinutes} and {MaximumAccessTokenExpirationMinutes}.");
        }
    }
}
