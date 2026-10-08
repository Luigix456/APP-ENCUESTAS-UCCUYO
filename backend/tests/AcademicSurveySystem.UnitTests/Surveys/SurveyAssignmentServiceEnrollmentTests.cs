using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Surveys.Assignments;
using AcademicSurveySystem.Domain.Academic.Entities;
using AcademicSurveySystem.Domain.Academic.Enums;
using AcademicSurveySystem.Domain.Surveys.Entities;
using AcademicSurveySystem.Domain.Surveys.Enums;
using AcademicSurveySystem.Infrastructure.Persistence;
using AcademicSurveySystem.Infrastructure.Surveys;
using Microsoft.EntityFrameworkCore;

namespace AcademicSurveySystem.UnitTests.Surveys;

public sealed class SurveyAssignmentServiceEnrollmentTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateAssignmentAsync_ForStudentSurvey_UsesSubjectEnrollmentAsSnapshot()
    {
        using var context = CreateContext();
        var academic = SeedAcademicContext(context);
        var survey = SeedPublishedSurvey(context, SurveyTarget.Student);
        context.SubjectEnrollments.Add(
            new SubjectEnrollment(Guid.NewGuid(), academic.Subject.Id, academic.Cycle.Id, 35, CreatedAtUtc));
        await context.SaveChangesAsync();
        var service = new SurveyAssignmentService(context);

        var result = await service.CreateAssignmentAsync(
            CreateRequest(survey.Id, academic),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.Equal(35, result.Value!.ExpectedRespondentCount);
    }

    [Fact]
    public async Task CreateAssignmentAsync_KeepsExistingAssignmentSnapshotWhenEnrollmentChanges()
    {
        using var context = CreateContext();
        var academic = SeedAcademicContext(context);
        var surveyV1 = SeedPublishedSurvey(context, SurveyTarget.Student);
        var surveyV2 = SeedPublishedSurvey(context, SurveyTarget.Student);
        var enrollment = new SubjectEnrollment(
            Guid.NewGuid(),
            academic.Subject.Id,
            academic.Cycle.Id,
            35,
            CreatedAtUtc);
        context.SubjectEnrollments.Add(enrollment);
        await context.SaveChangesAsync();
        var service = new SurveyAssignmentService(context);

        var first = await service.CreateAssignmentAsync(
            CreateRequest(surveyV1.Id, academic),
            CancellationToken.None);
        enrollment.UpdateEnrolledStudentCount(42, CreatedAtUtc.AddMinutes(1));
        await context.SaveChangesAsync();
        var second = await service.CreateAssignmentAsync(
            CreateRequest(surveyV2.Id, academic),
            CancellationToken.None);
        var reloadedFirst = await service.GetAssignmentByIdAsync(first.Value!.Id, CancellationToken.None);

        Assert.Equal(35, first.Value.ExpectedRespondentCount);
        Assert.Equal(35, reloadedFirst.Value!.ExpectedRespondentCount);
        Assert.Equal(42, second.Value!.ExpectedRespondentCount);
    }

    [Fact]
    public async Task CreateAssignmentAsync_ForStudentSurvey_RequiresEnrollment()
    {
        using var context = CreateContext();
        var academic = SeedAcademicContext(context);
        var survey = SeedPublishedSurvey(context, SurveyTarget.Student);
        var service = new SurveyAssignmentService(context);

        var result = await service.CreateAssignmentAsync(
            CreateRequest(survey.Id, academic),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Validation, result.Status);
        Assert.Contains(result.Errors, error => error.Code == "Subject.EnrollmentRequired");
    }

    [Fact]
    public async Task CreateAssignmentAsync_ForNonStudentSurvey_DoesNotRequireEnrollment()
    {
        using var context = CreateContext();
        var academic = SeedAcademicContext(context);
        var survey = SeedPublishedSurvey(context, SurveyTarget.Teacher);
        var service = new SurveyAssignmentService(context);

        var result = await service.CreateAssignmentAsync(
            CreateRequest(survey.Id, academic),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.Null(result.Value!.ExpectedRespondentCount);
    }

    [Fact]
    public async Task CreateAssignmentsAsync_WithMultipleTeachers_CreatesAssignmentsAtomically()
    {
        using var context = CreateContext();
        var academic = SeedAcademicContext(context);
        var secondTeacherAssignment = SeedTeacherAssignment(context, academic.Subject.Id, academic.Cycle.Id, "Grace", "Hopper");
        var survey = SeedPublishedSurvey(context, SurveyTarget.Student);
        context.SubjectEnrollments.Add(
            new SubjectEnrollment(Guid.NewGuid(), academic.Subject.Id, academic.Cycle.Id, 35, CreatedAtUtc));
        await context.SaveChangesAsync();
        var service = new SurveyAssignmentService(context);

        var result = await service.CreateAssignmentsAsync(
            new CreateSurveyAssignmentBatchRequest(
                survey.Id,
                academic.Career.Id,
                academic.Subject.Id,
                academic.Cycle.Id,
                [academic.TeacherAssignment.Id, secondTeacherAssignment.Id]),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.Equal(2, result.Value!.Count);
        Assert.All(result.Value, assignment => Assert.Equal(35, assignment.ExpectedRespondentCount));
        Assert.Equal(2, await context.SurveyAssignments.CountAsync());
    }

    [Fact]
    public async Task CreateAssignmentsAsync_WhenOneTeacherAssignmentIsInvalid_DoesNotCreateAnyAssignment()
    {
        using var context = CreateContext();
        var academic = SeedAcademicContext(context);
        var otherCycle = new AcademicCycle(
            Guid.NewGuid(),
            2027,
            AcademicCyclePeriod.Annual,
            new DateOnly(2027, 1, 1),
            new DateOnly(2027, 12, 31),
            CreatedAtUtc);
        context.AcademicCycles.Add(otherCycle);
        var invalidTeacherAssignment = SeedTeacherAssignment(context, academic.Subject.Id, otherCycle.Id, "Grace", "Hopper");
        var survey = SeedPublishedSurvey(context, SurveyTarget.Teacher);
        await context.SaveChangesAsync();
        var service = new SurveyAssignmentService(context);

        var result = await service.CreateAssignmentsAsync(
            new CreateSurveyAssignmentBatchRequest(
                survey.Id,
                academic.Career.Id,
                academic.Subject.Id,
                academic.Cycle.Id,
                [academic.TeacherAssignment.Id, invalidTeacherAssignment.Id]),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Validation, result.Status);
        Assert.Contains(
            result.Errors,
            error => error.Code == "SurveyAssignment.TeacherSubjectAssignmentCycleMismatch");
        Assert.Equal(0, await context.SurveyAssignments.CountAsync());
    }

    [Fact]
    public async Task CreateAssignmentsAsync_WhenDuplicateExists_ReturnsConflictWithoutPartialWrites()
    {
        using var context = CreateContext();
        var academic = SeedAcademicContext(context);
        var secondTeacherAssignment = SeedTeacherAssignment(context, academic.Subject.Id, academic.Cycle.Id, "Grace", "Hopper");
        var survey = SeedPublishedSurvey(context, SurveyTarget.Teacher);
        var service = new SurveyAssignmentService(context);
        var existing = await service.CreateAssignmentAsync(
            CreateRequest(survey.Id, academic),
            CancellationToken.None);

        var result = await service.CreateAssignmentsAsync(
            new CreateSurveyAssignmentBatchRequest(
                survey.Id,
                academic.Career.Id,
                academic.Subject.Id,
                academic.Cycle.Id,
                [academic.TeacherAssignment.Id, secondTeacherAssignment.Id]),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Conflict, result.Status);
        Assert.Equal(1, await context.SurveyAssignments.CountAsync());
        Assert.Equal(existing.Value!.Id, (await context.SurveyAssignments.SingleAsync()).Id);
    }

    private static CreateSurveyAssignmentRequest CreateRequest(
        Guid surveyId,
        AcademicFixture academic)
    {
        return new CreateSurveyAssignmentRequest(
            surveyId,
            academic.Career.Id,
            academic.Subject.Id,
            academic.Cycle.Id,
            academic.TeacherAssignment.Id);
    }

    private static Survey SeedPublishedSurvey(
        ApplicationDbContext context,
        SurveyTarget target)
    {
        var survey = new Survey(
            Guid.NewGuid(),
            Guid.NewGuid(),
            $"Encuesta {Guid.NewGuid():N}",
            null,
            target,
            CreatedAtUtc);
        var section = new SurveySection(
            Guid.NewGuid(),
            survey.Id,
            "Seccion",
            null,
            1,
            CreatedAtUtc);
        var question = new SurveyQuestion(
            Guid.NewGuid(),
            section.Id,
            "Pregunta",
            SurveyQuestionType.SingleChoice,
            isRequired: true,
            allowsComment: false,
            allowsOtherOption: false,
            order: 1,
            CreatedAtUtc);
        question.AddOption(
            new SurveyQuestionOption(Guid.NewGuid(), question.Id, "A", "a", 1, CreatedAtUtc),
            CreatedAtUtc);
        question.AddOption(
            new SurveyQuestionOption(Guid.NewGuid(), question.Id, "B", "b", 2, CreatedAtUtc),
            CreatedAtUtc);
        section.AddQuestion(question, CreatedAtUtc);
        survey.AddSection(section, CreatedAtUtc);
        survey.Publish(CreatedAtUtc.AddMinutes(1));

        context.Surveys.Add(survey);
        context.SaveChanges();

        return survey;
    }

    private static AcademicFixture SeedAcademicContext(ApplicationDbContext context)
    {
        var academicUnit = new AcademicUnit(
            Guid.NewGuid(),
            "academic-unit",
            "Academic Unit",
            CreatedAtUtc);
        var career = new Career(
            Guid.NewGuid(),
            academicUnit.Id,
            "career",
            "Career",
            CareerType.Undergraduate,
            CreatedAtUtc);
        var subject = new Subject(
            Guid.NewGuid(),
            career.Id,
            "subject",
            "Subject",
            1,
            SubjectPeriod.Annual,
            CreatedAtUtc);
        var cycle = new AcademicCycle(
            Guid.NewGuid(),
            2026,
            AcademicCyclePeriod.Annual,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            CreatedAtUtc);
        var teacher = new Teacher(Guid.NewGuid(), "Ada", "Lovelace", "ada@example.com", CreatedAtUtc);
        var teacherAssignment = new TeacherSubjectAssignment(
            Guid.NewGuid(),
            teacher.Id,
            subject.Id,
            cycle.Id,
            "Titular",
            CreatedAtUtc);

        context.AddRange(academicUnit, career, subject, cycle, teacher, teacherAssignment);
        context.SaveChanges();

        return new AcademicFixture(career, subject, cycle, teacherAssignment);
    }

    private static TeacherSubjectAssignment SeedTeacherAssignment(
        ApplicationDbContext context,
        Guid subjectId,
        Guid academicCycleId,
        string firstName,
        string lastName)
    {
        var teacher = new Teacher(
            Guid.NewGuid(),
            firstName,
            lastName,
            $"{firstName}.{lastName}@example.com",
            CreatedAtUtc);
        var teacherAssignment = new TeacherSubjectAssignment(
            Guid.NewGuid(),
            teacher.Id,
            subjectId,
            academicCycleId,
            "Adjunto",
            CreatedAtUtc);

        context.AddRange(teacher, teacherAssignment);
        context.SaveChanges();

        return teacherAssignment;
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

    private sealed record AcademicFixture(
        Career Career,
        Subject Subject,
        AcademicCycle Cycle,
        TeacherSubjectAssignment TeacherAssignment);
}
