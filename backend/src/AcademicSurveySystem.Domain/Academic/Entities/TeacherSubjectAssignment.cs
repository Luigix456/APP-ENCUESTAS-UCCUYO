using AcademicSurveySystem.Domain.Common;

namespace AcademicSurveySystem.Domain.Academic.Entities;

public sealed class TeacherSubjectAssignment
{
    private TeacherSubjectAssignment()
    {
        TeachingRole = null!;
    }

    public TeacherSubjectAssignment(
        Guid id,
        Guid teacherId,
        Guid subjectId,
        Guid academicCycleId,
        string teachingRole,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("TeacherSubjectAssignment id is required.");
        }

        if (teacherId == Guid.Empty)
        {
            throw new DomainException("TeacherId is required.");
        }

        if (subjectId == Guid.Empty)
        {
            throw new DomainException("SubjectId is required.");
        }

        if (academicCycleId == Guid.Empty)
        {
            throw new DomainException("AcademicCycleId is required.");
        }

        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        Id = id;
        TeacherId = teacherId;
        SubjectId = subjectId;
        AcademicCycleId = academicCycleId;
        TeachingRole = NormalizeRequiredText(teachingRole, nameof(TeachingRole), 100);
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid TeacherId { get; private set; }
    public Guid SubjectId { get; private set; }
    public Guid AcademicCycleId { get; private set; }
    public string TeachingRole { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Teacher Teacher { get; private set; } = null!;
    public Subject Subject { get; private set; } = null!;
    public AcademicCycle AcademicCycle { get; private set; } = null!;

    public void UpdateTeachingRole(string teachingRole, DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        TeachingRole = NormalizeRequiredText(teachingRole, nameof(TeachingRole), 100);
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Activate(DateTimeOffset updatedAtUtc)
    {
        ChangeActiveState(true, updatedAtUtc);
    }

    public void Deactivate(DateTimeOffset updatedAtUtc)
    {
        ChangeActiveState(false, updatedAtUtc);
    }

    private void ChangeActiveState(bool isActive, DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        IsActive = isActive;
        UpdatedAtUtc = updatedAtUtc;
    }

    private static string NormalizeRequiredText(string value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{fieldName} must be {maxLength} characters or fewer.");
        }

        return normalized;
    }

    private static void EnsureUtc(DateTimeOffset value, string fieldName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException($"{fieldName} must be UTC.");
        }
    }
}
