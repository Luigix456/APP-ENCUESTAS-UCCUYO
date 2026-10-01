using AcademicSurveySystem.Domain.Identity.Entities;

namespace AcademicSurveySystem.Application.Identity.AdminPasswordReset;

public interface IAdminPasswordResetStore
{
    Task<User?> FindUserByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken);

    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
