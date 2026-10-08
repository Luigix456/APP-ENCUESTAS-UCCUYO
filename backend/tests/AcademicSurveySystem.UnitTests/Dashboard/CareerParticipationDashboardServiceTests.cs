using AcademicSurveySystem.Application.Dashboard;
using AcademicSurveySystem.Application.Surveys.Results;
using AcademicSurveySystem.Domain.Academic.Entities;
using AcademicSurveySystem.Domain.Academic.Enums;
using AcademicSurveySystem.Domain.Surveys.Entities;
using AcademicSurveySystem.Domain.Surveys.Enums;
using AcademicSurveySystem.Infrastructure.Dashboard;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AcademicSurveySystem.UnitTests.Dashboard;

public sealed class CareerParticipationDashboardServiceTests
{
    [Fact]
    public async Task GetCareerParticipationAsync_ReturnsAggregatedParticipationMetrics()
    {
        using var context = CreateContext();
        var fixture = SeedDashboardFixture(context);
        var service = CreateService(context);

        var result = await service.GetCareerParticipationAsync(
            fixture.CareerId,
            fixture.AcademicCycleId,
            CancellationToken.None);

        Assert.True(result.Succeeded);
        var dashboard = result.Value!;

        Assert.Equal("2026 - Anual", dashboard.AcademicCycleLabel);
        Assert.Equal(4, dashboard.TotalSubjects);
        Assert.Equal(2, dashboard.SubjectsWithResponses);
        Assert.Equal(4, dashboard.StudentSurveyAssignmentsCount);
        Assert.Equal(2, dashboard.OpenSessionsCount);
        Assert.Equal(33.30m, dashboard.AverageParticipationPercentage);
        Assert.Equal(2, dashboard.LowParticipationAssignmentsCount);
        Assert.Equal(50m, dashboard.LowParticipationThresholdPercentage);

        var zero = dashboard.Items.Single(item => item.SurveyAssignmentId == fixture.ZeroPercentAssignmentId);
        Assert.Equal(35, zero.ExpectedRespondentCount);
        Assert.Equal(0, zero.ResponseCount);
        Assert.Equal(35, zero.RemainingCount);
        Assert.Equal(0m, zero.ParticipationPercentage);
        Assert.True(zero.IsLowParticipation);
        Assert.False(zero.HasResponses);
        Assert.False(zero.DetailedResultsAvailable);

        var low = dashboard.Items.Single(item => item.SurveyAssignmentId == fixture.LowPercentAssignmentId);
        Assert.Equal(499, low.ResponseCount);
        Assert.Equal(49.90m, low.ParticipationPercentage);
        Assert.True(low.IsLowParticipation);
        Assert.True(low.DetailedResultsAvailable);

        var exact = dashboard.Items.Single(item => item.SurveyAssignmentId == fixture.ExactThresholdAssignmentId);
        Assert.Equal(1, exact.ResponseCount);
        Assert.Equal(50m, exact.ParticipationPercentage);
        Assert.False(exact.IsLowParticipation);
        Assert.False(exact.DetailedResultsAvailable);

        var unknown = dashboard.Items.Single(item => item.SurveyAssignmentId == fixture.UnknownExpectedAssignmentId);
        Assert.Null(unknown.ExpectedRespondentCount);
        Assert.Null(unknown.RemainingCount);
        Assert.Null(unknown.ParticipationPercentage);
        Assert.False(unknown.IsLowParticipation);

        Assert.DoesNotContain(dashboard.Items, item => item.SurveyAssignmentId == fixture.TeacherAssignmentId);
        Assert.DoesNotContain(dashboard.Items, item => item.SurveyAssignmentId == fixture.InstitutionalAssignmentId);
    }

    [Fact]
    public async Task GetCareerParticipationAsync_CountsSubjectsWithResponsesDistinctly()
    {
        using var context = CreateContext();
        var fixture = SeedDashboardFixture(context);
        var service = CreateService(context);

        var result = await service.GetCareerParticipationAsync(
            fixture.CareerId,
            fixture.AcademicCycleId,
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Value!.SubjectsWithResponses);
    }

