using System.Text.RegularExpressions;
using AcademicSurveySystem.Domain.Academic.Enums;
using AcademicSurveySystem.Domain.Common;

namespace AcademicSurveySystem.Domain.Academic.Entities;

public sealed partial class Subject
{
    private readonly List<TeacherSubjectAssignment> _teacherSubjectAssignments = [];

    private Subject()
    {
        Code = null!;
        Name = null!;
    }

    public Subject(
        Guid id,
        Guid careerId,
        string code,
        string name,
        int year,
        SubjectPeriod period,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Subject id is required.");
        }

        if (careerId == Guid.Empty)
        {
            throw new DomainException("CareerId is required.");
        }

        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        EnsureYear(year);
        EnsureDefined(period, nameof(Period));

        Id = id;
        CareerId = careerId;
        Code = NormalizeCode(code, nameof(Code));
        Name = NormalizeRequiredText(name, nameof(Name), 200);
        Year = year;
        Period = period;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid CareerId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public int Year { get; private set; }
    public SubjectPeriod Period { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Career Career { get; private set; } = null!;
    public IReadOnlyCollection<TeacherSubjectAssignment> TeacherSubjectAssignments =>
        _teacherSubjectAssignments.AsReadOnly();

    public void UpdateName(string name, DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

        Name = NormalizeRequiredText(name, nameof(Name), 200);
        UpdatedAtUtc = updatedAtUtc;
    }

    public void UpdateYearAndPeriod(
        int year,
        SubjectPeriod period,
        DateTimeOffset updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        EnsureYear(year);
        EnsureDefined(period, nameof(Period));

        Year = year;
        Period = period;
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
        if (year is < 1 or > 10)
        {
            throw new DomainException("Year must be between 1 and 10.");
        }
    }

    private static string NormalizeCode(string value, string fieldName)
    {
        var normalized = NormalizeRequiredText(value, fieldName, 50).ToLowerInvariant();

        if (!CodeRegex().IsMatch(normalized))
        {
            throw new DomainException($"{fieldName} format is invalid.");
        }

        return normalized;
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

    private static void EnsureDefined(SubjectPeriod value, string fieldName)
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

    [GeneratedRegex("^[a-z0-9._-]+$")]
    private static partial Regex CodeRegex();
}
