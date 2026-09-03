using AcademicSurveySystem.Domain.Academic.Entities;
using AcademicSurveySystem.Domain.Academic.Enums;
using AcademicSurveySystem.Domain.Common;

namespace AcademicSurveySystem.UnitTests.Academic;

public sealed class SubjectTests
{
    private static readonly Guid CareerId = Guid.NewGuid();
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedAtUtc =
        new(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CreatesSubject_WhenValuesAreValid()
    {
        var subject = CreateSubject();

        Assert.Equal(CareerId, subject.CareerId);
        Assert.Equal("mat-1", subject.Code);
        Assert.Equal("Matemática", subject.Name);
        Assert.Equal(1, subject.Year);
        Assert.Equal(SubjectPeriod.FirstSemester, subject.Period);
        Assert.True(subject.IsActive);
    }

    [Fact]
    public void Constructor_RejectsEmptyCareerId()
    {
        Assert.Throws<DomainException>(() => CreateSubject(careerId: Guid.Empty));
    }

    [Fact]
    public void Constructor_NormalizesCode()
    {
        var subject = CreateSubject(code: " MAT.1 ");

        Assert.Equal("mat.1", subject.Code);
    }

    [Fact]
    public void Constructor_RejectsYearLowerThanOne()
    {
        Assert.Throws<DomainException>(() => CreateSubject(year: 0));
    }

    [Fact]
    public void Constructor_RejectsYearGreaterThanTen()
    {
        Assert.Throws<DomainException>(() => CreateSubject(year: 11));
    }

    [Fact]
    public void ActivateAndDeactivate_UpdateState()
    {
        var subject = CreateSubject();

        subject.Deactivate(UpdatedAtUtc);
        Assert.False(subject.IsActive);

        subject.Activate(UpdatedAtUtc.AddDays(1));
        Assert.True(subject.IsActive);
    }

    [Fact]
    public void UpdateYearAndPeriod_UpdatesValuesAndUpdatedAtUtc()
    {
        var subject = CreateSubject();

        subject.UpdateYearAndPeriod(2, SubjectPeriod.SecondSemester, UpdatedAtUtc);

        Assert.Equal(2, subject.Year);
        Assert.Equal(SubjectPeriod.SecondSemester, subject.Period);
        Assert.Equal(UpdatedAtUtc, subject.UpdatedAtUtc);
    }

    private static Subject CreateSubject(
        Guid? careerId = null,
        string code = "MAT-1",
        string name = "Matemática",
        int year = 1,
        SubjectPeriod period = SubjectPeriod.FirstSemester)
    {
        return new Subject(
            Guid.NewGuid(),
            careerId ?? CareerId,
            code,
            name,
            year,
            period,
            CreatedAtUtc);
    }
}
