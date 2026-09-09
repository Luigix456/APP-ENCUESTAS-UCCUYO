using AcademicSurveySystem.Application.Surveys.Assignments;
using AcademicSurveySystem.Domain.Common;
using AcademicSurveySystem.Domain.Surveys.Entities;

namespace AcademicSurveySystem.UnitTests.Surveys;

public sealed class SurveyAssignmentTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset UpdatedAtUtc =
        new(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CreatesAssignment_WhenValuesAreValid()
    {
        var surveyId = Guid.NewGuid();
        var careerId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var academicCycleId = Guid.NewGuid();
        var teacherSubjectAssignmentId = Guid.NewGuid();

        var assignment = new SurveyAssignment(
            Guid.NewGuid(),
            surveyId,
            careerId,
            subjectId,
            academicCycleId,
            teacherSubjectAssignmentId,
            CreatedAtUtc);

        Assert.Equal(surveyId, assignment.SurveyId);
        Assert.Equal(careerId, assignment.CareerId);
        Assert.Equal(subjectId, assignment.SubjectId);
        Assert.Equal(academicCycleId, assignment.AcademicCycleId);
        Assert.Equal(teacherSubjectAssignmentId, assignment.TeacherSubjectAssignmentId);
        Assert.True(assignment.IsActive);
        Assert.Equal(CreatedAtUtc, assignment.CreatedAtUtc);
        Assert.Equal(CreatedAtUtc, assignment.UpdatedAtUtc);
    }

    [Fact]
    public void Constructor_RejectsEmptySurveyId()
    {
        Assert.Throws<DomainException>(() => CreateAssignment(surveyId: Guid.Empty));
    }

    [Fact]
    public void Constructor_RejectsEmptyCareerId()
    {
        Assert.Throws<DomainException>(() => CreateAssignment(careerId: Guid.Empty));
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
    public void Constructor_RejectsEmptyTeacherSubjectAssignmentId()
    {
        Assert.Throws<DomainException>(() =>
            CreateAssignment(teacherSubjectAssignmentId: Guid.Empty));
    }

    [Fact]
    public void ActivateAndDeactivate_UpdateStateAndTimestamp()
    {
        var assignment = CreateAssignment();

        assignment.Deactivate(UpdatedAtUtc);

        Assert.False(assignment.IsActive);
        Assert.Equal(UpdatedAtUtc, assignment.UpdatedAtUtc);

        var reactivatedAtUtc = UpdatedAtUtc.AddDays(1);

        assignment.Activate(reactivatedAtUtc);

        Assert.True(assignment.IsActive);
        Assert.Equal(reactivatedAtUtc, assignment.UpdatedAtUtc);
    }

    [Fact]
    public void Constructor_RejectsNonUtcCreatedAt()
    {
        Assert.Throws<DomainException>(() =>
            CreateAssignment(createdAtUtc: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(-3))));
    }

    [Fact]
    public void CreateRequest_RejectsEmptyGuids()
    {
        var request = new CreateSurveyAssignmentRequest(
            Guid.Empty,
            Guid.Empty,
            Guid.Empty,
            Guid.Empty,
            Guid.Empty);

        var errors = request.Validate();

        Assert.Contains(errors, error => error.Code == "SurveyAssignment.SurveyIdRequired");
        Assert.Contains(errors, error => error.Code == "SurveyAssignment.CareerIdRequired");
        Assert.Contains(errors, error => error.Code == "SurveyAssignment.SubjectIdRequired");
        Assert.Contains(errors, error => error.Code == "SurveyAssignment.AcademicCycleIdRequired");
        Assert.Contains(
            errors,
            error => error.Code == "SurveyAssignment.TeacherSubjectAssignmentIdRequired");
    }

    [Fact]
    public void CreateRequest_AcceptsValidGuids()
    {
        var request = new CreateSurveyAssignmentRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

        var errors = request.Validate();

        Assert.Empty(errors);
    }

    private static SurveyAssignment CreateAssignment(
        Guid? surveyId = null,
        Guid? careerId = null,
        Guid? subjectId = null,
        Guid? academicCycleId = null,
        Guid? teacherSubjectAssignmentId = null,
        DateTimeOffset? createdAtUtc = null)
    {
        return new SurveyAssignment(
            Guid.NewGuid(),
            surveyId ?? Guid.NewGuid(),
            careerId ?? Guid.NewGuid(),
            subjectId ?? Guid.NewGuid(),
            academicCycleId ?? Guid.NewGuid(),
            teacherSubjectAssignmentId ?? Guid.NewGuid(),
            createdAtUtc ?? CreatedAtUtc);
    }
}
