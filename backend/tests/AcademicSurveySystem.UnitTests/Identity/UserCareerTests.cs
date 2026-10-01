using AcademicSurveySystem.Domain.Common;
using AcademicSurveySystem.Domain.Identity.Entities;

namespace AcademicSurveySystem.UnitTests.Identity;

public sealed class UserCareerTests
{
    private static readonly DateTimeOffset AssignedAtUtc =
        new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_WithValidData_CreatesAssociation()
    {
        var userId = Guid.NewGuid();
        var careerId = Guid.NewGuid();

        var userCareer = new UserCareer(userId, careerId, AssignedAtUtc);

        Assert.Equal(userId, userCareer.UserId);
        Assert.Equal(careerId, userCareer.CareerId);
        Assert.Equal(AssignedAtUtc, userCareer.AssignedAtUtc);
    }

    [Fact]
    public void Constructor_WithEmptyUserId_Throws()
    {
        Assert.Throws<DomainException>(() =>
            new UserCareer(Guid.Empty, Guid.NewGuid(), AssignedAtUtc));
    }

    [Fact]
    public void Constructor_WithEmptyCareerId_Throws()
    {
        Assert.Throws<DomainException>(() =>
            new UserCareer(Guid.NewGuid(), Guid.Empty, AssignedAtUtc));
    }

    [Fact]
    public void Constructor_WithNonUtcAssignedAt_Throws()
    {
        var assignedAt = new DateTimeOffset(2026, 9, 23, 9, 0, 0, TimeSpan.FromHours(-3));

        Assert.Throws<DomainException>(() =>
            new UserCareer(Guid.NewGuid(), Guid.NewGuid(), assignedAt));
    }

    [Fact]
    public void UserAssignCareer_DoesNotDuplicateAssociation()
    {
        var user = CreateUser();
        var careerId = Guid.NewGuid();

        user.AssignCareer(careerId, AssignedAtUtc);
        user.AssignCareer(careerId, AssignedAtUtc.AddMinutes(1));

        Assert.Single(user.UserCareers);
    }

    [Fact]
    public void UserRemoveCareer_RemovesExistingAssociation()
    {
        var user = CreateUser();
        var careerId = Guid.NewGuid();
        user.AssignCareer(careerId, AssignedAtUtc);

        user.RemoveCareer(careerId);

        Assert.Empty(user.UserCareers);
    }

    private static User CreateUser()
    {
        return new User(
            Guid.NewGuid(),
            "Test",
            "User",
            "test.user@example.com",
            "HASH",
            AssignedAtUtc);
    }
}
