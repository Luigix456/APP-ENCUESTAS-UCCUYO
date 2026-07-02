using AcademicSurveySystem.Domain.Common;

namespace AcademicSurveySystem.Domain.Identity.Entities;

public sealed class UserRole
{
    private UserRole()
    {
    }

    public UserRole(Guid userId, Guid roleId, DateTimeOffset assignedAtUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("UserId is required.");
        }

        if (roleId == Guid.Empty)
        {
            throw new DomainException("RoleId is required.");
        }

        EnsureUtc(assignedAtUtc, nameof(assignedAtUtc));

        UserId = userId;
        RoleId = roleId;
        AssignedAtUtc = assignedAtUtc;
    }

    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTimeOffset AssignedAtUtc { get; private set; }

    public User User { get; private set; } = null!;
    public Role Role { get; private set; } = null!;

    private static void EnsureUtc(DateTimeOffset value, string fieldName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException($"{fieldName} must be UTC.");
        }
    }
}
