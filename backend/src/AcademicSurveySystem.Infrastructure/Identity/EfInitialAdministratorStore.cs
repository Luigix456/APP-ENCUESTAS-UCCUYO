using AcademicSurveySystem.Application.Identity.InitialAdministrator;
using AcademicSurveySystem.Domain.Identity;
using AcademicSurveySystem.Domain.Identity.Entities;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AcademicSurveySystem.Infrastructure.Identity;

public sealed class EfInitialAdministratorStore : IInitialAdministratorStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfInitialAdministratorStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> AdministratorRoleExistsAsync(CancellationToken cancellationToken)
    {
        return _dbContext.Roles
            .AnyAsync(role => role.Id == IdentityCatalog.RoleIds.AdministratorId, cancellationToken);
    }

    public Task<bool> AnyAdministratorExistsAsync(CancellationToken cancellationToken)
    {
        return _dbContext.UserRoles
            .AnyAsync(userRole => userRole.RoleId == IdentityCatalog.RoleIds.AdministratorId, cancellationToken);
    }

    public Task<bool> UserExistsWithNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await operation(cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public Task AddUserAsync(User user, CancellationToken cancellationToken)
    {
        _dbContext.Users.Add(user);
        return Task.CompletedTask;
    }
}
