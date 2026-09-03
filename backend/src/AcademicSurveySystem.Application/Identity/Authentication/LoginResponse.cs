using AcademicSurveySystem.Application.Common.Authentication;

namespace AcademicSurveySystem.Application.Identity.Authentication;

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAtUtc,
    AuthenticatedUser User);
