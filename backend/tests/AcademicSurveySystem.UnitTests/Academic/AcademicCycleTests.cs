using AcademicSurveySystem.Domain.Academic.Entities;
using AcademicSurveySystem.Domain.Academic.Enums;
using AcademicSurveySystem.Domain.Common;

namespace AcademicSurveySystem.UnitTests.Academic;

public sealed class AcademicCycleTests
{
    private static readonly DateOnly StartDate = new(2026, 3, 1);
    private static readonly DateOnly EndDate = new(2026, 12, 15);
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedAtUtc =
        new(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CreatesAcademicCycle_WhenValuesAreValid()
    {
        var cycle = CreateCycle();

        Assert.Equal(2026, cycle.Year);
        Assert.Equal(AcademicCyclePeriod.Annual, cycle.Period);
        Assert.Equal(StartDate, cycle.StartDate);
        Assert.Equal(EndDate, cycle.EndDate);
        Assert.True(cycle.IsActive);
    }

    [Fact]
    public void Constructor_RejectsYearLowerThan2000()
    {
        Assert.Throws<DomainException>(() => CreateCycle(year: 1999));
    }

    [Fact]
    public void Constructor_RejectsYearGreaterThan2100()
    {
        Assert.Throws<DomainException>(() => CreateCycle(year: 2101));
    }

    [Fact]
    public void Constructor_RejectsStartDateGreaterThanEndDate()
    {
        Assert.Throws<DomainException>(() =>
            CreateCycle(startDate: EndDate, endDate: StartDate));
    }

    [Fact]
    public void ActivateAndDeactivate_UpdateState()
    {
        var cycle = CreateCycle();

        cycle.Deactivate(UpdatedAtUtc);
        Assert.False(cycle.IsActive);

        cycle.Activate(UpdatedAtUtc.AddDays(1));
        Assert.True(cycle.IsActive);
    }

    [Fact]
    public void UpdatePeriodAndDates_UpdatesValuesAndUpdatedAtUtc()
    {
        var cycle = CreateCycle();
        var newStartDate = new DateOnly(2026, 8, 1);
        var newEndDate = new DateOnly(2026, 11, 30);

        cycle.UpdatePeriodAndDates(
            AcademicCyclePeriod.SecondSemester,
            newStartDate,
            newEndDate,
            UpdatedAtUtc);

        Assert.Equal(AcademicCyclePeriod.SecondSemester, cycle.Period);
        Assert.Equal(newStartDate, cycle.StartDate);
        Assert.Equal(newEndDate, cycle.EndDate);
        Assert.Equal(UpdatedAtUtc, cycle.UpdatedAtUtc);
    }

    private static AcademicCycle CreateCycle(
        int year = 2026,
        AcademicCyclePeriod period = AcademicCyclePeriod.Annual,
        DateOnly? startDate = null,
        DateOnly? endDate = null)
    {
        return new AcademicCycle(
            Guid.NewGuid(),
            year,
            period,
            startDate ?? StartDate,
            endDate ?? EndDate,
            CreatedAtUtc);
    }
}
