namespace AcademicSurveySystem.Application.Identity.UserPasswordReset;

public interface IUserPasswordResetService
{
    Task<UserPasswordResetResult> ResetAsync(
        string? email,
        string? newPassword,
        CancellationToken cancellationToken = default);

    Task<UserPasswordResetResult> ResetByUserIdAsync(
        Guid userId,
        string? newPassword,
        CancellationToken cancellationToken = default);
}
