using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Identity.UserManagement;

public interface IUserManagementService
{
    Task<ApplicationResult<IReadOnlyCollection<UserDto>>> GetUsersAsync(
        bool includeInactive,
        CancellationToken cancellationToken);

    Task<ApplicationResult<UserDto>> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<UserDto>> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> UpdateUserAsync(
        Guid userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResult> ActivateUserAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<ApplicationResult> DeactivateUserAsync(
        Guid userId,
        Guid currentUserId,
        CancellationToken cancellationToken);

    Task<ApplicationResult<IReadOnlyCollection<RoleDto>>> GetRolesAsync(
        CancellationToken cancellationToken);

    Task<ApplicationResult<UserDto>> ReplaceUserRolesAsync(
        Guid userId,
        UpdateUserRolesRequest request,
        CancellationToken cancellationToken);
}
