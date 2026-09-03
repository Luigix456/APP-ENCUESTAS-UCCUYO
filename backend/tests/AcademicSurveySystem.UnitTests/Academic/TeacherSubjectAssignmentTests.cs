using AcademicSurveySystem.Domain.Academic.Entities;
using AcademicSurveySystem.Domain.Common;

namespace AcademicSurveySystem.UnitTests.Academic;

public sealed class TeacherSubjectAssignmentTests
{
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly Guid SubjectId = Guid.NewGuid();
    private static readonly Guid AcademicCycleId = Guid.NewGuid();
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedAtUtc =
        new(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CreatesAssignment_WhenValuesAreValid()
    {
        var assignment = CreateAssignment();

        Assert.Equal(TeacherId, assignment.TeacherId);
        Assert.Equal(SubjectId, assignment.SubjectId);
        Assert.Equal(AcademicCycleId, assignment.AcademicCycleId);
        Assert.Equal("Titular", assignment.TeachingRole);
        Assert.True(assignment.IsActive);
    }

    [Fact]
    public void Constructor_RejectsEmptyTeacherId()
    {
        Assert.Throws<DomainException>(() => CreateAssignment(teacherId: Guid.Empty));
    }

    [Fact]
    public void Constructor_RejectsEmptySubjectId()
    {
        Assert.Throws<DomainException>(() => CreateAssignment(subjectId: Guid.Empty));
    }

    [Fact]
    public void Constructor_RejectsEmptyAcademicCycleId()
    {
        Assert.Throws<DomainException>(() => CreateAssignment(academicCycleId: Guid.Empty));
    }

    [Fact]
    public void Constructor_RejectsEmptyTeachingRole()
    {
        Assert.Throws<DomainException>(() => CreateAssignment(teachingRole: " "));
    }

    [Fact]
    public void ActivateAndDeactivate_UpdateState()
    {
        var assignment = CreateAssignment();

        assignment.Deactivate(UpdatedAtUtc);
        Assert.False(assignment.IsActive);

        assignment.Activate(UpdatedAtUtc.AddDays(1));
        Assert.True(assignment.IsActive);
    }

    [Fact]
    public void UpdateTeachingRole_UpdatesValueAndUpdatedAtUtc()
    {
        var assignment = CreateAssignment();

        assignment.UpdateTeachingRole("Adjunta", UpdatedAtUtc);

        Assert.Equal("Adjunta", assignment.TeachingRole);
        Assert.Equal(UpdatedAtUtc, assignment.UpdatedAtUtc);
    }

    private static TeacherSubjectAssignment CreateAssignment(
        Guid? teacherId = null,
        Guid? subjectId = null,
        Guid? academicCycleId = null,
        string teachingRole = "Titular")
    {
        return new TeacherSubjectAssignment(
            Guid.NewGuid(),
            teacherId ?? TeacherId,
            subjectId ?? SubjectId,
            academicCycleId ?? AcademicCycleId,
            teachingRole,
            CreatedAtUtc);
    }
}
