namespace AcademicSurveySystem.Application.Common.Authentication;

public sealed record JwtTokenResult(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAtUtc);
