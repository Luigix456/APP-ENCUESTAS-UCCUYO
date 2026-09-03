namespace AcademicSurveySystem.Application.Identity.Authentication;

public enum AuthenticationFailureReason
{
    None = 0,
    InvalidRequest = 1,
    InvalidCredentials = 2,
    UserInactive = 3,
    UserBlocked = 4,
    Failed = 5
}
