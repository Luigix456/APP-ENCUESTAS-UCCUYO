namespace AcademicSurveySystem.Application.Common.Authentication;

public interface IJwtTokenGenerator
{
    JwtTokenResult GenerateToken(AuthenticatedUser user);
}
