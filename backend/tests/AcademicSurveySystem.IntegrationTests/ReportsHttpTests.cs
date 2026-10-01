using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AcademicSurveySystem.Application.Common.Authentication;
using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Surveys.Reports;
using AcademicSurveySystem.Application.Surveys.Results;
using AcademicSurveySystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace AcademicSurveySystem.IntegrationTests;

public sealed class ReportsHttpTests
{
    private const string Issuer = "AcademicSurveySystem.Tests";
    private const string Audience = "AcademicSurveySystem.Tests";
    private const string SigningKey = "TEST_SIGNING_KEY_WITH_AT_LEAST_32_CHARS";

    [Fact]
    public async Task Preview_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/reports/survey-assignments/{FakeReportData.AssignmentAId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Preview_WithoutResultsPermission_ReturnsForbidden()
    {
        using var factory = CreateFactory();
        using var client = CreateAuthenticatedClient(factory, ["academic.catalog.read"]);

        var response = await client.GetAsync($"/api/reports/survey-assignments/{FakeReportData.AssignmentAId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Preview_WithReadAll_ReturnsReport()
    {
        using var factory = CreateFactory();
        using var client = CreateAuthenticatedClient(factory, ["results.read_all"]);

        var response = await client.GetAsync($"/api/reports/survey-assignments/{FakeReportData.AssignmentBId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertCacheNoStore(response);
        var report = await response.Content.ReadFromJsonAsync<SurveyReportDto>();
        Assert.NotNull(report);
        Assert.Equal(FakeReportData.AssignmentBId, report.SurveyAssignmentId);
    }

    [Fact]
    public async Task Preview_WithReadCareerInsideScope_ReturnsReport()
    {
        using var factory = CreateFactory();
        using var client = CreateAuthenticatedClient(factory, ["results.read_career"]);

        var response = await client.GetAsync($"/api/reports/survey-assignments/{FakeReportData.AssignmentAId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Preview_WithReadCareerOutsideScope_ReturnsForbidden()
    {
        using var factory = CreateFactory();
        using var client = CreateAuthenticatedClient(factory, ["results.read_career"]);

        var response = await client.GetAsync($"/api/reports/survey-assignments/{FakeReportData.AssignmentBId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Pdf_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/reports/survey-assignments/{FakeReportData.AssignmentAId}/pdf");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Pdf_WithoutReportsExport_ReturnsForbidden()
    {
        using var factory = CreateFactory();
        using var client = CreateAuthenticatedClient(factory, ["results.read_all"]);

        var response = await client.GetAsync($"/api/reports/survey-assignments/{FakeReportData.AssignmentAId}/pdf");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Pdf_WithReportsExportButWithoutResultsPermission_ReturnsForbidden()
    {
        using var factory = CreateFactory();
        using var client = CreateAuthenticatedClient(factory, ["reports.export"]);

        var response = await client.GetAsync($"/api/reports/survey-assignments/{FakeReportData.AssignmentAId}/pdf");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Pdf_WithReadAllAndReportsExport_ReturnsPdf()
    {
        using var factory = CreateFactory();
        using var client = CreateAuthenticatedClient(factory, ["results.read_all", "reports.export"]);

        var response = await client.GetAsync($"/api/reports/survey-assignments/{FakeReportData.AssignmentBId}/pdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        AssertCacheNoStore(response);
        Assert.EndsWith(".pdf", response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName);
        var content = await response.Content.ReadAsByteArrayAsync();
        Assert.True(content.Length > 4);
        Assert.Equal("%PDF"u8.ToArray(), content[..4]);
    }

    [Fact]
    public async Task Pdf_WithReadCareerAndReportsExportInsideScope_ReturnsPdf()
    {
        using var factory = CreateFactory();
        using var client = CreateAuthenticatedClient(factory, ["results.read_career", "reports.export"]);

        var response = await client.GetAsync($"/api/reports/survey-assignments/{FakeReportData.AssignmentAId}/pdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Pdf_WithReadCareerAndReportsExportOutsideScope_ReturnsForbidden()
    {
        using var factory = CreateFactory();
        using var client = CreateAuthenticatedClient(factory, ["results.read_career", "reports.export"]);

        var response = await client.GetAsync($"/api/reports/survey-assignments/{FakeReportData.AssignmentBId}/pdf");

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
                    services.RemoveAll<IReportService>();
                    services.AddScoped<IReportService, FakeReportService>();
                    services.RemoveAll<IPdfReportGenerator>();
                    services.AddScoped<IPdfReportGenerator, FakePdfReportGenerator>();
                    services.RemoveAll<IResultsAccessService>();
                    services.AddScoped<IResultsAccessService, FakeResultsAccessService>();
                });
            });
    }

    private static void AssertCacheNoStore(HttpResponseMessage response)
    {
        Assert.NotNull(response.Headers.CacheControl);
        Assert.True(response.Headers.CacheControl.NoStore);
        Assert.True(response.Headers.CacheControl.Private);
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
            FakeReportData.UserId,
            "Reports",
            "User",
            "reports@example.com",
            [],
            permissions));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            token.TokenType,
            token.AccessToken);

        return client;
    }

    private static class FakeReportData
    {
        public static readonly Guid UserId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
        public static readonly Guid AssignmentAId = Guid.Parse("aaaaaaaa-0000-0000-0000-0000000000a1");
        public static readonly Guid AssignmentBId = Guid.Parse("bbbbbbbb-0000-0000-0000-0000000000b1");
    }

    private sealed class FakeResultsAccessService : IResultsAccessService
    {
        public Task<ResultsAccessScope> GetDiscoveryScopeAsync(
            Guid userId,
            bool hasReadAll,
            bool hasReadCareer,
            CancellationToken cancellationToken) =>
            Task.FromResult(hasReadAll
                ? ResultsAccessScope.All()
                : hasReadCareer
                    ? ResultsAccessScope.Career([Guid.NewGuid()])
                    : ResultsAccessScope.Forbidden());

        public Task<ResultsAccessDecision> AuthorizeSurveyAssignmentAsync(
            Guid userId,
            bool hasReadAll,
            bool hasReadCareer,
            Guid surveyAssignmentId,
            CancellationToken cancellationToken)
        {
            if (hasReadAll || (hasReadCareer && surveyAssignmentId == FakeReportData.AssignmentAId))
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
            Task.FromResult(ResultsAccessDecision.Forbidden());
    }

    private sealed class FakeReportService : IReportService
    {
        public Task<ApplicationResult<SurveyReportDto>> BuildSurveyAssignmentReportAsync(
            Guid surveyAssignmentId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(ApplicationResult<SurveyReportDto>.Success(new SurveyReportDto(
                new ReportInstitutionDto(
                    "Universidad Católica de Cuyo",
                    "Sistema Web de Gestión de Encuestas Académicas",
                    "Facultad de Ciencias Económicas y Empresariales"),
                DateTimeOffset.UtcNow,
                surveyAssignmentId,
                Guid.NewGuid(),
                "Encuesta",
                2,
                Guid.NewGuid(),
                "Carrera",
                Guid.NewGuid(),
                "Materia",
                Guid.NewGuid(),
                2026,
                "Annual",
                Guid.NewGuid(),
                "Ada Lovelace",
                "Titular",
                0,
                0,
                null,
                null,
                [])));
        }
    }

    private sealed class FakePdfReportGenerator : IPdfReportGenerator
    {
        public Task<ApplicationResult<GeneratedReportFileDto>> GenerateSurveyAssignmentReportAsync(
            SurveyReportDto report,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(ApplicationResult<GeneratedReportFileDto>.Success(new GeneratedReportFileDto(
                "Informe_Encuesta_v2.pdf",
                "application/pdf",
                "%PDF-1.7 fake"u8.ToArray())));
        }
    }
}
