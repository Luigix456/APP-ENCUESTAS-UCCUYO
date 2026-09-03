using AcademicSurveySystem.Domain.Academic.Enums;
using AcademicSurveySystem.Domain.Common;

namespace AcademicSurveySystem.Domain.Academic.Entities;

public sealed class AcademicCycle
{
    private readonly List<TeacherSubjectAssignment> _teacherSubjectAssignments = [];

    private AcademicCycle()
    {
    }

    public AcademicCycle(
        Guid id,
        int year,
        AcademicCyclePeriod period,
        DateOnly startDate,
        DateOnly endDate,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("AcademicCycle id is required.");
        }

        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        EnsureYear(year);
        EnsureDefined(period, nameof(Period));
        EnsureDateRange(startDate, endDate);

        Id = id;
        Year = year;
        Period = period;
        StartDate = startDate;
        EndDate = endDate;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public int Year { get; private set; }
    public AcademicCyclePeriod Period { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public IReadOnlyCollection<TeacherSubjectAssignment> TeacherSubjectAssignments =>
        _teacherSubjectAssignments.AsReadOnly();

    public void UpdatePeriodAndDates(
        AcademicCyclePeriod period,
        DateOnly startDate,
        DateOnly endDate,
        DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        EnsureDefined(period, nameof(Period));
        EnsureDateRange(startDate, endDate);

        Period = period;
        StartDate = startDate;
        EndDate = endDate;
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

    private static void EnsureYear(int year)
    {
        if (year is < 2000 or > 2100)
        {
            throw new DomainException("Year must be between 2000 and 2100.");
        }
    }

    private static void EnsureDateRange(DateOnly startDate, DateOnly endDate)
    {
        if (startDate > endDate)
        {
            throw new DomainException("StartDate must be less than or equal to EndDate.");
        }
    }

    private static void EnsureDefined(AcademicCyclePeriod value, string fieldName)
    {
        if (!Enum.IsDefined(value))
        {
            throw new DomainException($"{fieldName} is invalid.");
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
