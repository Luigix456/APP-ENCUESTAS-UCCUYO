using AcademicSurveySystem.Domain.Academic.Entities;
using AcademicSurveySystem.Domain.Common;

namespace AcademicSurveySystem.UnitTests.Academic;

public sealed class AcademicUnitTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset UpdatedAtUtc =
        new(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CreatesAcademicUnit_WhenValuesAreValid()
    {
        var unit = CreateAcademicUnit();

        Assert.Equal("economicas", unit.Code);
        Assert.Equal("Facultad de Ciencias Economicas y Empresariales", unit.Name);
        Assert.True(unit.IsActive);
        Assert.Equal(CreatedAtUtc, unit.CreatedAtUtc);
        Assert.Equal(CreatedAtUtc, unit.UpdatedAtUtc);
    }

    [Fact]
    public void Constructor_RejectsEmptyCode()
    {
        Assert.Throws<DomainException>(() => CreateAcademicUnit(code: " "));
    }

    [Fact]
    public void Constructor_RejectsInvalidCode()
    {
        Assert.Throws<DomainException>(() => CreateAcademicUnit(code: "facultad economicas"));
    }

    [Fact]
    public void Constructor_RejectsEmptyName()
    {
        Assert.Throws<DomainException>(() => CreateAcademicUnit(name: " "));
    }

    [Fact]
    public void UpdateName_UpdatesNameAndTimestamp()
    {
        var unit = CreateAcademicUnit();

        unit.UpdateName("Facultad de Ciencias Economicas", UpdatedAtUtc);

        Assert.Equal("Facultad de Ciencias Economicas", unit.Name);
        Assert.Equal(UpdatedAtUtc, unit.UpdatedAtUtc);
    }

    [Fact]
    public void ActivateAndDeactivate_UpdateState()
    {
        var unit = CreateAcademicUnit();

        unit.Deactivate(UpdatedAtUtc);
        Assert.False(unit.IsActive);

        unit.Activate(UpdatedAtUtc.AddDays(1));
        Assert.True(unit.IsActive);
    }

    private static AcademicUnit CreateAcademicUnit(
        string code = "ECONOMICAS",
        string name = "Facultad de Ciencias Economicas y Empresariales")
    {
        return new AcademicUnit(
            Guid.NewGuid(),
            code,
            name,
            CreatedAtUtc);
    }
}
