using AcademicSurveySystem.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AcademicSurveySystem.UnitTests;

public sealed class InfrastructureConfigurationTests
{
    [Fact]
    public void AddInfrastructure_Throws_WhenDefaultConnectionIsEmpty()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = string.Empty
            })
            .Build();

        var services = new ServiceCollection();

        void Act() => services.AddInfrastructure(configuration);

        var exception = Assert.Throws<InvalidOperationException>((Action)Act);

        Assert.Contains("DefaultConnection", exception.Message);
    }
}
