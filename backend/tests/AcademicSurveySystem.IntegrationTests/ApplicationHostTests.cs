using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AcademicSurveySystem.IntegrationTests;

public sealed class ApplicationStartupTests
{
    [Fact]
    public void ApiHost_CanBeBuilt_AndResolveApplicationDbContext_WithoutOpeningDatabaseConnection()
    {
        var previousConnectionString = Environment.GetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection");

        try
        {
            Environment.SetEnvironmentVariable(
                "ConnectionStrings__DefaultConnection",
                "Host=localhost;Port=5432;Database=academic_survey_db;Username=postgres;Password=postgres");

            using var factory = new WebApplicationFactory<Program>();
            using var scope = factory.Services.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            Assert.NotNull(dbContext);
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                "ConnectionStrings__DefaultConnection",
                previousConnectionString);
        }
    }

    [Fact]
    public void ApiHost_UsesPostgreSqlProvider_ForApplicationDbContext()
    {
        var previousConnectionString = Environment.GetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection");

        try
        {
            Environment.SetEnvironmentVariable(
                "ConnectionStrings__DefaultConnection",
                "Host=localhost;Port=5432;Database=academic_survey_db;Username=postgres;Password=postgres");

            using var factory = new WebApplicationFactory<Program>();
            using var scope = factory.Services.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            Assert.Equal(
                "Npgsql.EntityFrameworkCore.PostgreSQL",
                dbContext.Database.ProviderName);
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                "ConnectionStrings__DefaultConnection",
                previousConnectionString);
        }
    }
}