    [Fact]
    public async Task GetCareerParticipationAsync_ExcludesExpectedNullFromAverage()
    {
        using var context = CreateContext();
        var fixture = SeedDashboardFixture(context);
        var service = CreateService(context);

        var result = await service.GetCareerParticipationAsync(
            fixture.CareerId,
            fixture.AcademicCycleId,
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(33.30m, result.Value!.AverageParticipationPercentage);
    }

    [Fact]
    public async Task GetCareerParticipationAsync_WithNoAssignments_ReturnsEmptyDashboard()
    {
        using var context = CreateContext();
        var now = new DateTimeOffset(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);
        var unit = new AcademicUnit(Guid.NewGuid(), "UCC", "Universidad", now);
        var career = new Career(Guid.NewGuid(), unit.Id, "TUDS", "Tecnicatura", CareerType.Undergraduate, now);
        var subject = new Subject(Guid.NewGuid(), career.Id, "PRG1", "Programacion I", 1, SubjectPeriod.Annual, now);
        var cycle = new AcademicCycle(
            Guid.NewGuid(),
            2026,
            AcademicCyclePeriod.Annual,
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 12, 20),
            now);
        context.AddRange(unit, career, subject, cycle);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.GetCareerParticipationAsync(
            career.Id,
            cycle.Id,
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Value!.TotalSubjects);
        Assert.Equal(0, result.Value.SubjectsWithResponses);
        Assert.Equal(0, result.Value.StudentSurveyAssignmentsCount);
        Assert.Equal(0, result.Value.OpenSessionsCount);
        Assert.Null(result.Value.AverageParticipationPercentage);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public void DashboardDto_DoesNotExposeResponseContent()
    {
        var propertyNames = typeof(CareerParticipationDashboardItemDto)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.DoesNotContain("Answers", propertyNames);
        Assert.DoesNotContain("Comments", propertyNames);
        Assert.DoesNotContain("OtherText", propertyNames);
        Assert.DoesNotContain("TextValues", propertyNames);
        Assert.DoesNotContain("RatingValues", propertyNames);
    }

    private static CareerParticipationDashboardService CreateService(ApplicationDbContext context)
    {
        return new CareerParticipationDashboardService(
            context,
            Options.Create(new DashboardOptions { LowParticipationThresholdPercentage = 50m }),
            Options.Create(new ResultsPrivacyOptions { MinimumResponsesForDetailedResults = 5 }));
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static DashboardFixture SeedDashboardFixture(ApplicationDbContext context)
    {
        var now = new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);
        var userId = Guid.NewGuid();
        var unit = new AcademicUnit(Guid.NewGuid(), "UCC", "Universidad", now);
        var career = new Career(Guid.NewGuid(), unit.Id, "ADM", "Administracion", CareerType.Undergraduate, now);
        var cycle = new AcademicCycle(
            Guid.NewGuid(),
            2026,
            AcademicCyclePeriod.Annual,
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 12, 20),
            now);
        var subjectShared = new Subject(Guid.NewGuid(), career.Id, "MAT1", "Matematica I", 1, SubjectPeriod.Annual, now);
        var subjectExact = new Subject(Guid.NewGuid(), career.Id, "ADM1", "Administracion I", 1, SubjectPeriod.Annual, now);
        var subjectUnknown = new Subject(Guid.NewGuid(), career.Id, "ECO1", "Economia I", 1, SubjectPeriod.Annual, now);
        var subjectWithoutResponses = new Subject(Guid.NewGuid(), career.Id, "DER1", "Derecho I", 1, SubjectPeriod.Annual, now);
        var inactiveSubject = new Subject(Guid.NewGuid(), career.Id, "OLD", "Materia antigua", 1, SubjectPeriod.Annual, now);
        inactiveSubject.Deactivate(now.AddMinutes(1));

        var teacherA = new Teacher(Guid.NewGuid(), "Ada", "Lovelace", "ada@example.com", now);
        var teacherB = new Teacher(Guid.NewGuid(), "Grace", "Hopper", "grace@example.com", now);
        var teacherC = new Teacher(Guid.NewGuid(), "Carlos", "Gardel", "carlos@example.com", now);
        var teacherD = new Teacher(Guid.NewGuid(), "Juana", "Manso", "juana@example.com", now);

        var sharedAssignmentA = new TeacherSubjectAssignment(Guid.NewGuid(), teacherA.Id, subjectShared.Id, cycle.Id, "Titular", now);
        var sharedAssignmentB = new TeacherSubjectAssignment(Guid.NewGuid(), teacherB.Id, subjectShared.Id, cycle.Id, "Adjunto", now);
        var exactTeacherAssignment = new TeacherSubjectAssignment(Guid.NewGuid(), teacherC.Id, subjectExact.Id, cycle.Id, "Titular", now);
        var unknownTeacherAssignment = new TeacherSubjectAssignment(Guid.NewGuid(), teacherD.Id, subjectUnknown.Id, cycle.Id, "Titular", now);
        var institutionalTeacherAssignment = new TeacherSubjectAssignment(Guid.NewGuid(), teacherA.Id, subjectWithoutResponses.Id, cycle.Id, "Titular", now);

        var zeroSurvey = new Survey(Guid.NewGuid(), userId, "Encuesta 0", null, SurveyTarget.Student, now);
        var lowSurvey = new Survey(Guid.NewGuid(), userId, "Encuesta baja", null, SurveyTarget.Student, now);
        var exactSurvey = new Survey(Guid.NewGuid(), userId, "Encuesta exacta", null, SurveyTarget.Student, now);
        var unknownSurvey = new Survey(Guid.NewGuid(), userId, "Encuesta historica", null, SurveyTarget.Student, now);
        var teacherSurvey = new Survey(Guid.NewGuid(), userId, "Encuesta docente", null, SurveyTarget.Teacher, now);
        var institutionalSurvey = new Survey(Guid.NewGuid(), userId, "Encuesta institucional", null, SurveyTarget.Institutional, now);

        var zeroAssignment = new SurveyAssignment(
            Guid.NewGuid(),
            zeroSurvey.Id,
            career.Id,
            subjectShared.Id,
            cycle.Id,
            sharedAssignmentA.Id,
            35,
            now);
        var lowAssignment = new SurveyAssignment(
            Guid.NewGuid(),
            lowSurvey.Id,
            career.Id,
            subjectShared.Id,
            cycle.Id,
            sharedAssignmentB.Id,
            1000,
            now);
        var exactAssignment = new SurveyAssignment(
            Guid.NewGuid(),
            exactSurvey.Id,
            career.Id,
            subjectExact.Id,
            cycle.Id,
            exactTeacherAssignment.Id,
            2,
            now);
        var unknownAssignment = new SurveyAssignment(
            Guid.NewGuid(),
            unknownSurvey.Id,
            career.Id,
            subjectUnknown.Id,
            cycle.Id,
            unknownTeacherAssignment.Id,
            now);
        var teacherAssignment = new SurveyAssignment(
            Guid.NewGuid(),
            teacherSurvey.Id,
            career.Id,
            subjectExact.Id,
            cycle.Id,
            exactTeacherAssignment.Id,
            2,
            now);
        var institutionalAssignment = new SurveyAssignment(
            Guid.NewGuid(),
            institutionalSurvey.Id,
            career.Id,
            subjectWithoutResponses.Id,
            cycle.Id,
            institutionalTeacherAssignment.Id,
            10,
            now);

        var zeroSession = CreateSession(zeroAssignment.Id, userId, "ZERO", now);
        zeroSession.Open(now.AddMinutes(1));
        var lowSession = CreateSession(lowAssignment.Id, userId, "LOW", now);
        lowSession.Open(now.AddMinutes(1));
        var exactSession = CreateSession(exactAssignment.Id, userId, "EXACT", now);

        var lowResponses = Enumerable.Range(0, 499)
            .Select(index => new SurveyResponse(Guid.NewGuid(), lowSession.Id, lowSurvey.Id, now.AddMinutes(10 + index)))
            .ToArray();

        context.AddRange(
            unit,
            career,
            cycle,
            subjectShared,
            subjectExact,
            subjectUnknown,
            subjectWithoutResponses,
            inactiveSubject,
            teacherA,
            teacherB,
            teacherC,
            teacherD,
            sharedAssignmentA,
            sharedAssignmentB,
            exactTeacherAssignment,
            unknownTeacherAssignment,
            institutionalTeacherAssignment,
            zeroSurvey,
            lowSurvey,
            exactSurvey,
            unknownSurvey,
            teacherSurvey,
            institutionalSurvey,
            zeroAssignment,
            lowAssignment,
            exactAssignment,
            unknownAssignment,
            teacherAssignment,
            institutionalAssignment,
            zeroSession,
            lowSession,
            exactSession,
            new SurveyResponse(Guid.NewGuid(), exactSession.Id, exactSurvey.Id, now.AddHours(1)));
        context.AddRange(lowResponses);
        context.SaveChanges();

        return new DashboardFixture(
            career.Id,
            cycle.Id,
            zeroAssignment.Id,
            lowAssignment.Id,
            exactAssignment.Id,
            unknownAssignment.Id,
            teacherAssignment.Id,
            institutionalAssignment.Id);
    }

    private static SurveySession CreateSession(
        Guid surveyAssignmentId,
        Guid userId,
        string accessCode,
        DateTimeOffset now)
    {
        return new SurveySession(
            Guid.NewGuid(),
            surveyAssignmentId,
            userId,
            accessCode,
            null,
            null,
            now.AddDays(7),
            now);
    }

    private sealed record DashboardFixture(
        Guid CareerId,
        Guid AcademicCycleId,
        Guid ZeroPercentAssignmentId,
        Guid LowPercentAssignmentId,
        Guid ExactThresholdAssignmentId,
        Guid UnknownExpectedAssignmentId,
        Guid TeacherAssignmentId,
        Guid InstitutionalAssignmentId);
}
