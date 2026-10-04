using System.Collections.Concurrent;
using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Surveys.Responses;
using AcademicSurveySystem.Domain.Academic.Entities;
using AcademicSurveySystem.Domain.Academic.Enums;
using AcademicSurveySystem.Domain.Surveys.Entities;
using AcademicSurveySystem.Domain.Surveys.Enums;
using AcademicSurveySystem.Infrastructure.Persistence;
using AcademicSurveySystem.Infrastructure.Surveys;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AcademicSurveySystem.UnitTests.Surveys;

public sealed class SurveyResponseServiceLimitTests
{
    [Fact]
    public async Task SubmitResponseAsync_WhenExpectedCountReached_ReturnsConflictAndDoesNotPublish()
    {
        var databaseName = Guid.NewGuid().ToString();
        var root = new InMemoryDatabaseRoot();
        var options = CreateOptions(databaseName, root);
        await using var context = new ApplicationDbContext(options);
        var fixture = SeedSurveySession(context, expectedRespondentCount: 1, sessionCount: 1);
        var publisher = new CapturingProgressPublisher();
        var service = new SurveyResponseService(context, publisher);

        var first = await service.SubmitResponseAsync(
            fixture.AccessCodes[0],
            CreateRequest(fixture.OptionId, fixture.QuestionId),
            CancellationToken.None);
        var second = await service.SubmitResponseAsync(
            fixture.AccessCodes[0],
            CreateRequest(fixture.OptionId, fixture.QuestionId),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, first.Status);
        Assert.Equal(ApplicationResultStatus.Conflict, second.Status);
        Assert.Contains(second.Errors, error => error.Code == "Survey.ResponseLimitReached");
        Assert.Single(publisher.Published);
        Assert.Equal(1, publisher.Published[0].ResponseCount);
        Assert.Equal(0, publisher.Published[0].RemainingCount);
        Assert.Equal(100m, publisher.Published[0].ParticipationPercentage);
    }

    [Fact]
    public async Task SubmitResponseAsync_EnforcesExpectedCountAcrossMultipleSessions()
    {
        var databaseName = Guid.NewGuid().ToString();
        var root = new InMemoryDatabaseRoot();
        var options = CreateOptions(databaseName, root);
        await using var context = new ApplicationDbContext(options);
        var fixture = SeedSurveySession(context, expectedRespondentCount: 1, sessionCount: 2);
        var publisher = new CapturingProgressPublisher();
        var service = new SurveyResponseService(context, publisher);

        var first = await service.SubmitResponseAsync(
            fixture.AccessCodes[0],
            CreateRequest(fixture.OptionId, fixture.QuestionId),
            CancellationToken.None);
        var second = await service.SubmitResponseAsync(
            fixture.AccessCodes[1],
            CreateRequest(fixture.OptionId, fixture.QuestionId),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, first.Status);
        Assert.Equal(ApplicationResultStatus.Conflict, second.Status);
        Assert.Equal(1, await context.SurveyResponses.CountAsync());
        Assert.Single(publisher.Published);
    }

    [Fact]
    public async Task SubmitResponseAsync_WithConcurrentSubmissions_AllowsOnlyExpectedCount()
    {
        var databaseName = Guid.NewGuid().ToString();
        var root = new InMemoryDatabaseRoot();
        var seedOptions = CreateOptions(databaseName, root);
        List<string> accessCodes;
        Guid questionId;
        Guid optionId;

        await using (var seedContext = new ApplicationDbContext(seedOptions))
        {
            var fixture = SeedSurveySession(seedContext, expectedRespondentCount: 1, sessionCount: 4);
            accessCodes = fixture.AccessCodes;
            questionId = fixture.QuestionId;
            optionId = fixture.OptionId;
        }

        var publisher = new CapturingProgressPublisher();
        var tasks = accessCodes.Select(async accessCode =>
        {
            await using var context = new ApplicationDbContext(CreateOptions(databaseName, root));
            var service = new SurveyResponseService(context, publisher);

            return await service.SubmitResponseAsync(
                accessCode,
                CreateRequest(optionId, questionId),
                CancellationToken.None);
        });

        var results = await Task.WhenAll(tasks);
        await using var verificationContext = new ApplicationDbContext(CreateOptions(databaseName, root));

        Assert.Equal(1, results.Count(result => result.Status == ApplicationResultStatus.Success));
        Assert.Equal(3, results.Count(result => result.Status == ApplicationResultStatus.Conflict));
        Assert.Equal(1, await verificationContext.SurveyResponses.CountAsync());
        Assert.Single(publisher.Published);
    }

