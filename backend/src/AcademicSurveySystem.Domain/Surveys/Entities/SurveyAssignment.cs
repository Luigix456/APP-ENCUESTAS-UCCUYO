using AcademicSurveySystem.Domain.Academic.Entities;
using AcademicSurveySystem.Domain.Common;

namespace AcademicSurveySystem.Domain.Surveys.Entities;

public sealed class SurveyAssignment
{
    private SurveyAssignment()
    {
    }

    public SurveyAssignment(
        Guid id,
        Guid surveyId,
        Guid careerId,
        Guid subjectId,
        Guid academicCycleId,
        Guid teacherSubjectAssignmentId,
        DateTimeOffset createdAtUtc)
        : this(
            id,
            surveyId,
            careerId,
            subjectId,
            academicCycleId,
            teacherSubjectAssignmentId,
            null,
            createdAtUtc)
    {
    }

    public SurveyAssignment(
        Guid id,
        Guid surveyId,
        Guid careerId,
        Guid subjectId,
        Guid academicCycleId,
        Guid teacherSubjectAssignmentId,
        int? expectedRespondentCount,
        DateTimeOffset createdAtUtc)
    {
        EnsureRequired(id, nameof(Id));
        EnsureRequired(surveyId, nameof(SurveyId));
        EnsureRequired(careerId, nameof(CareerId));
        EnsureRequired(subjectId, nameof(SubjectId));
        EnsureRequired(academicCycleId, nameof(AcademicCycleId));
        EnsureRequired(teacherSubjectAssignmentId, nameof(TeacherSubjectAssignmentId));
        EnsureExpectedRespondentCount(expectedRespondentCount);
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        Id = id;
        SurveyId = surveyId;
        CareerId = careerId;
        SubjectId = subjectId;
        AcademicCycleId = academicCycleId;
        TeacherSubjectAssignmentId = teacherSubjectAssignmentId;
        ExpectedRespondentCount = expectedRespondentCount;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid SurveyId { get; private set; }
    public Guid CareerId { get; private set; }
    public Guid SubjectId { get; private set; }
    public Guid AcademicCycleId { get; private set; }
    public Guid TeacherSubjectAssignmentId { get; private set; }
    public int? ExpectedRespondentCount { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Survey Survey { get; private set; } = null!;
    public Career Career { get; private set; } = null!;
    public Subject Subject { get; private set; } = null!;
    public AcademicCycle AcademicCycle { get; private set; } = null!;
    public TeacherSubjectAssignment TeacherSubjectAssignment { get; private set; } = null!;

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

    private static void EnsureRequired(Guid value, string fieldName)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException($"{fieldName} is required.");
        }
    }

    private static void EnsureExpectedRespondentCount(int? value)
    {
        if (value is <= 0)
        {
            throw new DomainException("ExpectedRespondentCount must be greater than zero.");
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
