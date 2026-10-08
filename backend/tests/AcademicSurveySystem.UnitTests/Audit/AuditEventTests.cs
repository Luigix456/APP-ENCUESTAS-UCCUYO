using AcademicSurveySystem.Application.Audit;
using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Common.Security;
using AcademicSurveySystem.Application.Identity.UserManagement;
using AcademicSurveySystem.Application.Identity.UserPasswordReset;
using AcademicSurveySystem.Application.Surveys.Assignments;
using AcademicSurveySystem.Application.Surveys.Requests;
using AcademicSurveySystem.Application.Surveys.Responses;
using AcademicSurveySystem.Application.Surveys.Sessions;
using AcademicSurveySystem.Domain.Academic.Entities;
using AcademicSurveySystem.Domain.Academic.Enums;
using AcademicSurveySystem.Domain.Identity;
using AcademicSurveySystem.Domain.Surveys.Entities;
using AcademicSurveySystem.Domain.Surveys.Enums;
using AcademicSurveySystem.Infrastructure.Academic;
using AcademicSurveySystem.Infrastructure.Audit;
using AcademicSurveySystem.Infrastructure.Identity;
using AcademicSurveySystem.Infrastructure.Persistence;
using AcademicSurveySystem.Infrastructure.Surveys;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace AcademicSurveySystem.UnitTests.Audit;

