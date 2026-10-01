using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Identity.UserCareers;
using AcademicSurveySystem.Domain.Academic.Entities;
using AcademicSurveySystem.Domain.Academic.Enums;
using AcademicSurveySystem.Domain.Identity.Entities;
using AcademicSurveySystem.Infrastructure.Identity;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AcademicSurveySystem.UnitTests.Identity;

public sealed class UserCareerServiceTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ReplaceUserCareersAsync_WithEmptyCareerIds_RemovesAllAssociations()
    {
        using var context = CreateContext();
        var user = CreateUser();
        var career = CreateCareer("tuds", "TUDS");
        context.AddRange(user, career, new UserCareer(user.Id, career.Id, CreatedAtUtc));
        await context.SaveChangesAsync();
        var service = new UserCareerService(context);

        var result = await service.ReplaceUserCareersAsync(
            user.Id,
            new UpdateUserCareersRequest([]),
            CancellationToken.None);
        var getResult = await service.GetUserCareersAsync(user.Id, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Value!);
        Assert.True(getResult.Succeeded);
        Assert.Empty(getResult.Value!);
        Assert.Empty(context.UserCareers);
    }

    [Fact]
    public async Task ReplaceUserCareersAsync_ReplacesByDiffAndPreservesExistingAssignedAt()
    {
        using var context = CreateContext();
        var user = CreateUser();
        var careerA = CreateCareer("a", "A");
        var careerB = CreateCareer("b", "B");
        var careerC = CreateCareer("c", "C");
        var assignedA = CreatedAtUtc.AddDays(-3);
        var assignedB = CreatedAtUtc.AddDays(-2);
        context.AddRange(
            user,
            careerA,
            careerB,
            careerC,
            new UserCareer(user.Id, careerA.Id, assignedA),
            new UserCareer(user.Id, careerB.Id, assignedB));
        await context.SaveChangesAsync();
        var service = new UserCareerService(context);

        var result = await service.ReplaceUserCareersAsync(
            user.Id,
            new UpdateUserCareersRequest([careerB.Id, careerC.Id]),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(
            new[] { careerB.Id, careerC.Id }.Order().ToArray(),
            result.Value!.Select(item => item.CareerId).Order().ToArray());

        var persisted = context.UserCareers
            .AsNoTracking()
            .Where(item => item.UserId == user.Id)
            .ToArray();

        Assert.DoesNotContain(persisted, item => item.CareerId == careerA.Id);
        Assert.Contains(persisted, item => item.CareerId == careerB.Id && item.AssignedAtUtc == assignedB);
        Assert.Contains(persisted, item => item.CareerId == careerC.Id && item.AssignedAtUtc > assignedB);
    }

    [Fact]
    public async Task ReplaceUserCareersAsync_WithSameSet_DoesNotDuplicateOrChangeAssignedAt()
    {
        using var context = CreateContext();
        var user = CreateUser();
        var careerA = CreateCareer("a", "A");
        var careerB = CreateCareer("b", "B");
        var assignedA = CreatedAtUtc.AddDays(-3);
        var assignedB = CreatedAtUtc.AddDays(-2);
        context.AddRange(
            user,
            careerA,
            careerB,
            new UserCareer(user.Id, careerA.Id, assignedA),
            new UserCareer(user.Id, careerB.Id, assignedB));
        await context.SaveChangesAsync();
        var service = new UserCareerService(context);

        var result = await service.ReplaceUserCareersAsync(
            user.Id,
            new UpdateUserCareersRequest([careerB.Id, careerA.Id]),
            CancellationToken.None);

        Assert.True(result.Succeeded);

        var persisted = context.UserCareers
            .AsNoTracking()
            .Where(item => item.UserId == user.Id)
            .ToArray();

        Assert.Equal(2, persisted.Length);
        Assert.Contains(persisted, item => item.CareerId == careerA.Id && item.AssignedAtUtc == assignedA);
        Assert.Contains(persisted, item => item.CareerId == careerB.Id && item.AssignedAtUtc == assignedB);
    }

    [Fact]
    public async Task ReplaceUserCareersAsync_WithDuplicateCareerIds_DeduplicatesConsistently()
    {
        using var context = CreateContext();
        var user = CreateUser();
        var career = CreateCareer("tuds", "TUDS");
        context.AddRange(user, career);
        await context.SaveChangesAsync();
        var service = new UserCareerService(context);

        var result = await service.ReplaceUserCareersAsync(
            user.Id,
            new UpdateUserCareersRequest([career.Id, career.Id]),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Single(result.Value!);
        Assert.Single(context.UserCareers.Where(item => item.UserId == user.Id));
    }

    [Fact]
    public async Task ReplaceUserCareersAsync_WithMissingCareer_ReturnsNotFoundAndKeepsOriginalAssociations()
    {
        using var context = CreateContext();
        var user = CreateUser();
        var career = CreateCareer("tuds", "TUDS");
        var assignedAt = CreatedAtUtc.AddDays(-1);
        context.AddRange(user, career, new UserCareer(user.Id, career.Id, assignedAt));
        await context.SaveChangesAsync();
        var service = new UserCareerService(context);

        var result = await service.ReplaceUserCareersAsync(
            user.Id,
            new UpdateUserCareersRequest([Guid.NewGuid()]),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.NotFound, result.Status);

        var persisted = Assert.Single(context.UserCareers.Where(item => item.UserId == user.Id));
        Assert.Equal(career.Id, persisted.CareerId);
        Assert.Equal(assignedAt, persisted.AssignedAtUtc);
    }

    [Fact]
    public async Task ReplaceUserCareersAsync_WithInactiveCareer_ReturnsValidationAndKeepsOriginalAssociations()
    {
        using var context = CreateContext();
        var user = CreateUser();
        var activeCareer = CreateCareer("active", "Active");
        var inactiveCareer = CreateCareer("inactive", "Inactive");
        inactiveCareer.Deactivate(CreatedAtUtc.AddMinutes(1));
        var assignedAt = CreatedAtUtc.AddDays(-1);
        context.AddRange(user, activeCareer, inactiveCareer, new UserCareer(user.Id, activeCareer.Id, assignedAt));
        await context.SaveChangesAsync();
        var service = new UserCareerService(context);

        var result = await service.ReplaceUserCareersAsync(
            user.Id,
            new UpdateUserCareersRequest([inactiveCareer.Id]),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Validation, result.Status);

        var persisted = Assert.Single(context.UserCareers.Where(item => item.UserId == user.Id));
        Assert.Equal(activeCareer.Id, persisted.CareerId);
        Assert.Equal(assignedAt, persisted.AssignedAtUtc);
    }

    [Fact]
    public async Task ReplaceUserCareersAsync_WithMissingUser_ReturnsNotFound()
    {
        using var context = CreateContext();
        var service = new UserCareerService(context);

        var result = await service.ReplaceUserCareersAsync(
            Guid.NewGuid(),
            new UpdateUserCareersRequest([]),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.NotFound, result.Status);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }

    private static User CreateUser()
    {
        return new User(
            Guid.NewGuid(),
            "Test",
            "User",
            "test.user@example.com",
            "HASH",
            CreatedAtUtc);
    }

    private static Career CreateCareer(string code, string name)
    {
        return new Career(
            Guid.NewGuid(),
            code,
            name,
            CareerType.Undergraduate,
            CreatedAtUtc);
    }
}
