using AcademicSurveySystem.Application.Identity.AdminPasswordReset;
using AcademicSurveySystem.Domain.Identity.Entities;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AcademicSurveySystem.Infrastructure.Identity;

public sealed class EfAdminPasswordResetStore : IAdminPasswordResetStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfAdminPasswordResetStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> FindUserByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .Include(user => user.UserRoles)
            .SingleOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
