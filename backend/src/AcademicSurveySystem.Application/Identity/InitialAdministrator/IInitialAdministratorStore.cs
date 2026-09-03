using AcademicSurveySystem.Domain.Identity.Entities;

namespace AcademicSurveySystem.Application.Identity.InitialAdministrator;

public interface IInitialAdministratorStore
{
    Task<bool> AdministratorRoleExistsAsync(CancellationToken cancellationToken);

    Task<bool> AnyAdministratorExistsAsync(CancellationToken cancellationToken);

    Task<bool> UserExistsWithNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken);

    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken);

    Task AddUserAsync(User user, CancellationToken cancellationToken);
}
