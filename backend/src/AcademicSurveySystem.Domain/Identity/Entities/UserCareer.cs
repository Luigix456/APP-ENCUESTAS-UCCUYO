using AcademicSurveySystem.Domain.Academic.Entities;
using AcademicSurveySystem.Domain.Common;

namespace AcademicSurveySystem.Domain.Identity.Entities;

public sealed class UserCareer
{
    private UserCareer()
    {
    }

    public UserCareer(
        Guid userId,
        Guid careerId,
        DateTimeOffset assignedAtUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("UserId is required.");
        }

        if (careerId == Guid.Empty)
        {
            throw new DomainException("CareerId is required.");
        }

        EnsureUtc(assignedAtUtc, nameof(assignedAtUtc));

        UserId = userId;
        CareerId = careerId;
        AssignedAtUtc = assignedAtUtc;
    }

    public Guid UserId { get; private set; }
    public Guid CareerId { get; private set; }
    public DateTimeOffset AssignedAtUtc { get; private set; }
    public User User { get; private set; } = null!;
    public Career Career { get; private set; } = null!;

    private static void EnsureUtc(DateTimeOffset value, string fieldName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException($"{fieldName} must be UTC.");
        }
    }
}
