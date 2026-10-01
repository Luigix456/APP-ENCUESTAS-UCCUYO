using AcademicSurveySystem.Application.Academic;
using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Common.Security;
using AcademicSurveySystem.Application.Identity.AdminPasswordReset;
using AcademicSurveySystem.Application.Identity.Authentication;
using AcademicSurveySystem.Application.Identity.InitialAdministrator;
using AcademicSurveySystem.Application.Identity.UserCareers;
using AcademicSurveySystem.Application.Identity.UserManagement;
using AcademicSurveySystem.Application.Surveys;
using AcademicSurveySystem.Application.Surveys.Assignments;
using AcademicSurveySystem.Application.Surveys.Responses;
using AcademicSurveySystem.Application.Surveys.Reports;
using AcademicSurveySystem.Application.Surveys.Results;
using AcademicSurveySystem.Application.Surveys.Sessions;
using AcademicSurveySystem.Infrastructure.Persistence;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
    public void ApiHost_ResolvesAdminPasswordResetService()
    {
        using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();

        var passwordResetService = scope.ServiceProvider.GetRequiredService<IAdminPasswordResetService>();

        Assert.NotNull(passwordResetService);
    }

    [Fact]
    public void ApiHost_ResolvesAdminPasswordResetStore()
    {
        using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();

        var passwordResetStore = scope.ServiceProvider.GetRequiredService<IAdminPasswordResetStore>();

        Assert.NotNull(passwordResetStore);
    }

    [Fact]
    public void ApiHost_ResolvesUserCareerService()
    {
        using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();

        var userCareerService = scope.ServiceProvider.GetRequiredService<IUserCareerService>();

        Assert.NotNull(userCareerService);
    }

    [Fact]
    public void ApiHost_ResolvesUserManagementService()
    {
        using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();

        var userManagementService = scope.ServiceProvider.GetRequiredService<IUserManagementService>();

        Assert.NotNull(userManagementService);
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
    public void ApiHost_ResolvesSurveyAssignmentService()
    {
        using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();

        var surveyAssignmentService = scope.ServiceProvider.GetRequiredService<ISurveyAssignmentService>();

        Assert.NotNull(surveyAssignmentService);
    }

    [Fact]
    public void ApiHost_ResolvesSurveySessionService()
    {
        using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();

        var surveySessionService = scope.ServiceProvider.GetRequiredService<ISurveySessionService>();

        Assert.NotNull(surveySessionService);
    }

    [Fact]
    public void ApiHost_ResolvesSurveySessionAccessCodeGenerator()
    {
        using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();

        var accessCodeGenerator = scope.ServiceProvider.GetRequiredService<ISurveySessionAccessCodeGenerator>();

        Assert.NotNull(accessCodeGenerator);
    }

    [Fact]
    public void ApiHost_ResolvesSurveyResponseService()
    {
        using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();

        var surveyResponseService = scope.ServiceProvider.GetRequiredService<ISurveyResponseService>();

        Assert.NotNull(surveyResponseService);
    }

    [Fact]
    public void ApiHost_ResolvesSurveyResultsService()
    {
        using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();

        var surveyResultsService = scope.ServiceProvider.GetRequiredService<ISurveyResultsService>();

        Assert.NotNull(surveyResultsService);
    }

    [Fact]
    public void ApiHost_ResolvesReportServices()
    {
        using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();

        var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();
        var pdfReportGenerator = scope.ServiceProvider.GetRequiredService<IPdfReportGenerator>();

        Assert.NotNull(reportService);
        Assert.NotNull(pdfReportGenerator);
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
    public async Task UserCareers_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/identity/users/{Guid.NewGuid()}/careers");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task IdentityUsers_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/identity/users");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task IdentityRoles_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/identity/roles");

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

    [Fact]
    public async Task SurveyOptionUpdate_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/surveys/{Guid.NewGuid()}/sections/{Guid.NewGuid()}/questions/{Guid.NewGuid()}/options/{Guid.NewGuid()}",
            new
            {
                text = "Excelente",
                value = "excellent",
                order = 1
            });

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SurveyMatrixRowUpdate_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/surveys/{Guid.NewGuid()}/sections/{Guid.NewGuid()}/questions/{Guid.NewGuid()}/matrix-rows/{Guid.NewGuid()}",
            new
            {
                text = "Claridad",
                order = 1
            });

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SurveyAssignments_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/survey-assignments");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SurveySessions_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/survey-sessions");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Results_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/results/survey-assignments/{Guid.NewGuid()}/summary");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Reports_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/reports/survey-assignments/{Guid.NewGuid()}");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PublicSurveySessions_WithoutToken_DoesNotRequireAuthorization()
    {
        using var factory = CreateFactory(useFakeSurveySessionService: true);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/public/survey-sessions/codigo-inexistente");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PublicSurveyResponses_WithoutToken_DoesNotRequireAuthorization()
    {
        using var factory = CreateFactory(useFakeSurveySessionService: true);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/public/survey-sessions/codigo-valido/responses",
            new SubmitSurveyResponseRequest([]));

        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IReadOnlyDictionary<string, string?>? overrides = null,
        bool useFakeSurveySessionService = false)
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
                if (useFakeSurveySessionService)
                {
                    builder.ConfigureTestServices(services =>
                    {
                        services.RemoveAll<ISurveySessionService>();
                        services.AddScoped<ISurveySessionService, NotFoundSurveySessionService>();
                        services.RemoveAll<ISurveyResponseService>();
                        services.AddScoped<ISurveyResponseService, SuccessfulSurveyResponseService>();
                    });
                }

                builder.ConfigureLogging(logging => logging.ClearProviders());
            });
    }

    private sealed class NotFoundSurveySessionService : ISurveySessionService
    {
        public Task<ApplicationResult<IReadOnlyCollection<SurveySessionDto>>> GetSessionsAsync(
            bool includeInactive,
            string? status,
            Guid? surveyAssignmentId,
            string? accessCode,
            Guid? careerId,
            Guid? academicCycleId,
            Guid? subjectId,
            Guid? teacherId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<SurveySessionDto>> GetSessionByIdAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<SurveySessionDto>> CreateSessionAsync(
            CreateSurveySessionRequest request,
            Guid createdByUserId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> UpdateSessionAsync(
            Guid id,
            UpdateSurveySessionRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> OpenSessionAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> CloseSessionAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> CancelSessionAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> ActivateSessionAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> DeactivateSessionAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<PublicSurveySessionDto>> GetPublicSessionByAccessCodeAsync(
            string accessCode,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                ApplicationResult<PublicSurveySessionDto>.NotFound(
                    "Survey session was not found."));
    }

    private sealed class SuccessfulSurveyResponseService : ISurveyResponseService
    {
        public Task<ApplicationResult<SurveyResponseSubmissionDto>> SubmitResponseAsync(
            string accessCode,
            SubmitSurveyResponseRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                ApplicationResult<SurveyResponseSubmissionDto>.Success(
                    new SurveyResponseSubmissionDto(
                        Guid.NewGuid(),
                        new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.Zero))));
        }
    }
}