public sealed class AuditEventTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task IdentityCommands_WriteSafeAuditEntries()
    {
        await using var context = CreateContext();
        var service = new UserManagementService(context, new FakePasswordHasher(), CreateAuditWriter(context));

        var created = await service.CreateUserAsync(
            new CreateUserRequest(
                "Maria",
                "Gomez",
                "maria.gomez@institucion.edu.ar",
                "ValidPassword1!",
                [IdentityCatalog.GetRole("career_director").Id]),
            CancellationToken.None);
        await service.ReplaceUserRolesAsync(
            created.Value!.Id,
            new UpdateUserRolesRequest([IdentityCatalog.GetRole("dean").Id]),
            CancellationToken.None);
        var passwordReset = new UserPasswordResetService(
            context,
            new FakePasswordHasher(),
            NullLogger<UserPasswordResetService>.Instance,
            CreateAuditWriter(context));
        await passwordReset.ResetByUserIdAsync(
            created.Value.Id,
            "AnotherValidPassword1!",
            CancellationToken.None);

        var entries = await context.AuditEntries
            .OrderBy(entry => entry.OccurredAtUtc)
            .ToArrayAsync();

        Assert.Contains(entries, entry => entry.Action == "identity.user.created");
        Assert.Contains(entries, entry => entry.Action == "identity.user.roles_updated");
        var resetEntry = Assert.Single(entries, entry => entry.Action == "identity.user.password_reset");
        Assert.Null(resetEntry.MetadataJson);
        Assert.DoesNotContain("Password", string.Join(' ', entries.Select(entry => entry.MetadataJson)));
        Assert.DoesNotContain("HASHED_PASSWORD", string.Join(' ', entries.Select(entry => entry.MetadataJson)));
    }

    [Fact]
    public async Task AcademicCommands_WriteSubjectAndEnrollmentAuditEntries()
    {
        await using var context = CreateContext();
        var academic = SeedAcademicContext(context);
        var service = new AcademicCatalogService(context, CreateAuditWriter(context));

        var subject = await service.CreateSubjectAsync(
            new Application.Academic.Subjects.CreateSubjectRequest(
                academic.Career.Id,
                "matematica",
                "Matematica",
                1,
                "Annual"),
            CancellationToken.None);
        await service.SetSubjectEnrollmentAsync(
            subject.Value!.Id,
            academic.Cycle.Id,
            new Application.Academic.SubjectEnrollments.SetSubjectEnrollmentRequest(25),
            CancellationToken.None);
        await service.SetSubjectEnrollmentAsync(
            subject.Value.Id,
            academic.Cycle.Id,
            new Application.Academic.SubjectEnrollments.SetSubjectEnrollmentRequest(32),
            CancellationToken.None);

        var entries = await context.AuditEntries.ToArrayAsync();

        Assert.Contains(entries, entry => entry.Action == "academic.subject.created");
        Assert.Contains(entries, entry => entry.Action == "academic.enrollment.created");
        var updated = Assert.Single(entries, entry => entry.Action == "academic.enrollment.updated");
        Assert.Contains("\"oldCount\":25", updated.MetadataJson);
        Assert.Contains("\"newCount\":32", updated.MetadataJson);
    }

    [Fact]
    public async Task SurveyTemplateCommands_WritePublishAndVersionAuditEntries()
    {
        await using var context = CreateContext();
        var survey = SeedDraftSurvey(context);
        var service = new SurveyTemplateService(context, CreateAuditWriter(context));

        var publish = await service.PublishSurveyAsync(survey.Id, CancellationToken.None);
        var version = await service.GetOrCreateEditableVersionAsync(
            survey.Id,
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(publish.Succeeded);
        Assert.True(version.Succeeded);
        var entries = await context.AuditEntries.ToArrayAsync();
        Assert.Contains(entries, entry => entry.Action == "surveys.survey.published");
        Assert.Contains(entries, entry => entry.Action == "surveys.survey.version_created");
    }

    [Fact]
    public async Task AssignmentAndSessionCommands_WriteAuditEntriesWithoutAccessCode()
    {
        await using var context = CreateContext();
        var fixture = SeedAssignmentFixture(context);
        var assignmentService = new SurveyAssignmentService(context, CreateAuditWriter(context));
        var assignment = await assignmentService.CreateAssignmentAsync(
            new CreateSurveyAssignmentRequest(
                fixture.Survey.Id,
                fixture.Career.Id,
                fixture.Subject.Id,
                fixture.Cycle.Id,
                fixture.TeacherAssignment.Id),
            CancellationToken.None);
        var sessionService = new SurveySessionService(
            context,
            new FixedAccessCodeGenerator("PUBLIC-CODE"),
            CreateAuditWriter(context));
        var session = await sessionService.CreateSessionAsync(
            new CreateSurveySessionRequest(
                assignment.Value!.Id,
                "Clase",
                "Aula 1",
                DateTimeOffset.UtcNow.AddHours(2)),
            Guid.NewGuid(),
            CancellationToken.None);
        await sessionService.OpenSessionAsync(session.Value!.Id, CancellationToken.None);
        await sessionService.CloseSessionAsync(session.Value.Id, CancellationToken.None);

        var entries = await context.AuditEntries.ToArrayAsync();

        Assert.Contains(entries, entry => entry.Action == "survey_assignments.assignment.created");
        Assert.Contains(entries, entry => entry.Action == "survey_sessions.session.created");
        Assert.Contains(entries, entry => entry.Action == "survey_sessions.session.opened");
        Assert.Contains(entries, entry => entry.Action == "survey_sessions.session.closed");
        Assert.DoesNotContain("PUBLIC-CODE", string.Join(' ', entries.Select(entry => entry.MetadataJson)));
    }

    [Fact]
    public async Task PublicSurveyResponse_DoesNotWriteAuditEntry()
    {
        await using var context = CreateContext();
        var fixture = SeedAssignmentFixture(context);
        var assignment = new SurveyAssignment(
            Guid.NewGuid(),
            fixture.Survey.Id,
            fixture.Career.Id,
            fixture.Subject.Id,
            fixture.Cycle.Id,
            fixture.TeacherAssignment.Id,
            CreatedAtUtc);
        var session = new SurveySession(
            Guid.NewGuid(),
            assignment.Id,
            Guid.NewGuid(),
            "PUBLIC-CODE",
            "Clase",
            null,
            DateTimeOffset.UtcNow.AddHours(2),
            DateTimeOffset.UtcNow);
        session.Open(DateTimeOffset.UtcNow);
        context.AddRange(assignment, session);
        await context.SaveChangesAsync();
        var service = new SurveyResponseService(context, new NoOpProgressPublisher());

        var result = await service.SubmitResponseAsync(
            "PUBLIC-CODE",
            new SubmitSurveyResponseRequest([
                new SubmitSurveyAnswerRequest(
                    fixture.Question.Id,
                    OptionIds: null,
                    TextValue: "Respuesta",
                    NumericValue: null,
                    Comment: null,
                    MatrixAnswers: null)
            ]),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.Empty(await context.AuditEntries.ToArrayAsync());
    }

    private static AuditWriter CreateAuditWriter(ApplicationDbContext context)
    {
        var actor = new CurrentAuditActorAccessor();
        actor.SetCurrent(new AuditActor(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Admin Auditor"));
        return new AuditWriter(context, actor);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static AcademicFixture SeedAcademicContext(ApplicationDbContext context)
    {
        var unit = new AcademicUnit(Guid.NewGuid(), "unidad", "Unidad", CreatedAtUtc);
        var career = new Career(Guid.NewGuid(), unit.Id, "carrera", "Carrera", CareerType.Undergraduate, CreatedAtUtc);
        var cycle = new AcademicCycle(
            Guid.NewGuid(),
            2026,
            AcademicCyclePeriod.Annual,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            CreatedAtUtc);
        context.AddRange(unit, career, cycle);
        context.SaveChanges();
        return new AcademicFixture(career, cycle);
    }

    private static AssignmentFixture SeedAssignmentFixture(ApplicationDbContext context)
    {
        var academic = SeedAcademicContext(context);
        var subject = new Subject(
            Guid.NewGuid(),
            academic.Career.Id,
            "materia",
            "Materia",
            1,
            SubjectPeriod.Annual,
            CreatedAtUtc);
        var teacher = new Teacher(Guid.NewGuid(), "Ada", "Lovelace", null, CreatedAtUtc);
        var teacherAssignment = new TeacherSubjectAssignment(
            Guid.NewGuid(),
            teacher.Id,
            subject.Id,
            academic.Cycle.Id,
            "Titular",
            CreatedAtUtc);
        var survey = SeedDraftSurvey(context);
        survey.Publish(CreatedAtUtc.AddMinutes(1));
        context.AddRange(subject, teacher, teacherAssignment);
        context.SaveChanges();
        return new AssignmentFixture(
            academic.Career,
            academic.Cycle,
            subject,
            teacherAssignment,
            survey,
            survey.Sections.Single().Questions.Single());
    }

    private static Survey SeedDraftSurvey(ApplicationDbContext context)
    {
        var survey = new Survey(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Encuesta",
            null,
            SurveyTarget.Teacher,
            CreatedAtUtc);
        var section = new SurveySection(Guid.NewGuid(), survey.Id, "Seccion", null, 1, CreatedAtUtc);
        var question = new SurveyQuestion(
            Guid.NewGuid(),
            section.Id,
            "Pregunta",
            SurveyQuestionType.ShortText,
            isRequired: true,
            allowsComment: false,
            allowsOtherOption: false,
            order: 1,
            CreatedAtUtc);
        section.AddQuestion(question, CreatedAtUtc);
        survey.AddSection(section, CreatedAtUtc);
        context.Surveys.Add(survey);
        context.SaveChanges();
        return survey;
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => "HASHED_PASSWORD";

        public PasswordVerificationResult Verify(string password, string passwordHash) =>
            PasswordVerificationResult.Success;
    }

    private sealed class FixedAccessCodeGenerator : ISurveySessionAccessCodeGenerator
    {
        private readonly string _accessCode;

        public FixedAccessCodeGenerator(string accessCode)
        {
            _accessCode = accessCode;
        }

        public string Generate() => _accessCode;
    }

    private sealed class NoOpProgressPublisher : ISurveyResponseProgressPublisher
    {
        public Task PublishAsync(
            SurveyResponseProgressDto progress,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed record AcademicFixture(Career Career, AcademicCycle Cycle);

    private sealed record AssignmentFixture(
        Career Career,
        AcademicCycle Cycle,
        Subject Subject,
        TeacherSubjectAssignment TeacherAssignment,
        Survey Survey,
        SurveyQuestion Question);
}
