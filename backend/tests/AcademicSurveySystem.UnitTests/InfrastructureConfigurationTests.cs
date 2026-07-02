using AcademicSurveySystem.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AcademicSurveySystem.UnitTests;

public sealed class InfrastructureConfigurationTests
{
    public static TheoryData<string?> InvalidConnectionStrings => new()
    {
        null,
        string.Empty,
        "   "
    };

    [Theory]
    [MemberData(nameof(InvalidConnectionStrings))]
    public void AddInfrastructure_ThrowsClearException_WhenDefaultConnectionIsInvalid(
        string? connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString
            })
            .Build();

        var services = new ServiceCollection();

        void Act() => services.AddInfrastructure(configuration);

        var exception = Assert.Throws<InvalidOperationException>((Action)Act);

        Assert.Contains("DefaultConnection", exception.Message);
    }
}
