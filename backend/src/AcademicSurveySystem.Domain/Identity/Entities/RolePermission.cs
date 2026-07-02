using AcademicSurveySystem.Domain.Common;

namespace AcademicSurveySystem.Domain.Identity.Entities;

public sealed class RolePermission
{
    private RolePermission()
    {
    }

    public RolePermission(Guid roleId, Guid permissionId, DateTimeOffset grantedAtUtc)
    {
        if (roleId == Guid.Empty)
        {
            throw new DomainException("RoleId is required.");
        }

        if (permissionId == Guid.Empty)
        {
            throw new DomainException("PermissionId is required.");
        }

        EnsureUtc(grantedAtUtc, nameof(grantedAtUtc));

        RoleId = roleId;
        PermissionId = permissionId;
        GrantedAtUtc = grantedAtUtc;
    }

    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }
    public DateTimeOffset GrantedAtUtc { get; private set; }

    public Role Role { get; private set; } = null!;
    public Permission Permission { get; private set; } = null!;

    private static void EnsureUtc(DateTimeOffset value, string fieldName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException($"{fieldName} must be UTC.");
        }
    }
}
