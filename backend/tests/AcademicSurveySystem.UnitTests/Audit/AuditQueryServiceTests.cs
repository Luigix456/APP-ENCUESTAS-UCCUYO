using AcademicSurveySystem.Application.Audit;
using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Domain.Audit.Entities;
using AcademicSurveySystem.Infrastructure.Audit;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AcademicSurveySystem.UnitTests.Audit;

public sealed class AuditQueryServiceTests
{
    private static readonly DateTimeOffset OccurredAtUtc =
        new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetEntriesAsync_WithPageSize25AndNoFilters_ReturnsEmptyPage()
    {
        using var context = CreateContext();
        var service = new AuditQueryService(context);

        var result = await service.GetEntriesAsync(
            EmptyFilter() with { Page = 1, PageSize = 25 },
            CancellationToken.None);

        AssertSuccess(result);
        Assert.Empty(result.Value!.Items);
        Assert.Equal(0, result.Value.TotalItems);
        Assert.Equal(25, result.Value.PageSize);
    }

    [Fact]
    public async Task GetEntriesAsync_WithCanonicalSurveyModule_ReturnsStoredSurveyModules()
    {
        using var context = CreateContext();
        context.AuditEntries.AddRange(
            CreateEntry("surveys.survey.published", "survey_templates"),
            CreateEntry("survey_assignments.assignment.created", "survey_assignments"),
            CreateEntry("academic.subject.created", "academic_catalog"));
        await context.SaveChangesAsync();
        var service = new AuditQueryService(context);

        var result = await service.GetEntriesAsync(
            EmptyFilter() with { Module = "surveys", PageSize = 25 },
            CancellationToken.None);

        AssertSuccess(result);
        Assert.Equal(2, result.Value!.TotalItems);
        Assert.All(result.Value.Items, item =>
            Assert.Contains(item.Module, new[] { "survey_templates", "survey_assignments" }));
    }

    [Fact]
    public async Task GetEntriesAsync_WithCanonicalAcademicModule_ReturnsStoredAcademicModule()
    {
        using var context = CreateContext();
        context.AuditEntries.AddRange(
            CreateEntry("academic.subject.created", "academic_catalog"),
            CreateEntry("identity.user.created", "identity"));
        await context.SaveChangesAsync();
        var service = new AuditQueryService(context);

        var result = await service.GetEntriesAsync(
            EmptyFilter() with { Module = "academic", PageSize = 25 },
            CancellationToken.None);

        AssertSuccess(result);
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal("academic_catalog", item.Module);
    }

    [Fact]
    public async Task GetEntriesAsync_WithInvalidModule_ReturnsEmptyPage()
    {
        using var context = CreateContext();
        context.AuditEntries.Add(CreateEntry("identity.user.created", "identity"));
        await context.SaveChangesAsync();
        var service = new AuditQueryService(context);

        var result = await service.GetEntriesAsync(
            EmptyFilter() with { Module = "unknown", PageSize = 25 },
            CancellationToken.None);

        AssertSuccess(result);
        Assert.Empty(result.Value!.Items);
        Assert.Equal(0, result.Value.TotalItems);
    }

    [Fact]
    public async Task GetEntriesAsync_WithPageSizeGreaterThan100_ReturnsValidation()
    {
        using var context = CreateContext();
        var service = new AuditQueryService(context);

        var result = await service.GetEntriesAsync(
            EmptyFilter() with { PageSize = 101 },
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Validation, result.Status);
        Assert.Contains(result.Errors, error => error.Code == "Audit.PageSizeInvalid");
    }

    private static AuditEntryFilter EmptyFilter() =>
        new(
            FromUtc: null,
            ToUtc: null,
            ActorUserId: null,
            Module: null,
            Action: null,
            EntityType: null,
            Search: null,
            Page: 1,
            PageSize: 25);

    private static void AssertSuccess<T>(ApplicationResult<T> result)
    {
        Assert.True(
            result.Status == ApplicationResultStatus.Success,
            string.Join(", ", result.Errors.Select(error => $"{error.Code}: {error.Message}")));
    }

    private static AuditEntry CreateEntry(string action, string module) =>
        new(
            Guid.NewGuid(),
            OccurredAtUtc,
            actorUserId: null,
            actorDisplayName: null,
            action,
            module,
            "Entity",
            entityId: null,
            $"Evento {action}.",
            metadataJson: null);

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
