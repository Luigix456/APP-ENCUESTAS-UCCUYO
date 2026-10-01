using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Identity.UserManagement;

public sealed record UpdateUserRolesRequest(IReadOnlyCollection<Guid>? RoleIds)
{
    public IReadOnlyCollection<ApplicationError> Validate()
    {
        var errors = new List<ApplicationError>();

        if (RoleIds is null)
        {
            errors.Add(new ApplicationError("Identity.RoleIdsRequired", "Role ids are required."));
            return errors;
        }

        if (RoleIds.Any(roleId => roleId == Guid.Empty))
        {
            errors.Add(new ApplicationError("Identity.RoleIdRequired", "Role ids must not be empty."));
        }

        if (RoleIds.Count != RoleIds.Distinct().Count())
        {
            errors.Add(new ApplicationError("Identity.DuplicateRoleId", "Role ids must not contain duplicates."));
        }

        return errors;
    }
}
