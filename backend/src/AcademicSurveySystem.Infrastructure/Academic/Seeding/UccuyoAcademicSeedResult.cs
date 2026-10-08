namespace AcademicSurveySystem.Infrastructure.Academic.Seeding;

public sealed record UccuyoAcademicSeedResult(
    int AcademicUnitsCreated,
    int CareersCreated,
    int AcademicUnitsExisting,
    int CareersExisting,
    IReadOnlyCollection<string> Warnings);
