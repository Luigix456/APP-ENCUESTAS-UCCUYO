using AcademicSurveySystem.Application.Identity.Authentication;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AcademicSurveySystem.Infrastructure.Identity.Authentication;

public sealed class EfAuthenticationUserStore : IAuthenticationUserStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfAuthenticationUserStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AuthenticationUserSnapshot?> FindByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .Where(user => user.NormalizedEmail == normalizedEmail)
            .Select(user => new AuthenticationUserSnapshot(
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.PasswordHash,
                user.Status,
                user.UserRoles.Select(userRole => userRole.Role.Code).ToArray(),
                user.UserRoles
                    .SelectMany(userRole => userRole.Role.RolePermissions)
                    .Select(rolePermission => rolePermission.Permission.Code)
                    .ToArray()))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task UpdatePasswordHashAsync(
        Guid userId,
        string passwordHash,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .SingleAsync(item => item.Id == userId, cancellationToken);

        user.ChangePasswordHash(passwordHash, updatedAtUtc);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
