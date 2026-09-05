using AcademicSurveySystem.Application.Academic;
using AcademicSurveySystem.Application.Common.Security;
using AcademicSurveySystem.Application.Identity.Authentication;
using AcademicSurveySystem.Application.Identity.InitialAdministrator;
using AcademicSurveySystem.Application.Surveys;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AcademicSurveySystem.IntegrationTests;

public sealed class ApplicationStartupTests
{
    [Fact]
    public void ApiHost_CanBeBuilt_WithoutInitialAdminValues()
    {
        using var factory = CreateFactory();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.NotNull(dbContext);
    }

    [Fact]
    public void ApiHost_UsesPostgreSqlProvider_ForApplicationDbContext()
    {
        using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.Equal(
            "Npgsql.EntityFrameworkCore.PostgreSQL",
            dbContext.Database.ProviderName);
    }

    [Fact]
    public void ApiHost_ResolvesPasswordHasher()
    {
        using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();

        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        Assert.NotNull(passwordHasher);
    }

    [Fact]
    public void ApiHost_ResolvesJwtTokenGenerator()
    {
        using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();

        var tokenGenerator = scope.ServiceProvider.GetRequiredService<
            AcademicSurveySystem.Application.Common.Authentication.IJwtTokenGenerator>();

        Assert.NotNull(tokenGenerator);
    }

    [Fact]
    public void ApiHost_ResolvesAuthenticationService()
    {
        using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();

        var authenticationService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();

        Assert.NotNull(authenticationService);
    }

    [Fact]
    public void ApiHost_ResolvesAcademicCatalogService()
    {
        using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();

        var academicCatalogService = scope.ServiceProvider.GetRequiredService<IAcademicCatalogService>();

        Assert.NotNull(academicCatalogService);
    }

    [Fact]
    public void ApiHost_ResolvesSurveyTemplateService()
    {
        using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();

        var surveyTemplateService = scope.ServiceProvider.GetRequiredService<ISurveyTemplateService>();

        Assert.NotNull(surveyTemplateService);
    }

    [Fact]
    public void ApiHost_ResolvesInitialAdministratorBootstrapper()
    {
        using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();

        var bootstrapper = scope.ServiceProvider.GetRequiredService<IInitialAdministratorBootstrapper>();

        Assert.NotNull(bootstrapper);
    }

    [Fact]
    public void ApiHost_InvalidWorkFactor_FailsWithClearMessage()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Security:PasswordHashing:WorkFactor"] = "9"
        });

        var exception = Assert.Throws<OptionsValidationException>(() =>
        {
            _ = factory.Services;
        });

        Assert.Contains("WorkFactor", exception.Message);
    }

    [Fact]
    public void ApiHost_MissingJwtSigningKey_FailsWithClearMessage()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Jwt:SigningKey"] = string.Empty
        });

        var exception = Assert.ThrowsAny<Exception>(() =>
        {
            _ = factory.Services;
        });

        Assert.Contains("SigningKey", exception.ToString());
    }

    [Fact]
    public void ApiHost_ShortJwtSigningKey_FailsWithClearMessage()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Jwt:SigningKey"] = "short-key"
        });

        var exception = Assert.ThrowsAny<Exception>(() =>
        {
            _ = factory.Services;
        });

        Assert.Contains("SigningKey", exception.ToString());
    }

    [Fact]
    public async Task AuthMe_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Careers_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/academic/careers");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AcademicCycles_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/academic/academic-cycles");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Subjects_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/academic/subjects");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Teachers_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/academic/teachers");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TeacherSubjectAssignments_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/academic/teacher-subject-assignments");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Surveys_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/surveys");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IReadOnlyDictionary<string, string?>? overrides = null)
    {
        var configuration = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] =
                "Host=localhost;Port=5432;Database=academic_survey_db;Username=postgres;Password=postgres",
            ["Security:PasswordHashing:WorkFactor"] = "12",
            ["Jwt:Issuer"] = "AcademicSurveySystem.Tests",
            ["Jwt:Audience"] = "AcademicSurveySystem.Tests",
            ["Jwt:SigningKey"] = "TEST_SIGNING_KEY_WITH_AT_LEAST_32_CHARS",
            ["Jwt:AccessTokenExpirationMinutes"] = "60"
        };

        if (overrides is not null)
        {
            foreach (var item in overrides)
            {
                configuration[item.Key] = item.Value;
            }
        }

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                    configBuilder.AddInMemoryCollection(configuration));
                builder.ConfigureLogging(logging => logging.ClearProviders());
            });
    }
}
