using AcademicSurveySystem.Domain.Academic.Entities;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AcademicSurveySystem.Infrastructure.Academic.Seeding;

public sealed class UccuyoAcademicCatalogSeeder
{
    private readonly ApplicationDbContext _dbContext;

    public UccuyoAcademicCatalogSeeder(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UccuyoAcademicSeedResult> SeedAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var existingUnits = await _dbContext.AcademicUnits
            .ToListAsync(cancellationToken);
        var existingCareers = await _dbContext.Careers
            .ToListAsync(cancellationToken);
        var warnings = new List<string>();

        var unitResolutions = ResolveUnits(existingUnits, now, warnings);
        ValidateCareerConflicts(existingCareers, unitResolutions);

        if (_dbContext.Database.IsRelational())
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            var result = await InsertMissingAsync(
                existingCareers,
                unitResolutions,
                now,
                warnings,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return result;
        }

        return await InsertMissingAsync(
            existingCareers,
            unitResolutions,
            now,
            warnings,
            cancellationToken);
    }

    private async Task<UccuyoAcademicSeedResult> InsertMissingAsync(
        IReadOnlyCollection<Career> existingCareers,
        IReadOnlyCollection<UnitResolution> unitResolutions,
        DateTimeOffset now,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var academicUnitsCreated = 0;
        var academicUnitsExisting = 0;
        var careersCreated = 0;
        var careersExisting = 0;
        var careersByCode = existingCareers.ToDictionary(career => career.Code, StringComparer.OrdinalIgnoreCase);
        var careers = existingCareers.ToList();

        foreach (var resolution in unitResolutions)
        {
            if (resolution.IsNew)
            {
                _dbContext.AcademicUnits.Add(resolution.Unit);
                academicUnitsCreated++;
            }
            else
            {
                academicUnitsExisting++;
            }

            foreach (var careerSeed in resolution.Seed.Careers)
            {
                if (careersByCode.TryGetValue(careerSeed.Code, out var careerByCode))
                {
                    careersExisting++;
                    AddPreservedCareerWarning(careerSeed, careerByCode, warnings);
                    continue;
                }

                var careerByName = careers.FirstOrDefault(career =>
                    career.AcademicUnitId == resolution.Unit.Id
                    && NamesMatch(career.Name, careerSeed.Name));

                if (careerByName is not null)
                {
                    careersExisting++;
                    warnings.Add(
                        $"Career '{careerByName.Name}' already exists in academic unit '{resolution.Unit.Code}' with code '{careerByName.Code}'. Seed code '{careerSeed.Code}' was not inserted.");
                    continue;
                }

                var career = new Career(
                    Guid.NewGuid(),
                    resolution.Unit.Id,
                    careerSeed.Code,
                    careerSeed.Name,
                    careerSeed.Type,
                    now);

                _dbContext.Careers.Add(career);
                careersByCode[career.Code] = career;
                careers.Add(career);
                careersCreated++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UccuyoAcademicSeedResult(
            academicUnitsCreated,
            careersCreated,
            academicUnitsExisting,
            careersExisting,
            warnings.ToArray());
    }

    private static IReadOnlyCollection<UnitResolution> ResolveUnits(
        IReadOnlyCollection<AcademicUnit> existingUnits,
        DateTimeOffset now,
        ICollection<string> warnings)
    {
        var unitsByCode = existingUnits.ToDictionary(unit => unit.Code, StringComparer.OrdinalIgnoreCase);
        var units = existingUnits.ToList();
        var resolutions = new List<UnitResolution>();

        foreach (var unitSeed in UccuyoAcademicCatalog.Units)
        {
            if (unitsByCode.TryGetValue(unitSeed.Code, out var unitByCode))
            {
                if (!string.Equals(unitByCode.Name.Trim(), unitSeed.Name.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    warnings.Add(
                        $"AcademicUnit code '{unitSeed.Code}' already exists with name '{unitByCode.Name}'. Seed name '{unitSeed.Name}' was not applied.");
                }

                resolutions.Add(new UnitResolution(unitSeed, unitByCode, IsNew: false));
                continue;
            }

            var unitByName = units.FirstOrDefault(unit => NamesMatch(unit.Name, unitSeed.Name));

            if (unitByName is not null)
            {
                warnings.Add(
                    $"AcademicUnit '{unitByName.Name}' already exists with code '{unitByName.Code}'. Seed code '{unitSeed.Code}' was not inserted.");
                resolutions.Add(new UnitResolution(unitSeed, unitByName, IsNew: false));
                continue;
            }

            var newUnit = new AcademicUnit(
                Guid.NewGuid(),
                unitSeed.Code,
                unitSeed.Name,
                now);
            resolutions.Add(new UnitResolution(unitSeed, newUnit, IsNew: true));
            unitsByCode[newUnit.Code] = newUnit;
            units.Add(newUnit);
        }

        return resolutions;
    }

    private static void ValidateCareerConflicts(
        IReadOnlyCollection<Career> existingCareers,
        IReadOnlyCollection<UnitResolution> unitResolutions)
    {
        var careersByCode = existingCareers.ToDictionary(career => career.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var resolution in unitResolutions)
        {
            foreach (var careerSeed in resolution.Seed.Careers)
            {
                if (careersByCode.TryGetValue(careerSeed.Code, out var existingCareer)
                    && existingCareer.AcademicUnitId != resolution.Unit.Id)
                {
                    throw new InvalidOperationException(
                        $"Career code '{careerSeed.Code}' already belongs to another AcademicUnit.");
                }
            }
        }
    }

    private static bool NamesMatch(string left, string right) =>
        string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);

    private static void AddPreservedCareerWarning(
        CareerSeed seed,
        Career existingCareer,
        List<string> warnings)
    {
        if (!string.Equals(existingCareer.Name.Trim(), seed.Name.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add(
                $"Career code '{seed.Code}' already exists with name '{existingCareer.Name}'. Seed name '{seed.Name}' was not applied.");
        }

        if (existingCareer.Type != seed.Type)
        {
            warnings.Add(
                $"Career code '{seed.Code}' already exists with type '{existingCareer.Type}'. Seed type '{seed.Type}' was not applied.");
        }
    }

    private sealed record UnitResolution(
        AcademicUnitSeed Seed,
        AcademicUnit Unit,
        bool IsNew);
}
