using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AcademicSurveySystem.Application.Audit;
using AcademicSurveySystem.Application.Common.Authentication;
using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace AcademicSurveySystem.IntegrationTests;

public sealed class AuditHttpTests
{
    private const string Issuer = "AcademicSurveySystem.Tests";
    private const string Audience = "AcademicSurveySystem.Tests";
    private const string SigningKey = "TEST_SIGNING_KEY_WITH_AT_LEAST_32_CHARS";

    [Fact]
    public async Task Audit_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/audit");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Audit_WithoutAuditReadPermission_ReturnsForbidden()
    {
        using var factory = CreateFactory();
        using var client = CreateAuthenticatedClient(factory, ["identity.users.read"]);

        var response = await client.GetAsync("/api/audit");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Audit_WithAuditReadPermission_ReturnsPagedEntriesAndPassesFilters()
    {
        using var factory = CreateFactory();
        using var client = CreateAuthenticatedClient(factory, ["audit.read"]);
        var actorUserId = Guid.NewGuid();

        var response = await client.GetAsync(
            $"/api/audit?page=2&pageSize=50&module=identity&action=identity.user.created&entityType=User&actorUserId={actorUserId}&search=maria");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<AuditEntriesPageDto>();
        Assert.NotNull(page);
        Assert.Equal(2, page.Page);
        Assert.Equal(50, page.PageSize);
        Assert.Equal(1, page.TotalItems);
        var entry = Assert.Single(page.Items);
        Assert.Equal("identity.user.created", entry.Action);
        Assert.Equal("identity", entry.Module);
        Assert.Equal("administrator", entry.Metadata!["roleCodes"]![0]!.GetValue<string>());

        var filter = FakeAuditQueryService.LastFilter;
        Assert.NotNull(filter);
        Assert.Equal(2, filter.Page);
        Assert.Equal(50, filter.PageSize);
        Assert.Equal("identity", filter.Module);
        Assert.Equal("identity.user.created", filter.Action);
        Assert.Equal("User", filter.EntityType);
        Assert.Equal(actorUserId, filter.ActorUserId);
        Assert.Equal("maria", filter.Search);
    }

    [Fact]
    public async Task Audit_WithAuditReadPermission_AcceptsPageSize25WithoutFilters()
    {
        using var factory = CreateFactory();
        using var client = CreateAuthenticatedClient(factory, ["audit.read"]);

        var response = await client.GetAsync("/api/audit?page=1&pageSize=25");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var filter = FakeAuditQueryService.LastFilter;
        Assert.NotNull(filter);
        Assert.Equal(1, filter.Page);
        Assert.Equal(25, filter.PageSize);
        Assert.Null(filter.Module);
        Assert.Null(filter.Action);
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
                    services.RemoveAll<IAuditQueryService>();
                    services.AddScoped<IAuditQueryService, FakeAuditQueryService>();
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
            Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"),
            "Audit",
            "User",
            "audit@example.com",
            [],
            permissions));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            token.TokenType,
            token.AccessToken);

        return client;
    }

    private sealed class FakeAuditQueryService : IAuditQueryService
    {
        public static AuditEntryFilter? LastFilter { get; private set; }

        public Task<ApplicationResult<AuditEntriesPageDto>> GetEntriesAsync(
            AuditEntryFilter filter,
            CancellationToken cancellationToken)
        {
            LastFilter = filter;
            return Task.FromResult(ApplicationResult<AuditEntriesPageDto>.Success(new AuditEntriesPageDto(
                [
                    new AuditEntryDto(
                        Guid.NewGuid(),
                        new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero),
                        Guid.NewGuid(),
                        "Admin User",
                        "identity.user.created",
                        "identity",
                        "User",
                        Guid.NewGuid(),
                        "Creó el usuario Maria Gomez.",
                        JsonNode.Parse("""{"roleCodes":["administrator"]}"""))
                ],
                filter.Page,
                filter.PageSize,
                1,
                1)));
        }
    }
}
