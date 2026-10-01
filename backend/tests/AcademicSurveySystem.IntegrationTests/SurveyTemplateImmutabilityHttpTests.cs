using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademicSurveySystem.Application.Common.Authentication;
using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Surveys;
using AcademicSurveySystem.Application.Surveys.Dtos;
using AcademicSurveySystem.Application.Surveys.Requests;
using AcademicSurveySystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace AcademicSurveySystem.IntegrationTests;

public sealed class SurveyTemplateImmutabilityHttpTests
{
    private const string Issuer = "AcademicSurveySystem.Tests";
    private const string Audience = "AcademicSurveySystem.Tests";
    private const string SigningKey = "TEST_SIGNING_KEY_WITH_AT_LEAST_32_CHARS";

    [Fact]
    public async Task PutOption_WhenSurveyIsPublished_ReturnsBadRequestWithSurveyNotEditable()
    {
        using var factory = CreateFactory();
        using var client = CreateAuthenticatedClient(factory);

        var response = await client.PutAsJsonAsync(
            $"/api/surveys/{Guid.NewGuid()}/sections/{Guid.NewGuid()}/questions/{Guid.NewGuid()}/options/{Guid.NewGuid()}",
            new UpdateSurveyQuestionOptionRequest("Excelente", "excellent", 1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorCodeAsync(response, "Survey.NotEditable");
    }

    [Fact]
    public async Task PutMatrixRow_WhenSurveyIsPublished_ReturnsBadRequestWithSurveyNotEditable()
    {
        using var factory = CreateFactory();
        using var client = CreateAuthenticatedClient(factory);

        var response = await client.PutAsJsonAsync(
            $"/api/surveys/{Guid.NewGuid()}/sections/{Guid.NewGuid()}/questions/{Guid.NewGuid()}/matrix-rows/{Guid.NewGuid()}",
            new UpdateSurveyMatrixRowRequest("Claridad", 1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErrorCodeAsync(response, "Survey.NotEditable");
    }

    [Fact]
    public async Task GetSurvey_StillReturnsOk()
    {
        using var factory = CreateFactory();
        using var client = CreateAuthenticatedClient(factory);

        var response = await client.GetAsync($"/api/surveys/{FakeSurveyTemplateService.SurveyId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
                    services.RemoveAll<ISurveyTemplateService>();
                    services.AddScoped<ISurveyTemplateService, FakeSurveyTemplateService>();
                });
            });
    }

    private static HttpClient CreateAuthenticatedClient(WebApplicationFactory<Program> factory)
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
            Guid.NewGuid(),
            "Admin",
            "User",
            "admin@example.com",
            ["administrator"],
            ["surveys.templates.manage", "surveys.templates.read"]));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            token.TokenType,
            token.AccessToken);

        return client;
    }

    private static async Task AssertErrorCodeAsync(HttpResponseMessage response, string expectedCode)
    {
        using var content = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var errors = content.RootElement.GetProperty("errors");

        Assert.Contains(errors.EnumerateArray(), error =>
            error.GetProperty("code").GetString() == expectedCode);
    }

    private sealed class FakeSurveyTemplateService : ISurveyTemplateService
    {
        public static readonly Guid SurveyId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        private static readonly ApplicationResult<SurveyDetailDto> NotEditable =
            ApplicationResult<SurveyDetailDto>.Validation([
                new ApplicationError("Survey.NotEditable", "Only draft surveys can be modified.")
            ]);

        public Task<ApplicationResult<IReadOnlyCollection<SurveySummaryDto>>> GetSurveysAsync(
            bool includeInactive,
            string? status,
            string? target,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<IReadOnlyCollection<SurveySummaryDto>>.Success([]));

        public Task<ApplicationResult<SurveyDetailDto>> GetSurveyByIdAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            Task.FromResult(ApplicationResult<SurveyDetailDto>.Success(new SurveyDetailDto(
                id,
                Guid.NewGuid(),
                "Encuesta publicada",
                null,
                "Student",
                "Published",
                IsAnonymous: true,
                IsActive: true,
                [],
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow)));

        public Task<ApplicationResult<SurveyDetailDto>> CreateSurveyAsync(
            Guid createdByUserId,
            CreateSurveyRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<SurveyEditableVersionDto>> GetOrCreateEditableVersionAsync(
            Guid surveyId,
            Guid currentUserId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> UpdateSurveyAsync(
            Guid id,
            UpdateSurveyRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> PublishSurveyAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> ArchiveSurveyAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> ActivateSurveyAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> DeactivateSurveyAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<SurveyDetailDto>> AddSectionAsync(
            Guid surveyId,
            CreateSurveySectionRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> UpdateSectionAsync(
            Guid surveyId,
            Guid sectionId,
            UpdateSurveySectionRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> ActivateSectionAsync(
            Guid surveyId,
            Guid sectionId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> DeactivateSectionAsync(
            Guid surveyId,
            Guid sectionId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<SurveyDetailDto>> AddQuestionAsync(
            Guid surveyId,
            Guid sectionId,
            CreateSurveyQuestionRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> UpdateQuestionAsync(
            Guid surveyId,
            Guid sectionId,
            Guid questionId,
            UpdateSurveyQuestionRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> ActivateQuestionAsync(
            Guid surveyId,
            Guid sectionId,
            Guid questionId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> DeactivateQuestionAsync(
            Guid surveyId,
            Guid sectionId,
            Guid questionId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<SurveyDetailDto>> AddOptionAsync(
            Guid surveyId,
            Guid sectionId,
            Guid questionId,
            CreateSurveyQuestionOptionRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<SurveyDetailDto>> UpdateOptionAsync(
            Guid surveyId,
            Guid sectionId,
            Guid questionId,
            Guid optionId,
            UpdateSurveyQuestionOptionRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(NotEditable);

        public Task<ApplicationResult> ActivateOptionAsync(
            Guid surveyId,
            Guid sectionId,
            Guid questionId,
            Guid optionId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> DeactivateOptionAsync(
            Guid surveyId,
            Guid sectionId,
            Guid questionId,
            Guid optionId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<SurveyDetailDto>> AddMatrixRowAsync(
            Guid surveyId,
            Guid sectionId,
            Guid questionId,
            CreateSurveyMatrixRowRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult<SurveyDetailDto>> UpdateMatrixRowAsync(
            Guid surveyId,
            Guid sectionId,
            Guid questionId,
            Guid rowId,
            UpdateSurveyMatrixRowRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(NotEditable);

        public Task<ApplicationResult> ActivateMatrixRowAsync(
            Guid surveyId,
            Guid sectionId,
            Guid questionId,
            Guid rowId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ApplicationResult> DeactivateMatrixRowAsync(
            Guid surveyId,
            Guid sectionId,
            Guid questionId,
            Guid rowId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
