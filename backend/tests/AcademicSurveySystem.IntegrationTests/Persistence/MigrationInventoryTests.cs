using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AcademicSurveySystem.IntegrationTests.Persistence;

public sealed class MigrationInventoryTests
{
    [Fact]
    public void ApplicationDbContext_HasExpectedMigrationsWithoutSurveyTemplateEndpointMigration()
    {
        using var context = CreateContext();

        var migrations = context.Database.GetMigrations().ToArray();

        Assert.Equal(
            [
                "20260702154244_InitialInfrastructure",
                "20260702162821_AddIdentityCore",
                "20260702164345_SeedIdentityCatalog",
                "20260903191756_AddAcademicCatalog",
                "20260903235455_AddSurveyCore",
                "20260907164458_AddSurveyAssignments",
                "20260909213944_AddSurveySessions",
                "20260922193237_AddSurveyResponses",
                "20260923171235_AddUserCareerAssignments",
                "20260924050231_AddRatingScaleBounds",
                "20260924052902_AddSurveyAnswerOtherText",
                "20261001041359_AddSurveyTemplateVersioning",
                "20261001090000_AddAcademicUnits"
            ],
            migrations);
    }

    [Fact]
    public void ApplicationDbContext_DoesNotUseEfInMemoryProvider()
    {
        using var context = CreateContext();

        Assert.NotEqual("Microsoft.EntityFrameworkCore.InMemory", context.Database.ProviderName);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=academic_survey_db;Username=postgres;Password=postgres")
            .Options;

        return new ApplicationDbContext(options);
    }
}
