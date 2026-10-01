using System.Text.RegularExpressions;
using AcademicSurveySystem.Application.Surveys.Sessions;
using AcademicSurveySystem.Domain.Common;
using AcademicSurveySystem.Domain.Surveys.Entities;
using AcademicSurveySystem.Domain.Surveys.Enums;
using AcademicSurveySystem.Infrastructure.Surveys;

namespace AcademicSurveySystem.UnitTests.Surveys;

public sealed class SurveySessionTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 9, 7, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_WithValidData_CreatesSession()
    {
        var session = CreateSession();

        Assert.Equal(SurveySessionStatus.Created, session.Status);
        Assert.True(session.IsActive);
        Assert.Equal(CreatedAtUtc, session.CreatedAtUtc);
        Assert.Equal(CreatedAtUtc, session.UpdatedAtUtc);
    }

    [Fact]
    public void Constructor_WithEmptySurveyAssignmentId_Throws()
    {
        Assert.Throws<DomainException>(() => CreateSession(surveyAssignmentId: Guid.Empty));
    }

    [Fact]
    public void Constructor_WithEmptyCreatedByUserId_Throws()
    {
        Assert.Throws<DomainException>(() => CreateSession(createdByUserId: Guid.Empty));
    }

    [Fact]
    public void Constructor_WithEmptyAccessCode_Throws()
    {
        Assert.Throws<DomainException>(() => CreateSession(accessCode: " "));
    }

    [Fact]
    public void Constructor_WithNonUtcExpiresAt_Throws()
    {
        var expiresAt = new DateTimeOffset(2026, 9, 7, 22, 0, 0, TimeSpan.FromHours(-3));

        Assert.Throws<DomainException>(() => CreateSession(expiresAtUtc: expiresAt));
    }

    [Fact]
    public void Constructor_WithExpiresAtBeforeOrEqualCreatedAt_Throws()
    {
        Assert.Throws<DomainException>(() => CreateSession(expiresAtUtc: CreatedAtUtc));
    }

    [Fact]
    public void Open_WhenCreatedActiveAndNotExpired_ChangesStatusAndOpenedAt()
    {
        var session = CreateSession();
        var openedAt = CreatedAtUtc.AddMinutes(10);

        session.Open(openedAt);

        Assert.Equal(SurveySessionStatus.Open, session.Status);
        Assert.Equal(openedAt, session.OpenedAtUtc);
        Assert.Equal(openedAt, session.UpdatedAtUtc);
    }

    [Fact]
    public void Open_WhenInactive_Throws()
    {
        var session = CreateSession();
        session.Deactivate(CreatedAtUtc.AddMinutes(1));

        Assert.Throws<DomainException>(() => session.Open(CreatedAtUtc.AddMinutes(2)));
    }

    [Fact]
    public void Open_WhenExpired_Throws()
    {
        var session = CreateSession(expiresAtUtc: CreatedAtUtc.AddMinutes(5));

        Assert.Throws<DomainException>(() => session.Open(CreatedAtUtc.AddMinutes(5)));
    }

    [Fact]
    public void Close_WhenOpen_ChangesStatusAndClosedAt()
    {
        var session = CreateSession();
        session.Open(CreatedAtUtc.AddMinutes(1));
        var closedAt = CreatedAtUtc.AddMinutes(2);

        session.Close(closedAt);

        Assert.Equal(SurveySessionStatus.Closed, session.Status);
        Assert.Equal(closedAt, session.ClosedAtUtc);
    }

    [Fact]
    public void Cancel_WhenCreated_ChangesStatus()
    {
        var session = CreateSession();
        var cancelledAt = CreatedAtUtc.AddMinutes(3);

        session.Cancel(cancelledAt);

        Assert.Equal(SurveySessionStatus.Cancelled, session.Status);
        Assert.Equal(cancelledAt, session.UpdatedAtUtc);
    }

    [Fact]
    public void Expire_WhenOpenAndPastExpiration_ChangesStatus()
    {
        var session = CreateSession(expiresAtUtc: CreatedAtUtc.AddMinutes(5));
        session.Open(CreatedAtUtc.AddMinutes(1));
        var expiredAt = CreatedAtUtc.AddMinutes(5);

        session.Expire(expiredAt);

        Assert.Equal(SurveySessionStatus.Expired, session.Status);
        Assert.Equal(expiredAt, session.UpdatedAtUtc);
    }

    [Fact]
    public void IsAvailable_ReturnsTrueOnlyWhenOpenActiveAndNotExpired()
    {
        var session = CreateSession();
        session.Open(CreatedAtUtc.AddMinutes(1));

        Assert.True(session.IsAvailable(CreatedAtUtc.AddMinutes(2)));
    }

    [Fact]
    public void IsAvailable_ReturnsFalseWhenCreated()
    {
        var session = CreateSession();

        Assert.False(session.IsAvailable(CreatedAtUtc.AddMinutes(1)));
    }

    [Fact]
    public void IsAvailable_ReturnsFalseWhenClosed()
    {
        var session = CreateSession();
        session.Close(CreatedAtUtc.AddMinutes(1));

        Assert.False(session.IsAvailable(CreatedAtUtc.AddMinutes(2)));
    }

    [Fact]
    public void IsAvailable_ReturnsFalseWhenCancelled()
    {
        var session = CreateSession();
        session.Cancel(CreatedAtUtc.AddMinutes(1));

        Assert.False(session.IsAvailable(CreatedAtUtc.AddMinutes(2)));
    }

    [Fact]
    public void IsAvailable_ReturnsFalseWhenExpired()
    {
        var session = CreateSession(expiresAtUtc: CreatedAtUtc.AddMinutes(3));
        session.Open(CreatedAtUtc.AddMinutes(1));
        session.Expire(CreatedAtUtc.AddMinutes(3));

        Assert.False(session.IsAvailable(CreatedAtUtc.AddMinutes(4)));
    }

    [Fact]
    public void IsAvailable_ReturnsFalseWhenInactive()
    {
        var session = CreateSession();
        session.Open(CreatedAtUtc.AddMinutes(1));
        session.Deactivate(CreatedAtUtc.AddMinutes(2));

        Assert.False(session.IsAvailable(CreatedAtUtc.AddMinutes(3)));
    }

    [Fact]
    public void UpdateDetails_ChangesUpdatedAt()
    {
        var session = CreateSession();
        var updatedAt = CreatedAtUtc.AddMinutes(5);

        session.UpdateDetails("Updated title", "Aula 4", CreatedAtUtc.AddHours(3), updatedAt);

        Assert.Equal(updatedAt, session.UpdatedAtUtc);
        Assert.Equal("Updated title", session.Title);
        Assert.Equal("Aula 4", session.Location);
    }

    [Fact]
    public void CreateSurveySessionRequest_WithEmptySurveyAssignmentId_Fails()
    {
        var request = new CreateSurveySessionRequest(
            Guid.Empty,
            "Title",
            "Room",
            CreatedAtUtc.AddHours(1));

        Assert.Contains(
            request.Validate(CreatedAtUtc),
            error => error.Code == "SurveySession.SurveyAssignmentIdRequired");
    }

    [Fact]
    public void CreateSurveySessionRequest_WithNonUtcExpiresAt_Fails()
    {
        var request = new CreateSurveySessionRequest(
            Guid.NewGuid(),
            "Title",
            "Room",
            new DateTimeOffset(2026, 9, 7, 22, 0, 0, TimeSpan.FromHours(-3)));

        Assert.Contains(
            request.Validate(CreatedAtUtc),
            error => error.Code == "SurveySession.ExpiresAtUtcMustBeUtc");
    }

    [Fact]
    public void CreateSurveySessionRequest_WithPastExpiresAt_Fails()
    {
        var request = new CreateSurveySessionRequest(
            Guid.NewGuid(),
            "Title",
            "Room",
            CreatedAtUtc.AddMinutes(-1));

        Assert.Contains(
            request.Validate(CreatedAtUtc),
            error => error.Code == "SurveySession.ExpiresAtUtcMustBeFuture");
    }

    [Fact]
    public void CreateSurveySessionRequest_WithTooLongTitle_Fails()
    {
        var request = new CreateSurveySessionRequest(
            Guid.NewGuid(),
            new string('a', 201),
            "Room",
            CreatedAtUtc.AddHours(1));

        Assert.Contains(
            request.Validate(CreatedAtUtc),
            error => error.Code == "SurveySession.TitleTooLong");
    }

    [Fact]
    public void UpdateSurveySessionRequest_WithValidData_Passes()
    {
        var request = new UpdateSurveySessionRequest(
            "Title",
            "Room",
            CreatedAtUtc.AddHours(1));

        Assert.Empty(request.Validate(CreatedAtUtc));
    }

    [Fact]
    public void AccessCodeGenerator_GeneratesNonEmptyUrlSafeCode()
    {
        var generator = new SurveySessionAccessCodeGenerator();

        var code = generator.Generate();

        Assert.False(string.IsNullOrWhiteSpace(code));
        Assert.Matches(new Regex("^[A-Za-z0-9_-]+$"), code);
        Assert.InRange(code.Length, 32, 64);
    }

    [Fact]
    public void AccessCodeGenerator_GeneratesDifferentCodes()
    {
        var generator = new SurveySessionAccessCodeGenerator();

        var codes = Enumerable.Range(0, 20)
            .Select(_ => generator.Generate())
            .ToArray();

        Assert.Equal(codes.Length, codes.Distinct(StringComparer.Ordinal).Count());
    }

    private static SurveySession CreateSession(
        Guid? surveyAssignmentId = null,
        Guid? createdByUserId = null,
        string accessCode = "access-code",
        DateTimeOffset? expiresAtUtc = null)
    {
        return new SurveySession(
            Guid.NewGuid(),
            surveyAssignmentId ?? Guid.NewGuid(),
            createdByUserId ?? Guid.NewGuid(),
            accessCode,
            "Title",
            "Room",
            expiresAtUtc ?? CreatedAtUtc.AddHours(2),
            CreatedAtUtc);
    }
}
