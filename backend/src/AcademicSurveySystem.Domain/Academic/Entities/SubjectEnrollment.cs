using AcademicSurveySystem.Domain.Common;

namespace AcademicSurveySystem.Domain.Academic.Entities;

public sealed class SubjectEnrollment
{
    private SubjectEnrollment()
    {
    }

    public SubjectEnrollment(
        Guid id,
        Guid subjectId,
        Guid academicCycleId,
        int enrolledStudentCount,
        DateTimeOffset createdAtUtc)
    {
        EnsureRequired(id, nameof(Id));
        EnsureRequired(subjectId, nameof(SubjectId));
        EnsureRequired(academicCycleId, nameof(AcademicCycleId));
        EnsureEnrolledStudentCount(enrolledStudentCount);
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        Id = id;
        SubjectId = subjectId;
        AcademicCycleId = academicCycleId;
        EnrolledStudentCount = enrolledStudentCount;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid SubjectId { get; private set; }
    public Guid AcademicCycleId { get; private set; }
    public int EnrolledStudentCount { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Subject Subject { get; private set; } = null!;
    public AcademicCycle AcademicCycle { get; private set; } = null!;

    public void UpdateEnrolledStudentCount(
        int enrolledStudentCount,
        DateTimeOffset updatedAtUtc)
    {
        EnsureEnrolledStudentCount(enrolledStudentCount);
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        EnrolledStudentCount = enrolledStudentCount;
        UpdatedAtUtc = updatedAtUtc;
    }

    private static void EnsureRequired(Guid value, string fieldName)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException($"{fieldName} is required.");
        }
    }

    private static void EnsureEnrolledStudentCount(int value)
    {
        if (value <= 0)
        {
            throw new DomainException("EnrolledStudentCount must be greater than zero.");
        }
    }

    private static void EnsureUtc(DateTimeOffset value, string fieldName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException($"{fieldName} must be UTC.");
        }
    }
}
