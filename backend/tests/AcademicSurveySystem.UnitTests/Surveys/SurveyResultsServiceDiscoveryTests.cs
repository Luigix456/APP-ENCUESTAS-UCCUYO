using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Surveys.Results;
using AcademicSurveySystem.Domain.Academic.Entities;
using AcademicSurveySystem.Domain.Academic.Enums;
using AcademicSurveySystem.Domain.Identity.Entities;
using AcademicSurveySystem.Domain.Surveys.Entities;
using AcademicSurveySystem.Domain.Surveys.Enums;
using AcademicSurveySystem.Infrastructure.Persistence;
using AcademicSurveySystem.Infrastructure.Surveys.Results;
using Microsoft.EntityFrameworkCore;

namespace AcademicSurveySystem.UnitTests.Surveys;

public sealed class SurveyResultsServiceDiscoveryTests
{
    [Fact]
    public async Task GetSurveyAssignmentResultsAsync_WithReadAllScope_ReturnsAssignmentsFromMultipleCareers()
    {
        using var context = CreateContext();
        var fixture = SeedResultsFixture(context);
        var service = new SurveyResultsService(context);

        var result = await service.GetSurveyAssignmentResultsAsync(
            ResultsAccessScope.All(),
            EmptyFilter(),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.Contains(result.Value!, item => item.SurveyAssignmentId == fixture.AssignmentAId);
        Assert.Contains(result.Value!, item => item.SurveyAssignmentId == fixture.AssignmentBId);
        Assert.Contains(result.Value!, item => item.SurveyAssignmentId == fixture.AssignmentCId);
    }

    [Fact]
    public async Task GetSurveyAssignmentResultsAsync_WithCareerScope_ReturnsOnlyAllowedCareer()
    {
        using var context = CreateContext();
        var fixture = SeedResultsFixture(context);
        var service = new SurveyResultsService(context);

        var result = await service.GetSurveyAssignmentResultsAsync(
            ResultsAccessScope.Career([fixture.CareerAId]),
            EmptyFilter(),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.Contains(result.Value!, item => item.SurveyAssignmentId == fixture.AssignmentAId);
        Assert.Contains(result.Value!, item => item.SurveyAssignmentId == fixture.AssignmentCId);
        Assert.DoesNotContain(result.Value!, item => item.SurveyAssignmentId == fixture.AssignmentBId);
    }

    [Fact]
    public async Task GetSurveyAssignmentResultsAsync_WithMultipleCareerScope_ReturnsOnlyThoseCareers()
    {
        using var context = CreateContext();
        var fixture = SeedResultsFixture(context);
        var service = new SurveyResultsService(context);

        var result = await service.GetSurveyAssignmentResultsAsync(
            ResultsAccessScope.Career([fixture.CareerAId, fixture.CareerBId]),
            EmptyFilter(),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.Contains(result.Value!, item => item.SurveyAssignmentId == fixture.AssignmentAId);
        Assert.Contains(result.Value!, item => item.SurveyAssignmentId == fixture.AssignmentBId);
        Assert.Contains(result.Value!, item => item.SurveyAssignmentId == fixture.AssignmentCId);
    }

    [Fact]
    public async Task GetSurveyAssignmentResultsAsync_WithReadCareerAndNoUserCareer_ReturnsEmptyList()
    {
        using var context = CreateContext();
        SeedResultsFixture(context);
        var service = new SurveyResultsService(context);

        var result = await service.GetSurveyAssignmentResultsAsync(
            ResultsAccessScope.Career([]),
            EmptyFilter(),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task GetSurveyAssignmentResultsAsync_FilterDoesNotExpandCareerScope()
    {
        using var context = CreateContext();
        var fixture = SeedResultsFixture(context);
        var service = new SurveyResultsService(context);

        var result = await service.GetSurveyAssignmentResultsAsync(
            ResultsAccessScope.Career([fixture.CareerAId]),
            EmptyFilter() with { CareerId = fixture.CareerBId },
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task GetSurveyAssignmentResultsAsync_SupportsAllFiltersInsideScope()
    {
        using var context = CreateContext();
        var fixture = SeedResultsFixture(context);
        var service = new SurveyResultsService(context);

        var result = await service.GetSurveyAssignmentResultsAsync(
            ResultsAccessScope.All(),
            new SurveyAssignmentResultsFilter(
                fixture.SurveyAId,
                fixture.CareerAId,
                fixture.SubjectAId,
                fixture.AcademicCycleId,
                fixture.TeacherAId),
            CancellationToken.None);

        var item = Assert.Single(result.Value!);
        Assert.Equal(fixture.AssignmentAId, item.SurveyAssignmentId);
    }

    [Fact]
    public async Task GetSurveyAssignmentResultsAsync_InactiveAssignmentWithHistoryStillAppears()
    {
        using var context = CreateContext();
        var fixture = SeedResultsFixture(context);
        var service = new SurveyResultsService(context);

        var result = await service.GetSurveyAssignmentResultsAsync(
            ResultsAccessScope.Career([fixture.CareerAId]),
            EmptyFilter(),
            CancellationToken.None);

        var item = result.Value!.Single(item => item.SurveyAssignmentId == fixture.AssignmentAId);
        Assert.False(item.IsActive);
        Assert.Equal(2, item.TotalSessions);
        Assert.Equal(3, item.TotalResponses);
        Assert.Equal(fixture.FirstSubmittedAtUtc, item.FirstSubmittedAtUtc);
        Assert.Equal(fixture.LastSubmittedAtUtc, item.LastSubmittedAtUtc);
    }

    [Fact]
    public async Task GetSurveyAssignmentResultsAsync_AssignmentWithoutResponsesHasZeroTotalsAndNullDates()
    {
        using var context = CreateContext();
        var fixture = SeedResultsFixture(context);
        var service = new SurveyResultsService(context);

        var result = await service.GetSurveyAssignmentResultsAsync(
            ResultsAccessScope.Career([fixture.CareerAId]),
            EmptyFilter(),
            CancellationToken.None);

        var item = result.Value!.Single(item => item.SurveyAssignmentId == fixture.AssignmentCId);
        Assert.Equal(0, item.TotalSessions);
        Assert.Equal(0, item.TotalResponses);
        Assert.Null(item.FirstSubmittedAtUtc);
        Assert.Null(item.LastSubmittedAtUtc);
    }

    [Fact]
    public async Task GetSurveyAssignmentResultsAsync_TotalsMatchSummary()
    {
        using var context = CreateContext();
        var fixture = SeedResultsFixture(context);
        var service = new SurveyResultsService(context);

        var listResult = await service.GetSurveyAssignmentResultsAsync(
            ResultsAccessScope.All(),
            EmptyFilter() with { CareerId = fixture.CareerAId },
            CancellationToken.None);
        var summaryResult = await service.GetSurveyAssignmentSummaryAsync(
            fixture.AssignmentAId,
            CancellationToken.None);

        var item = listResult.Value!.Single(item => item.SurveyAssignmentId == fixture.AssignmentAId);
        Assert.Equal(summaryResult.Value!.TotalSessions, item.TotalSessions);
        Assert.Equal(summaryResult.Value.TotalResponses, item.TotalResponses);
        Assert.Equal(summaryResult.Value.FirstSubmittedAtUtc, item.FirstSubmittedAtUtc);
        Assert.Equal(summaryResult.Value.LastSubmittedAtUtc, item.LastSubmittedAtUtc);
    }

    [Fact]
    public async Task ResultsAccessService_WithoutResultsPermission_ReturnsForbiddenScope()
    {
        using var context = CreateContext();
        var fixture = SeedResultsFixture(context);
        var service = new ResultsAccessService(context);

        var scope = await service.GetDiscoveryScopeAsync(
            fixture.UserId,
            hasReadAll: false,
            hasReadCareer: false,
            CancellationToken.None);

        Assert.Equal(ResultsAccessScopeType.Forbidden, scope.Type);
    }

    [Fact]
    public async Task ResultsAccessService_WithReadCareer_ReturnsUserCareerScope()
    {
        using var context = CreateContext();
        var fixture = SeedResultsFixture(context);
        var service = new ResultsAccessService(context);

        var scope = await service.GetDiscoveryScopeAsync(
            fixture.UserId,
            hasReadAll: false,
            hasReadCareer: true,
            CancellationToken.None);

        Assert.Equal(ResultsAccessScopeType.Career, scope.Type);
        Assert.Equal([fixture.CareerAId], scope.CareerIds);
    }

    private static SurveyAssignmentResultsFilter EmptyFilter() =>
        new(null, null, null, null, null);

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static ResultsFixture SeedResultsFixture(ApplicationDbContext context)
    {
        var now = new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
        var userId = Guid.NewGuid();
        var academicUnit = new AcademicUnit(Guid.NewGuid(), "academic-unit", "Academic Unit", now);
        var careerA = new Career(Guid.NewGuid(), academicUnit.Id, "career-a", "Career A", CareerType.Undergraduate, now);
        var careerB = new Career(Guid.NewGuid(), academicUnit.Id, "career-b", "Career B", CareerType.Undergraduate, now);
        var careerC = new Career(Guid.NewGuid(), academicUnit.Id, "career-c", "Career C", CareerType.Undergraduate, now);
        var subjectA = new Subject(Guid.NewGuid(), careerA.Id, "subject-a", "Subject A", 1, SubjectPeriod.Annual, now);
        var subjectB = new Subject(Guid.NewGuid(), careerB.Id, "subject-b", "Subject B", 1, SubjectPeriod.Annual, now);
        var subjectC = new Subject(Guid.NewGuid(), careerA.Id, "subject-c", "Subject C", 1, SubjectPeriod.Annual, now);
        var cycle = new AcademicCycle(
            Guid.NewGuid(),
            2026,
            AcademicCyclePeriod.Annual,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            now);
        var teacherA = new Teacher(Guid.NewGuid(), "Ada", "Lovelace", "ada@example.com", now);
        var teacherB = new Teacher(Guid.NewGuid(), "Grace", "Hopper", "grace@example.com", now);
        var teacherAssignmentA = new TeacherSubjectAssignment(
            Guid.NewGuid(),
            teacherA.Id,
            subjectA.Id,
            cycle.Id,
            "Titular",
            now);
        var teacherAssignmentB = new TeacherSubjectAssignment(
            Guid.NewGuid(),
            teacherB.Id,
            subjectB.Id,
            cycle.Id,
            "Adjunto",
            now);
        var teacherAssignmentC = new TeacherSubjectAssignment(
            Guid.NewGuid(),
            teacherA.Id,
            subjectC.Id,
            cycle.Id,
            "Titular",
            now);
        var surveyA = new Survey(Guid.NewGuid(), userId, "Survey A", null, SurveyTarget.Student, now);
        var surveyB = new Survey(Guid.NewGuid(), userId, "Survey B", null, SurveyTarget.Student, now);
        var surveyC = new Survey(Guid.NewGuid(), userId, "Survey C", null, SurveyTarget.Student, now);
        var assignmentA = new SurveyAssignment(
            Guid.NewGuid(),
            surveyA.Id,
            careerA.Id,
            subjectA.Id,
            cycle.Id,
            teacherAssignmentA.Id,
            now);
        var assignmentB = new SurveyAssignment(
            Guid.NewGuid(),
            surveyB.Id,
            careerB.Id,
            subjectB.Id,
            cycle.Id,
            teacherAssignmentB.Id,
            now);
        var assignmentC = new SurveyAssignment(
            Guid.NewGuid(),
            surveyC.Id,
            careerA.Id,
            subjectC.Id,
            cycle.Id,
            teacherAssignmentC.Id,
            now);

        assignmentA.Deactivate(now.AddMinutes(1));

        var sessionA1 = new SurveySession(
            Guid.NewGuid(),
            assignmentA.Id,
            userId,
            "A1",
            null,
            null,
            now.AddDays(7),
            now);
        var sessionA2 = new SurveySession(
            Guid.NewGuid(),
            assignmentA.Id,
            userId,
            "A2",
            null,
            null,
            now.AddDays(7),
            now);
        var sessionB = new SurveySession(
            Guid.NewGuid(),
            assignmentB.Id,
            userId,
            "B1",
            null,
            null,
            now.AddDays(7),
            now);
        var firstSubmittedAtUtc = now.AddHours(1);
        var lastSubmittedAtUtc = now.AddHours(3);

        context.AddRange(
            academicUnit,
            careerA,
            careerB,
            careerC,
            subjectA,
            subjectB,
            subjectC,
            cycle,
            teacherA,
            teacherB,
            teacherAssignmentA,
            teacherAssignmentB,
            teacherAssignmentC,
            surveyA,
            surveyB,
            surveyC,
            assignmentA,
            assignmentB,
            assignmentC,
            sessionA1,
            sessionA2,
            sessionB,
            new SurveyResponse(Guid.NewGuid(), sessionA1.Id, surveyA.Id, firstSubmittedAtUtc),
            new SurveyResponse(Guid.NewGuid(), sessionA1.Id, surveyA.Id, now.AddHours(2)),
            new SurveyResponse(Guid.NewGuid(), sessionA2.Id, surveyA.Id, lastSubmittedAtUtc),
            new SurveyResponse(Guid.NewGuid(), sessionB.Id, surveyB.Id, now.AddHours(4)),
            new UserCareer(userId, careerA.Id, now));
        context.SaveChanges();

        return new ResultsFixture(
            userId,
            careerA.Id,
            careerB.Id,
            surveyA.Id,
            subjectA.Id,
            cycle.Id,
            teacherA.Id,
            assignmentA.Id,
            assignmentB.Id,
            assignmentC.Id,
            firstSubmittedAtUtc,
            lastSubmittedAtUtc);
    }

    private sealed record ResultsFixture(
        Guid UserId,
        Guid CareerAId,
        Guid CareerBId,
        Guid SurveyAId,
        Guid SubjectAId,
        Guid AcademicCycleId,
        Guid TeacherAId,
        Guid AssignmentAId,
        Guid AssignmentBId,
        Guid AssignmentCId,
        DateTimeOffset FirstSubmittedAtUtc,
        DateTimeOffset LastSubmittedAtUtc);
}
