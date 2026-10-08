using AcademicSurveySystem.Domain.Academic.Entities;
using AcademicSurveySystem.Domain.Academic.Enums;
using AcademicSurveySystem.Infrastructure.Academic.Seeding;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AcademicSurveySystem.UnitTests.Academic;

public sealed class UccuyoAcademicCatalogSeederTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Catalog_HasExpectedTotalsAndUniqueCodes()
    {
        var units = UccuyoAcademicCatalog.Units;
        var unitCodes = units.Select(unit => unit.Code).ToArray();
        var careerCodes = units.SelectMany(unit => unit.Careers).Select(career => career.Code).ToArray();

        Assert.Equal(9, units.Count);
        Assert.Equal(62, units.Sum(unit => unit.Careers.Count));
        Assert.Equal(unitCodes.Length, unitCodes.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(careerCodes.Length, careerCodes.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Catalog_CodesSatisfyDomainNormalization()
    {
        foreach (var unitSeed in UccuyoAcademicCatalog.Units)
        {
            var unit = new AcademicUnit(Guid.NewGuid(), unitSeed.Code, unitSeed.Name, CreatedAtUtc);

            Assert.Equal(unitSeed.Code, unit.Code);

            foreach (var careerSeed in unitSeed.Careers)
            {
                var career = new Career(
                    Guid.NewGuid(),
                    unit.Id,
                    careerSeed.Code,
                    careerSeed.Name,
                    careerSeed.Type,
                    CreatedAtUtc);

                Assert.Equal(careerSeed.Code, career.Code);
            }
        }
    }

    [Fact]
    public void Catalog_UsesExpectedCareerTypes()
    {
        var careers = UccuyoAcademicCatalog.Units.SelectMany(unit => unit.Careers).ToArray();

        Assert.Equal(41, careers.Count(career => career.Type == CareerType.Undergraduate));
        Assert.Equal(17, careers.Count(career => career.Type == CareerType.Postgraduate));
        Assert.Equal(4, careers.Count(career => career.Type == CareerType.Course));
        Assert.DoesNotContain(careers, career => career.Type == CareerType.Other);
        Assert.All(
            careers.Where(career => career.Name.StartsWith("Diplomatura", StringComparison.OrdinalIgnoreCase)),
            career => Assert.Equal(CareerType.Course, career.Type));
        Assert.All(
            careers.Where(career =>
                career.Name.Contains("Maestría", StringComparison.OrdinalIgnoreCase)
                || career.Name.Contains("Doctorado", StringComparison.OrdinalIgnoreCase)
                || career.Name.Contains("Especialización", StringComparison.OrdinalIgnoreCase)
                || career.Name.StartsWith("Esp.", StringComparison.OrdinalIgnoreCase)),
            career => Assert.Equal(CareerType.Postgraduate, career.Type));
    }

    [Fact]
    public async Task SeedAsync_WithEmptyDatabase_CreatesExpectedCatalog()
    {
        using var context = CreateContext();
        var seeder = new UccuyoAcademicCatalogSeeder(context);

        var result = await seeder.SeedAsync();

        Assert.Equal(9, result.AcademicUnitsCreated);
        Assert.Equal(62, result.CareersCreated);
        Assert.Equal(0, result.AcademicUnitsExisting);
        Assert.Equal(0, result.CareersExisting);
        Assert.Empty(result.Warnings);
        Assert.Equal(9, await context.AcademicUnits.CountAsync());
        Assert.Equal(62, await context.Careers.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_WhenRunTwice_DoesNotCreateDuplicates()
    {
        using var context = CreateContext();
        var seeder = new UccuyoAcademicCatalogSeeder(context);

        await seeder.SeedAsync();
        var secondRun = await seeder.SeedAsync();

        Assert.Equal(0, secondRun.AcademicUnitsCreated);
        Assert.Equal(0, secondRun.CareersCreated);
        Assert.Equal(9, secondRun.AcademicUnitsExisting);
        Assert.Equal(62, secondRun.CareersExisting);
        Assert.Equal(9, await context.AcademicUnits.CountAsync());
        Assert.Equal(62, await context.Careers.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_WithExistingEconomicasAndTuds_PreservesIdsNamesAndRelations()
    {
        using var context = CreateContext();
        var existingUnit = new AcademicUnit(
            Guid.NewGuid(),
            "economicas",
            "Facultad de Ciencias Económicas y Empresariales",
            CreatedAtUtc);
        existingUnit.Deactivate(CreatedAtUtc.AddMinutes(1));
        var existingTuds = new Career(
            Guid.NewGuid(),
            existingUnit.Id,
            "tuds",
            "Tecnicatura Universitaria en Desarrollo de Software",
            CareerType.Undergraduate,
            CreatedAtUtc);
        var subject = new Subject(
            Guid.NewGuid(),
            existingTuds.Id,
            "programacion-1",
            "Programación I",
            1,
            SubjectPeriod.FirstSemester,
            CreatedAtUtc);
        context.AddRange(existingUnit, existingTuds, subject);
        await context.SaveChangesAsync();
        var seeder = new UccuyoAcademicCatalogSeeder(context);

        var result = await seeder.SeedAsync();

        Assert.Equal(8, result.AcademicUnitsCreated);
        Assert.Equal(61, result.CareersCreated);
        Assert.Equal(1, result.AcademicUnitsExisting);
        Assert.Equal(1, result.CareersExisting);
        Assert.Contains(result.Warnings, warning => warning.Contains("AcademicUnit code 'economicas'", StringComparison.Ordinal));
        Assert.Contains(result.Warnings, warning => warning.Contains("Career code 'tuds'", StringComparison.Ordinal));
        Assert.Equal(9, await context.AcademicUnits.CountAsync());
        Assert.Equal(62, await context.Careers.CountAsync());

        var reloadedUnit = await context.AcademicUnits.SingleAsync(unit => unit.Code == "economicas");
        var reloadedTuds = await context.Careers.SingleAsync(career => career.Code == "tuds");
        var reloadedSubject = await context.Subjects.SingleAsync(item => item.Code == "programacion-1");

        Assert.Equal(existingUnit.Id, reloadedUnit.Id);
        Assert.Equal("Facultad de Ciencias Económicas y Empresariales", reloadedUnit.Name);
        Assert.False(reloadedUnit.IsActive);
        Assert.Equal(existingTuds.Id, reloadedTuds.Id);
        Assert.Equal("Tecnicatura Universitaria en Desarrollo de Software", reloadedTuds.Name);
        Assert.Equal(existingTuds.Id, reloadedSubject.CareerId);
    }

    [Fact]
    public async Task SeedAsync_WhenCareerCodeExistsInAnotherAcademicUnit_FailsWithoutAddingCatalogData()
    {
        using var context = CreateContext();
        var unit = new AcademicUnit(Guid.NewGuid(), "otra", "Otra unidad", CreatedAtUtc);
        var tudsInWrongUnit = new Career(
            Guid.NewGuid(),
            unit.Id,
            "tuds",
            "Tecnicatura Universitaria en Desarrollo de Software",
            CareerType.Undergraduate,
            CreatedAtUtc);
        context.AddRange(unit, tudsInWrongUnit);
        await context.SaveChangesAsync();
        var seeder = new UccuyoAcademicCatalogSeeder(context);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => seeder.SeedAsync());

        Assert.Equal("Career code 'tuds' already belongs to another AcademicUnit.", exception.Message);
        Assert.Equal(1, await context.AcademicUnits.CountAsync());
        Assert.Equal(1, await context.Careers.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_WhenCareerNameExistsInSameUnit_DoesNotDuplicateOrModifyIt()
    {
        using var context = CreateContext();
        var unit = new AcademicUnit(Guid.NewGuid(), "medicas", "Facultad de Cs. Médicas", CreatedAtUtc);
        var existingCareer = new Career(
            Guid.NewGuid(),
            unit.Id,
            "legacy-medicina",
            "Medicina",
            CareerType.Other,
            CreatedAtUtc);
        context.AddRange(unit, existingCareer);
        await context.SaveChangesAsync();
        var seeder = new UccuyoAcademicCatalogSeeder(context);

        var result = await seeder.SeedAsync();

        Assert.Equal(8, result.AcademicUnitsCreated);
        Assert.Equal(61, result.CareersCreated);
        Assert.Equal(1, result.AcademicUnitsExisting);
        Assert.Equal(1, result.CareersExisting);
        Assert.Contains(result.Warnings, warning => warning.Contains("Seed code 'med-medicina' was not inserted.", StringComparison.Ordinal));
        Assert.False(await context.Careers.AnyAsync(career => career.Code == "med-medicina"));

        var reloadedCareer = await context.Careers.SingleAsync(career => career.Code == "legacy-medicina");
        Assert.Equal(existingCareer.Id, reloadedCareer.Id);
        Assert.Equal("Medicina", reloadedCareer.Name);
        Assert.Equal(CareerType.Other, reloadedCareer.Type);
    }

    [Fact]
    public async Task SeedAsync_WhenAcademicUnitNameExistsWithDifferentCode_DoesNotDuplicateOrModifyIt()
    {
        using var context = CreateContext();
        var existingUnit = new AcademicUnit(
            Guid.NewGuid(),
            "legacy-seguridad",
            "Escuela de Seguridad",
            CreatedAtUtc);
        context.AcademicUnits.Add(existingUnit);
        await context.SaveChangesAsync();
        var seeder = new UccuyoAcademicCatalogSeeder(context);

        var result = await seeder.SeedAsync();

        Assert.Equal(8, result.AcademicUnitsCreated);
        Assert.Equal(62, result.CareersCreated);
        Assert.Equal(1, result.AcademicUnitsExisting);
        Assert.Equal(0, result.CareersExisting);
        Assert.Contains(result.Warnings, warning => warning.Contains("Seed code 'seguridad' was not inserted.", StringComparison.Ordinal));
        Assert.False(await context.AcademicUnits.AnyAsync(unit => unit.Code == "seguridad"));

        var reloadedUnit = await context.AcademicUnits.SingleAsync(unit => unit.Code == "legacy-seguridad");
        Assert.Equal(existingUnit.Id, reloadedUnit.Id);
        Assert.Equal("Escuela de Seguridad", reloadedUnit.Name);
        Assert.Equal(4, await context.Careers.CountAsync(career => career.AcademicUnitId == existingUnit.Id));
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
