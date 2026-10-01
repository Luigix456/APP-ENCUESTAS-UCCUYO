using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AcademicSurveySystem.Application.Common.Authentication;
using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Surveys.Results;
using AcademicSurveySystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace AcademicSurveySystem.IntegrationTests;

public sealed class ResultsDiscoveryHttpTests
{
    private const string Issuer = "AcademicSurveySystem.Tests";
    private const string Audience = "AcademicSurveySystem.Tests";
    private const string SigningKey = "TEST_SIGNING_KEY_WITH_AT_LEAST_32_CHARS";

    [Fact]
    public async Task GetSurveyAssignments_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/results/survey-assignments");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSurveyAssignments_WithoutResultsPermission_ReturnsForbidden()
    {
        using var factory = CreateFactory();
        using var client = CreateAuthenticatedClient(factory, ["academic.catalog.read"]);

        var response = await client.GetAsync("/api/results/survey-assignments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetSurveyAssignments_WithReadAll_ReturnsAllAssignments()
    {
        using var factory = CreateFactory();
        using var client = CreateAuthenticatedClient(factory, ["results.read_all"]);

        var response = await client.GetAsync("/api/results/survey-assignments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<SurveyAssignmentResultListItemDto[]>();
        Assert.NotNull(items);
        Assert.Contains(items, item => item.SurveyAssignmentId == FakeResultsData.AssignmentAId);
        Assert.Contains(items, item => item.SurveyAssignmentId == FakeResultsData.AssignmentBId);
    }

    [Fact]
    public async Task GetSurveyAssignments_WithReadCareer_ReturnsOnlyScopedAssignments()
    {
        using var factory = CreateFactory();
        using var client = CreateAuthenticatedClient(factory, ["results.read_career"]);

        var response = await client.GetAsync("/api/results/survey-assignments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<SurveyAssignmentResultListItemDto[]>();
        Assert.NotNull(items);
        Assert.Contains(items, item => item.SurveyAssignmentId == FakeResultsData.AssignmentAId);
        Assert.DoesNotContain(items, item => item.SurveyAssignmentId == FakeResultsData.AssignmentBId);
    }

    [Fact]
    public async Task GetSurveyAssignments_WithReadCareerAndOtherCareerFilter_ReturnsEmptyList()
    {
        using var factory = CreateFactory();
        using var client = CreateAuthenticatedClient(factory, ["results.read_career"]);

        var response = await client.GetAsync(
            $"/api/results/survey-assignments?careerId={FakeResultsData.CareerBId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<SurveyAssignmentResultListItemDto[]>();
        Assert.NotNull(items);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetSurveyAssignmentSummary_WithReadCareerOutsideScope_ReturnsForbidden()
    {
        using var factory = CreateFactory();
        using var client = CreateAuthenticatedClient(factory, ["results.read_career"]);

        var response = await client.GetAsync(
            $"/api/results/survey-assignments/{FakeResultsData.AssignmentBId}/summary");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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
                    services.RemoveAll<ISurveyResultsService>();
                    services.AddScoped<ISurveyResultsService, FakeSurveyResultsService>();
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
            FakeResultsData.UserId,
            "Results",
            "User",
            "results@example.com",
            [],
            permissions));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            token.TokenType,
            token.AccessToken);

        return client;
    }

    private static class FakeResultsData
    {
        public static readonly Guid UserId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
        public static readonly Guid AssignmentAId = Guid.Parse("aaaaaaaa-0000-0000-0000-0000000000a1");
        public static readonly Guid AssignmentBId = Guid.Parse("bbbbbbbb-0000-0000-0000-0000000000b1");
        public static readonly Guid CareerAId = Guid.Parse("aaaaaaaa-0000-0000-0000-0000000000c1");
        public static readonly Guid CareerBId = Guid.Parse("bbbbbbbb-0000-0000-0000-0000000000c2");

        public static readonly IReadOnlyCollection<SurveyAssignmentResultListItemDto> Assignments =
        [
            CreateAssignment(AssignmentAId, CareerAId, "Career A"),
            CreateAssignment(AssignmentBId, CareerBId, "Career B")
        ];

        private static SurveyAssignmentResultListItemDto CreateAssignment(
            Guid assignmentId,
            Guid careerId,
            string careerName)
        {
            return new SurveyAssignmentResultListItemDto(
                assignmentId,
                Guid.NewGuid(),
                $"Survey {careerName}",
                careerId,
                careerName,
                Guid.NewGuid(),
                $"Subject {careerName}",
                Guid.NewGuid(),
                2026,
                "Annual",
                Guid.NewGuid(),
                "Docente Resultados",
                "Titular",
                IsActive: true,
                TotalSessions: 1,
                TotalResponses: 2,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow);
        }
    }

    private sealed class FakeSurveyResultsService : ISurveyResultsService
    {
        public Task<ApplicationResult<IReadOnlyCollection<SurveyAssignmentResultListItemDto>>> GetSurveyAssignmentResultsAsync(
            ResultsAccessScope accessScope,
            SurveyAssignmentResultsFilter filter,
            CancellationToken cancellationToken)
        {
            var query = FakeResultsData.Assignments.AsEnumerable();

            if (accessScope.Type == ResultsAccessScopeType.Career)
            {
                query = query.Where(item => accessScope.CareerIds.Contains(item.CareerId));
            }

            if (filter.CareerId is not null)
            {
                query = query.Where(item => item.CareerId == filter.CareerId.Value);
            }

            if (filter.SurveyId is not null)
            {
                query = query.Where(item => item.SurveyId == filter.SurveyId.Value);
            }

            if (filter.SubjectId is not null)
            {
                query = query.Where(item => item.SubjectId == filter.SubjectId.Value);
            }

            if (filter.AcademicCycleId is not null)
            {
                query = query.Where(item => item.AcademicCycleId == filter.AcademicCycleId.Value);
            }

            if (filter.TeacherId is not null)
            {
                query = query.Where(item => item.TeacherId == filter.TeacherId.Value);
            }

            return Task.FromResult(
                ApplicationResult<IReadOnlyCollection<SurveyAssignmentResultListItemDto>>.Success(
                    query.ToArray()));
        }

        public Task<ApplicationResult<SurveyResultsSummaryDto>> GetSurveyAssignmentSummaryAsync(
            Guid surveyAssignmentId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(ApplicationResult<SurveyResultsSummaryDto>.Success(new SurveyResultsSummaryDto(
                surveyAssignmentId,
                Guid.NewGuid(),
                "Survey",
                FakeResultsData.CareerAId,
                "Career A",
                Guid.NewGuid(),
                "Subject",
                Guid.NewGuid(),
                2026,
                "Annual",
                Guid.NewGuid(),
                "Docente",
                TotalResponses: 1,
                TotalSessions: 1,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow)));
        }

        public Task<ApplicationResult<IReadOnlyCollection<SurveyQuestionResultsDto>>> GetSurveyAssignmentQuestionResultsAsync(
            Guid surveyAssignmentId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<SurveyQuestionResultsDto>> GetSurveyAssignmentQuestionResultAsync(
            Guid surveyAssignmentId,
            Guid questionId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<SurveyResultsSummaryDto>> GetSurveySessionSummaryAsync(
            Guid surveySessionId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
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
                return Task.FromResult(ResultsAccessScope.Career([FakeResultsData.CareerAId]));
            }

            return Task.FromResult(ResultsAccessScope.Forbidden());
        }

        public Task<ResultsAccessDecision> AuthorizeSurveyAssignmentAsync(
            Guid userId,
            bool hasReadAll,
            bool hasReadCareer,
            Guid surveyAssignmentId,
            CancellationToken cancellationToken)
        {
            if (hasReadAll || (hasReadCareer && surveyAssignmentId == FakeResultsData.AssignmentAId))
            {
                return Task.FromResult(ResultsAccessDecision.Allowed());
            }

            return Task.FromResult(ResultsAccessDecision.Forbidden());
        }

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
