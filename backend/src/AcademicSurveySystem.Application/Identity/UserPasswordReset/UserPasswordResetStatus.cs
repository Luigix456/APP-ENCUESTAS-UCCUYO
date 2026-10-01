namespace AcademicSurveySystem.Application.Identity.UserPasswordReset;

public enum UserPasswordResetStatus
{
    Updated = 1,
    InvalidRequest = 2,
    UserNotFound = 3,
    InvalidPassword = 4,
    Failed = 5
}
