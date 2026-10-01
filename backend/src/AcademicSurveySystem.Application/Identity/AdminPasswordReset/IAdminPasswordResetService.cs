namespace AcademicSurveySystem.Application.Identity.AdminPasswordReset;

public interface IAdminPasswordResetService
{
    Task<AdminPasswordResetResult> ResetAsync(CancellationToken cancellationToken = default);
}
