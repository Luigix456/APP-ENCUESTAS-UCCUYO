using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Common.Security;
using AcademicSurveySystem.Application.Audit;
using AcademicSurveySystem.Application.Identity.InitialAdministrator;
using AcademicSurveySystem.Application.Identity.UserManagement;
using AcademicSurveySystem.Domain.Common;
using AcademicSurveySystem.Domain.Identity;
using AcademicSurveySystem.Domain.Identity.Entities;
using AcademicSurveySystem.Domain.Identity.Enums;
using AcademicSurveySystem.Infrastructure.Audit;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AcademicSurveySystem.Infrastructure.Identity;

public sealed class UserManagementService : IUserManagementService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditWriter _auditWriter;

    public UserManagementService(
        ApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        IAuditWriter? auditWriter = null)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _auditWriter = auditWriter ?? NoOpAuditWriter.Instance;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<UserDto>>> GetUsersAsync(
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var users = await BuildUserQuery(includeInactive)
            .ToArrayAsync(cancellationToken);

        return ApplicationResult<IReadOnlyCollection<UserDto>>.Success(
            users.Select(MapUser).ToArray());
    }

    public async Task<ApplicationResult<UserDto>> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await BuildUserQuery(includeInactive: true)
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);

        return user is null
            ? ApplicationResult<UserDto>.NotFound("User was not found.")
            : ApplicationResult<UserDto>.Success(MapUser(user));
    }

    public async Task<ApplicationResult<UserDto>> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate().ToList();
        var passwordValidation = InitialAdministratorPasswordPolicy.Validate(request.Password);

        if (!passwordValidation.IsValid)
        {
            validationErrors.AddRange(passwordValidation.Errors.Select(error =>
                new ApplicationError("Identity.InvalidPassword", error)));
        }

        if (validationErrors.Count > 0)
        {
            return ApplicationResult<UserDto>.Validation(validationErrors);
        }

        var normalizedEmail = NormalizeEmail(request.Email!);

        if (await EmailExistsAsync(normalizedEmail, excludedUserId: null, cancellationToken))
        {
            return EmailConflict<UserDto>();
        }

        var requestedRoleIds = NormalizeRoleIds(request.RoleIds);
        var rolesResult = await GetExistingRolesAsync(requestedRoleIds, cancellationToken);

        if (rolesResult.Status != ApplicationResultStatus.Success)
        {
            return ApplicationResult<UserDto>.NotFound("Role was not found.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var now = DateTimeOffset.UtcNow;
            var user = new User(
                Guid.NewGuid(),
                request.FirstName!,
                request.LastName!,
                request.Email!,
                _passwordHasher.Hash(request.Password!),
                now);

            foreach (var roleId in requestedRoleIds)
            {
                user.AssignRole(roleId, now);
            }

            _dbContext.Users.Add(user);
            var roleCodes = await LoadRoleCodesAsync(requestedRoleIds, cancellationToken);
            await _auditWriter.WriteAsync(
                "identity.user.created",
                "identity",
                "User",
                user.Id,
                $"Creó el usuario {BuildDisplayName(user)}.",
                roleCodes.Length == 0 ? null : new { roleCodes },
                cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var createdUser = await LoadUserDtoAsync(user.Id, cancellationToken);

            return ApplicationResult<UserDto>.Success(createdUser!);
        }
        catch (DomainException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return ApplicationResult<UserDto>.Validation([
                new ApplicationError("Identity.InvalidUser", exception.Message)
            ]);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return EmailConflict<UserDto>();
        }
    }

    public async Task<ApplicationResult> UpdateUserAsync(
        Guid userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate();

        if (validationErrors.Count > 0)
        {
            return ApplicationResult.Validation(validationErrors);
        }

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);

        if (user is null)
        {
            return ApplicationResult.NotFound("User was not found.");
        }

        var normalizedEmail = NormalizeEmail(request.Email!);

        if (await EmailExistsAsync(normalizedEmail, userId, cancellationToken))
        {
            return EmailConflict();
        }

        try
        {
            var changedFields = new List<string>();

            if (!string.Equals(user.FirstName, request.FirstName, StringComparison.Ordinal))
            {
                changedFields.Add("firstName");
            }

            if (!string.Equals(user.LastName, request.LastName, StringComparison.Ordinal))
            {
                changedFields.Add("lastName");
            }

            if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
            {
                changedFields.Add("email");
            }

            var now = DateTimeOffset.UtcNow;
            user.UpdateName(request.FirstName!, request.LastName!, now);
            user.ChangeEmail(request.Email!, now);
            await _auditWriter.WriteAsync(
                "identity.user.updated",
                "identity",
                "User",
                user.Id,
                $"Actualizó el usuario {BuildDisplayName(user)}.",
                new { changedFields },
                cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApplicationResult.Success();
        }
        catch (DomainException exception)
        {
            return ApplicationResult.Validation([
                new ApplicationError("Identity.InvalidUser", exception.Message)
            ]);
        }
        catch (DbUpdateException)
        {
            return EmailConflict();
        }
    }

    public Task<ApplicationResult> ActivateUserAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        ChangeUserStatusAsync(userId, activate: true, currentUserId: null, cancellationToken);

    public Task<ApplicationResult> DeactivateUserAsync(
        Guid userId,
        Guid currentUserId,
        CancellationToken cancellationToken) =>
        ChangeUserStatusAsync(userId, activate: false, currentUserId, cancellationToken);

    public async Task<ApplicationResult<IReadOnlyCollection<RoleDto>>> GetRolesAsync(
        CancellationToken cancellationToken)
    {
        var roles = await _dbContext.Roles
            .AsNoTracking()
            .Where(role => role.IsActive)
            .OrderBy(role => role.Name)
            .Select(role => new RoleDto(
                role.Id,
                role.Code,
                role.Name,
                role.RolePermissions
                    .Select(rolePermission => rolePermission.Permission.Code)
                    .OrderBy(permission => permission)
                    .ToArray()))
            .ToArrayAsync(cancellationToken);

        return ApplicationResult<IReadOnlyCollection<RoleDto>>.Success(roles);
    }

    public async Task<ApplicationResult<UserDto>> ReplaceUserRolesAsync(
        Guid userId,
        UpdateUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = request.Validate();

        if (validationErrors.Count > 0)
        {
            return ApplicationResult<UserDto>.Validation(validationErrors);
        }

        var user = await _dbContext.Users
            .Include(item => item.UserRoles)
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);

        if (user is null)
        {
            return ApplicationResult<UserDto>.NotFound("User was not found.");
        }

        var requestedRoleIds = NormalizeRoleIds(request.RoleIds);
        var rolesResult = await GetExistingRolesAsync(requestedRoleIds, cancellationToken);

        if (rolesResult.Status != ApplicationResultStatus.Success)
        {
            return ApplicationResult<UserDto>.NotFound("Role was not found.");
        }

        if (!requestedRoleIds.Contains(IdentityCatalog.RoleIds.AdministratorId)
            && await IsLastActiveAdministratorAsync(userId, cancellationToken))
        {
            return ApplicationResult<UserDto>.Conflict("At least one active administrator is required.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var currentRoleIds = user.UserRoles
                .Select(userRole => userRole.RoleId)
                .ToHashSet();
            var requestedRoleIdSet = requestedRoleIds.ToHashSet();
            var oldRoleCodes = await LoadRoleCodesAsync(currentRoleIds, cancellationToken);
            var newRoleCodes = await LoadRoleCodesAsync(requestedRoleIds, cancellationToken);

            foreach (var roleId in currentRoleIds.Except(requestedRoleIdSet).ToArray())
            {
                user.RemoveRole(roleId);
            }

            var now = DateTimeOffset.UtcNow;

            foreach (var roleId in requestedRoleIds.Except(currentRoleIds))
            {
                user.AssignRole(roleId, now);
            }

            await _auditWriter.WriteAsync(
                "identity.user.roles_updated",
                "identity",
                "User",
                user.Id,
                $"Actualizó los roles del usuario {BuildDisplayName(user)}.",
                new { oldRoleCodes, newRoleCodes },
                cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var updatedUser = await LoadUserDtoAsync(userId, cancellationToken);

            return ApplicationResult<UserDto>.Success(updatedUser!);
        }
        catch (DomainException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return ApplicationResult<UserDto>.Validation([
                new ApplicationError("Identity.InvalidRoleAssignment", exception.Message)
            ]);
        }
    }

    private async Task<ApplicationResult> ChangeUserStatusAsync(
        Guid userId,
        bool activate,
        Guid? currentUserId,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);

        if (user is null)
        {
            return ApplicationResult.NotFound("User was not found.");
        }

        var now = DateTimeOffset.UtcNow;

        if (activate)
        {
            user.Activate(now);
            await _auditWriter.WriteAsync(
                "identity.user.activated",
                "identity",
                "User",
                user.Id,
                $"Activó el usuario {BuildDisplayName(user)}.",
                null,
                cancellationToken);
        }
        else
        {
            if (currentUserId == userId)
            {
                return ApplicationResult.Conflict("The current user cannot deactivate itself.");
            }

            if (await IsLastActiveAdministratorAsync(userId, cancellationToken))
            {
                return ApplicationResult.Conflict("At least one active administrator is required.");
            }

            user.Deactivate(now);
            await _auditWriter.WriteAsync(
                "identity.user.deactivated",
                "identity",
                "User",
                user.Id,
                $"Desactivó el usuario {BuildDisplayName(user)}.",
                null,
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApplicationResult.Success();
    }

    private IQueryable<User> BuildUserQuery(bool includeInactive)
    {
        var query = _dbContext.Users
            .AsNoTracking()
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(user => user.Status == UserStatus.Active);
        }

        return query
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .ThenBy(user => user.Email);
    }

    private async Task<UserDto?> LoadUserDtoAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await BuildUserQuery(includeInactive: true)
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);

        return user is null ? null : MapUser(user);
    }

    private static UserDto MapUser(User user)
    {
        return new UserDto(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Status.ToString(),
            user.UserRoles
                .OrderBy(userRole => userRole.Role.Name)
                .Select(userRole => new UserRoleDto(
                    userRole.RoleId,
                    userRole.Role.Code,
                    userRole.Role.Name))
                .ToArray(),
            user.CreatedAtUtc,
            user.UpdatedAtUtc);
    }

    private async Task<bool> EmailExistsAsync(
        string normalizedEmail,
        Guid? excludedUserId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Users.AnyAsync(
            user => user.NormalizedEmail == normalizedEmail
                && (!excludedUserId.HasValue || user.Id != excludedUserId.Value),
            cancellationToken);
    }

    private async Task<ApplicationResult> GetExistingRolesAsync(
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken)
    {
        if (roleIds.Count == 0)
        {
            return ApplicationResult.Success();
        }

        var existingRoleIds = await _dbContext.Roles
            .Where(role => roleIds.Contains(role.Id) && role.IsActive)
            .Select(role => role.Id)
            .ToArrayAsync(cancellationToken);

        return existingRoleIds.Length == roleIds.Count
            ? ApplicationResult.Success()
            : ApplicationResult.NotFound("Role was not found.");
    }

    private Task<string[]> LoadRoleCodesAsync(
        IEnumerable<Guid> roleIds,
        CancellationToken cancellationToken)
    {
        var ids = roleIds.Distinct().ToArray();

        if (ids.Length == 0)
        {
            return Task.FromResult(Array.Empty<string>());
        }

        return _dbContext.Roles
            .AsNoTracking()
            .Where(role => ids.Contains(role.Id))
            .OrderBy(role => role.Code)
            .Select(role => role.Code)
            .ToArrayAsync(cancellationToken);
    }

    private async Task<bool> IsLastActiveAdministratorAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var userIsActiveAdministrator = await _dbContext.UserRoles
            .AnyAsync(
                userRole => userRole.UserId == userId
                    && userRole.RoleId == IdentityCatalog.RoleIds.AdministratorId
                    && userRole.User.Status == UserStatus.Active,
                cancellationToken);

        if (!userIsActiveAdministrator)
        {
            return false;
        }

        var activeAdministratorCount = await _dbContext.UserRoles
            .CountAsync(
                userRole => userRole.RoleId == IdentityCatalog.RoleIds.AdministratorId
                    && userRole.User.Status == UserStatus.Active,
                cancellationToken);

        return activeAdministratorCount <= 1;
    }

    private static Guid[] NormalizeRoleIds(IReadOnlyCollection<Guid>? roleIds)
    {
        return (roleIds ?? Array.Empty<Guid>())
            .Distinct()
            .ToArray();
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }

    private static string BuildDisplayName(User user)
    {
        var displayName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(displayName) ? user.Email : displayName;
    }

    private static ApplicationResult EmailConflict()
    {
        return ApplicationResult.Conflict("A user with the same email already exists.");
    }

    private static ApplicationResult<T> EmailConflict<T>()
    {
        return ApplicationResult<T>.Conflict("A user with the same email already exists.");
    }
}
