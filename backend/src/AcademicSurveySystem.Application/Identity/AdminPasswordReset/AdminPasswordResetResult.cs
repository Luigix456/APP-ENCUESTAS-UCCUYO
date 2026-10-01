namespace AcademicSurveySystem.Application.Identity.AdminPasswordReset;

public sealed record AdminPasswordResetResult(
    AdminPasswordResetStatus Status,
    string Message)
{
    public bool Succeeded => Status == AdminPasswordResetStatus.Updated;

    public static AdminPasswordResetResult Updated() =>
        new(AdminPasswordResetStatus.Updated, "Administrator password reset completed.");

    public static AdminPasswordResetResult Failed(string message) =>
        new(AdminPasswordResetStatus.Failed, message);
}
