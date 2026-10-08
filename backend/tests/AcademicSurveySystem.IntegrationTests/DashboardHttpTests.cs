using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AcademicSurveySystem.Application.Common.Authentication;
using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Dashboard;
using AcademicSurveySystem.Application.Surveys.Results;
using AcademicSurveySystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace AcademicSurveySystem.IntegrationTests;

public sealed class DashboardHttpTests
{
    private const string Issuer = "AcademicSurveySystem.Tests";
    private const string Audience = "AcademicSurveySystem.Tests";
    private const string SigningKey = "TEST_SIGNING_KEY_WITH_AT_LEAST_32_CHARS";

    [Fact]
    public async Task GetCareerParticipation_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(BuildUrl(FakeDashboardData.CareerAId));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCareerParticipation_WithoutResultsPermission_ReturnsForbidden()
    {
        using var factory = CreateFactory();
        using var client = CreateAuthenticatedClient(factory, ["surveys.sessions.manage"]);

        var response = await client.GetAsync(BuildUrl(FakeDashboardData.CareerAId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetCareerParticipation_WithReadCareerInsideScope_ReturnsDashboard()
    {
        using var factory = CreateFactory();
        using var client = CreateAuthenticatedClient(factory, ["results.read_career"]);

        var response = await client.GetAsync(BuildUrl(FakeDashboardData.CareerAId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dashboard = await response.Content.ReadFromJsonAsync<CareerParticipationDashboardDto>();
        Assert.NotNull(dashboard);
        Assert.Equal(FakeDashboardData.CareerAId, dashboard.CareerId);
    }

    [Fact]
    public async Task GetCareerParticipation_WithReadCareerOutsideScope_ReturnsForbidden()
    {
        using var factory = CreateFactory();
        using var client = CreateAuthenticatedClient(factory, ["results.read_career"]);

        var response = await client.GetAsync(BuildUrl(FakeDashboardData.CareerBId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetCareerParticipation_WithReadAll_ReturnsDashboardOutsideCareerScope()
    {
        using var factory = CreateFactory();
        using var client = CreateAuthenticatedClient(factory, ["results.read_all"]);

        var response = await client.GetAsync(BuildUrl(FakeDashboardData.CareerBId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dashboard = await response.Content.ReadFromJsonAsync<CareerParticipationDashboardDto>();
        Assert.NotNull(dashboard);
        Assert.Equal(FakeDashboardData.CareerBId, dashboard.CareerId);
    }

    private static string BuildUrl(Guid careerId)
    {
        return $"/api/dashboard/careers/{careerId}?academicCycleId={FakeDashboardData.AcademicCycleId}";
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        var configuration = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] =
                "Host=localhost;Port=5432;Database=academic_survey_db;Username=postgres;Password=postgres",
            ["Security:PasswordHashing:WorkFactor"] = "12",
            ["Jwt:Issuer"] = Issuer,
            ["Jwt:Audience"] = Audience,
            ["Jwt:SigningKey"] = SigningKey,
            ["Jwt:AccessTokenExpirationMinutes"] = "60"
        };

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                    configBuilder.AddInMemoryCollection(configuration));

                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<ICareerParticipationDashboardService>();
                    services.AddScoped<ICareerParticipationDashboardService, FakeDashboardService>();
                    services.RemoveAll<IResultsAccessService>();
                    services.AddScoped<IResultsAccessService, FakeResultsAccessService>();
                });
            });
    }

    private static HttpClient CreateAuthenticatedClient(
        WebApplicationFactory<Program> factory,
        IReadOnlyCollection<string> permissions)
    {
        var client = factory.CreateClient();
        var generator = new JwtTokenGenerator(Options.Create(new JwtOptions
        {
            Issuer = Issuer,
            Audience = Audience,
            SigningKey = SigningKey,
            AccessTokenExpirationMinutes = 60
        }));
        var token = generator.GenerateToken(new AuthenticatedUser(
            FakeDashboardData.UserId,
            "Dashboard",
            "User",
            "dashboard@example.com",
            [],
            permissions));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            token.TokenType,
            token.AccessToken);

        return client;
    }

    private static class FakeDashboardData
    {
        public static readonly Guid UserId = Guid.Parse("dddddddd-0000-0000-0000-000000000001");
        public static readonly Guid CareerAId = Guid.Parse("dddddddd-0000-0000-0000-0000000000c1");
        public static readonly Guid CareerBId = Guid.Parse("eeeeeeee-0000-0000-0000-0000000000c2");
        public static readonly Guid AcademicCycleId = Guid.Parse("dddddddd-0000-0000-0000-0000000000ac");
    }

    private sealed class FakeDashboardService : ICareerParticipationDashboardService
    {
        public Task<ApplicationResult<CareerParticipationDashboardDto>> GetCareerParticipationAsync(
            Guid careerId,
            Guid academicCycleId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(ApplicationResult<CareerParticipationDashboardDto>.Success(
                new CareerParticipationDashboardDto(
                    careerId,
                    careerId == FakeDashboardData.CareerAId ? "Career A" : "Career B",
                    academicCycleId,
                    "2026 - Annual",
                    TotalSubjects: 1,
                    SubjectsWithResponses: 1,
                    StudentSurveyAssignmentsCount: 1,
                    OpenSessionsCount: 1,
                    AverageParticipationPercentage: 50m,
                    LowParticipationAssignmentsCount: 0,
                    LowParticipationThresholdPercentage: 50m,
                    [])));
        }
    }

    private sealed class FakeResultsAccessService : IResultsAccessService
    {
        public Task<ResultsAccessScope> GetDiscoveryScopeAsync(
            Guid userId,
            bool hasReadAll,
            bool hasReadCareer,
            CancellationToken cancellationToken)
        {
            if (hasReadAll)
            {
                return Task.FromResult(ResultsAccessScope.All());
            }

            if (hasReadCareer)
            {
                return Task.FromResult(ResultsAccessScope.Career([FakeDashboardData.CareerAId]));
            }

            return Task.FromResult(ResultsAccessScope.Forbidden());
        }

        public Task<ResultsAccessDecision> AuthorizeSurveyAssignmentAsync(
            Guid userId,
            bool hasReadAll,
            bool hasReadCareer,
            Guid surveyAssignmentId,
            CancellationToken cancellationToken) =>
            Task.FromResult(hasReadAll || hasReadCareer
                ? ResultsAccessDecision.Allowed()
                : ResultsAccessDecision.Forbidden());

        public Task<ResultsAccessDecision> AuthorizeSurveySessionAsync(
            Guid userId,
            bool hasReadAll,
            bool hasReadCareer,
            Guid surveySessionId,
            CancellationToken cancellationToken) =>
            Task.FromResult(hasReadAll || hasReadCareer
                ? ResultsAccessDecision.Allowed()
                : ResultsAccessDecision.Forbidden());
    }
}
