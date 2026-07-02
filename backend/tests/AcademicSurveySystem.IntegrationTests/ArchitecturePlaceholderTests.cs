using AcademicSurveySystem.Infrastructure;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AcademicSurveySystem.IntegrationTests;

public sealed class ApplicationStartupTests
{
    [Fact]
    public void ServiceProvider_CanBeBuilt_WithInfrastructureConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Port=5432;Database=academic_survey_db;Username=postgres;Password=postgres"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);

        using var serviceProvider = services.BuildServiceProvider();

        Assert.NotNull(serviceProvider.GetRequiredService<ApplicationDbContext>());
    }
}
