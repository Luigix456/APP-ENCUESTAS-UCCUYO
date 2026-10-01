using AcademicSurveySystem.Domain.Academic.Entities;
using AcademicSurveySystem.Domain.Academic.Enums;
using AcademicSurveySystem.Domain.Common;

namespace AcademicSurveySystem.UnitTests.Academic;

public sealed class CareerTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset UpdatedAtUtc =
        new(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CreatesCareer_WhenValuesAreValid()
    {
        var career = CreateCareer();

        Assert.NotEqual(Guid.Empty, career.AcademicUnitId);
        Assert.Equal("sis", career.Code);
        Assert.Equal("Sistemas", career.Name);
        Assert.Equal(CareerType.Undergraduate, career.Type);
        Assert.True(career.IsActive);
        Assert.Equal(CreatedAtUtc, career.CreatedAtUtc);
        Assert.Equal(CreatedAtUtc, career.UpdatedAtUtc);
    }

    [Fact]
    public void Constructor_NormalizesCode()
    {
        var career = CreateCareer(code: " SIS.2026 ");

        Assert.Equal("sis.2026", career.Code);
    }

    [Fact]
    public void Constructor_RejectsEmptyCode()
    {
        Assert.Throws<DomainException>(() => CreateCareer(code: " "));
    }

    [Fact]
    public void Constructor_RejectsInvalidCode()
    {
        Assert.Throws<DomainException>(() => CreateCareer(code: "sis 2026"));
    }

    [Fact]
    public void Constructor_RejectsEmptyName()
    {
        Assert.Throws<DomainException>(() => CreateCareer(name: " "));
    }

    [Fact]
    public void Constructor_RejectsEmptyAcademicUnitId()
    {
        Assert.Throws<DomainException>(() => CreateCareer(academicUnitId: Guid.Empty));
    }

    [Fact]
    public void ActivateAndDeactivate_UpdateState()
    {
        var career = CreateCareer();

        career.Deactivate(UpdatedAtUtc);
        Assert.False(career.IsActive);

        career.Activate(UpdatedAtUtc.AddDays(1));
        Assert.True(career.IsActive);
    }

    [Fact]
    public void UpdateName_UpdatesUpdatedAtUtc()
    {
        var career = CreateCareer();

        career.UpdateName("Ingeniería en Sistemas", UpdatedAtUtc);

        Assert.Equal("Ingeniería en Sistemas", career.Name);
        Assert.Equal(UpdatedAtUtc, career.UpdatedAtUtc);
    }

    [Fact]
    public void Constructor_RejectsNonUtcDate()
    {
        Assert.Throws<DomainException>(() =>
            CreateCareer(createdAtUtc: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(-3))));
    }

    private static Career CreateCareer(
        Guid? academicUnitId = null,
        string code = "SIS",
        string name = "Sistemas",
        CareerType type = CareerType.Undergraduate,
        DateTimeOffset? createdAtUtc = null)
    {
        return new Career(
            Guid.NewGuid(),
            academicUnitId ?? Guid.NewGuid(),
            code,
            name,
            type,
            createdAtUtc ?? CreatedAtUtc);
    }
}
