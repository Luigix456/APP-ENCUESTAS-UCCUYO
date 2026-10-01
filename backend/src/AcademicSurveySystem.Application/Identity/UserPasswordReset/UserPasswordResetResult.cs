namespace AcademicSurveySystem.Application.Identity.UserPasswordReset;

public sealed record UserPasswordResetResult(
    UserPasswordResetStatus Status,
    string Message)
{
    public bool Succeeded => Status == UserPasswordResetStatus.Updated;

    public static UserPasswordResetResult Updated() =>
        new(UserPasswordResetStatus.Updated, "Password reset completed successfully.");

    public static UserPasswordResetResult Failed(UserPasswordResetStatus status, string message) =>
        new(status, message);
}