    [Fact]
    public async Task SubmitResponseAsync_PersistsResponseEvenIfProgressPublisherFailsAfterCommit()
    {
        var databaseName = Guid.NewGuid().ToString();
        var root = new InMemoryDatabaseRoot();
        var options = CreateOptions(databaseName, root);
        await using var context = new ApplicationDbContext(options);
        var fixture = SeedSurveySession(context, expectedRespondentCount: 1, sessionCount: 1);
        var service = new SurveyResponseService(context, new ThrowingProgressPublisher());

        var result = await service.SubmitResponseAsync(
            fixture.AccessCodes[0],
            CreateRequest(fixture.OptionId, fixture.QuestionId),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.Equal(1, await context.SurveyResponses.CountAsync());
    }

    private static DbContextOptions<ApplicationDbContext> CreateOptions(
        string databaseName,
        InMemoryDatabaseRoot root)
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName, root)
            .Options;
    }

    private static SubmitSurveyResponseRequest CreateRequest(Guid optionId, Guid questionId)
    {
        return new SubmitSurveyResponseRequest([
            new SubmitSurveyAnswerRequest(
                questionId,
                [optionId],
                null,
                null,
                null,
                null)
        ]);
    }

    private static ResponseFixture SeedSurveySession(
        ApplicationDbContext context,
        int expectedRespondentCount,
        int sessionCount)
    {
        context.Database.EnsureCreated();
        var now = DateTimeOffset.UtcNow;
        var academicUnit = new AcademicUnit(Guid.NewGuid(), "unit", "Unit", now);
        var career = new Career(
            Guid.NewGuid(),
            academicUnit.Id,
            "career",
            "Career",
            CareerType.Undergraduate,
            now);
        var subject = new Subject(
            Guid.NewGuid(),
            career.Id,
            "subject",
            "Subject",
            1,
            SubjectPeriod.Annual,
            now);
        var cycle = new AcademicCycle(
            Guid.NewGuid(),
            now.Year,
            AcademicCyclePeriod.Annual,
            new DateOnly(now.Year, 1, 1),
            new DateOnly(now.Year, 12, 31),
            now);
        var teacher = new Teacher(Guid.NewGuid(), "Ada", "Lovelace", "ada@example.com", now);
        var teacherAssignment = new TeacherSubjectAssignment(
            Guid.NewGuid(),
            teacher.Id,
            subject.Id,
            cycle.Id,
            "Titular",
            now);
        var survey = CreatePublishedSurvey(now);
        var assignment = new SurveyAssignment(
            Guid.NewGuid(),
            survey.Id,
            career.Id,
            subject.Id,
            cycle.Id,
            teacherAssignment.Id,
            expectedRespondentCount,
            now);

        var accessCodes = Enumerable
            .Range(1, sessionCount)
            .Select(index => $"access-{Guid.NewGuid():N}-{index}")
            .ToList();
        var sessions = accessCodes
            .Select(accessCode =>
            {
                var session = new SurveySession(
                    Guid.NewGuid(),
                    assignment.Id,
                    Guid.NewGuid(),
                    accessCode,
                    "Sesion",
                    null,
                    DateTimeOffset.UtcNow.AddHours(1),
                    DateTimeOffset.UtcNow.AddMinutes(-1));
                session.Open(DateTimeOffset.UtcNow);
                return session;
            })
            .ToArray();

        context.AddRange(academicUnit, career, subject, cycle, teacher, teacherAssignment, survey, assignment);
        context.SurveySessions.AddRange(sessions);
        context.SaveChanges();

        var question = survey.Sections.Single().Questions.Single();

        return new ResponseFixture(
            accessCodes,
            question.Id,
            question.Options.First().Id);
    }

    private static Survey CreatePublishedSurvey(DateTimeOffset now)
    {
        var survey = new Survey(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Encuesta",
            null,
            SurveyTarget.Student,
            now);
        var section = new SurveySection(Guid.NewGuid(), survey.Id, "Seccion", null, 1, now);
        var question = new SurveyQuestion(
            Guid.NewGuid(),
            section.Id,
            "Pregunta",
            SurveyQuestionType.SingleChoice,
            isRequired: true,
            allowsComment: false,
            allowsOtherOption: false,
            order: 1,
            now);
        question.AddOption(new SurveyQuestionOption(Guid.NewGuid(), question.Id, "A", "a", 1, now), now);
        question.AddOption(new SurveyQuestionOption(Guid.NewGuid(), question.Id, "B", "b", 2, now), now);
        section.AddQuestion(question, now);
        survey.AddSection(section, now);
        survey.Publish(now);

        return survey;
    }

    private sealed class CapturingProgressPublisher : ISurveyResponseProgressPublisher
    {
        private readonly ConcurrentBag<SurveyResponseProgressDto> _published = [];

        public IReadOnlyList<SurveyResponseProgressDto> Published => _published.ToArray();

        public Task PublishAsync(
            SurveyResponseProgressDto progress,
            CancellationToken cancellationToken = default)
        {
            _published.Add(progress);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingProgressPublisher : ISurveyResponseProgressPublisher
    {
        public Task PublishAsync(
            SurveyResponseProgressDto progress,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Publisher failure");
        }
    }

    private sealed record ResponseFixture(
        List<string> AccessCodes,
        Guid QuestionId,
        Guid OptionId);
}
