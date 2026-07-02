using AcademicSurveySystem.Domain.Common;
using AcademicSurveySystem.Domain.Identity.Entities;
using AcademicSurveySystem.Domain.Identity.Enums;

namespace AcademicSurveySystem.UnitTests.Identity;

public sealed class UserTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedAt = new(2026, 1, 2, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CreatesValidUser()
    {
        var user = CreateUser();

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("Luis", user.FirstName);
        Assert.Equal("Perez", user.LastName);
        Assert.Equal(UserStatus.Active, user.Status);
        Assert.Equal(CreatedAt, user.CreatedAtUtc);
        Assert.Equal(CreatedAt, user.UpdatedAtUtc);
    }

    [Fact]
    public void Constructor_NormalizesEmail()
    {
        var user = new User(Guid.NewGuid(), "Luis", "Perez", "  LUIS@Example.COM  ", "hash", CreatedAt);

        Assert.Equal("luis@example.com", user.Email);
        Assert.Equal("LUIS@EXAMPLE.COM", user.NormalizedEmail);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsEmptyFirstName(string firstName)
    {
        Assert.Throws<DomainException>(() =>
            new User(Guid.NewGuid(), firstName, "Perez", "luis@example.com", "hash", CreatedAt));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsEmptyLastName(string lastName)
    {
        Assert.Throws<DomainException>(() =>
            new User(Guid.NewGuid(), "Luis", lastName, "luis@example.com", "hash", CreatedAt));
    }

    [Fact]
    public void Constructor_RejectsInvalidEmail()
    {
        Assert.Throws<DomainException>(() =>
            new User(Guid.NewGuid(), "Luis", "Perez", "invalid-email", "hash", CreatedAt));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsEmptyPasswordHash(string passwordHash)
    {
        Assert.Throws<DomainException>(() =>
            new User(Guid.NewGuid(), "Luis", "Perez", "luis@example.com", passwordHash, CreatedAt));
    }

    [Fact]
    public void Constructor_SetsInitialStatusActive()
    {
        Assert.Equal(UserStatus.Active, CreateUser().Status);
    }

    [Fact]
    public void Deactivate_ChangesStatus()
    {
        var user = CreateUser();

        user.Deactivate(UpdatedAt);

        Assert.Equal(UserStatus.Inactive, user.Status);
    }

    [Fact]
    public void Block_ChangesStatus()
    {
        var user = CreateUser();

        user.Block(UpdatedAt);

        Assert.Equal(UserStatus.Blocked, user.Status);
    }

    [Fact]
    public void Activate_ChangesStatus()
    {
        var user = CreateUser();
        user.Block(UpdatedAt);

        user.Activate(UpdatedAt.AddDays(1));

        Assert.Equal(UserStatus.Active, user.Status);
    }

    [Fact]
    public void UpdateName_ChangesUpdatedAtUtc()
    {
        var user = CreateUser();

        user.UpdateName("Ana", "Lopez", UpdatedAt);

        Assert.Equal("Ana", user.FirstName);
        Assert.Equal("Lopez", user.LastName);
        Assert.Equal(UpdatedAt, user.UpdatedAtUtc);
    }

    [Fact]
    public void AssignRole_AddsRole()
    {
        var user = CreateUser();
        var roleId = Guid.NewGuid();

        user.AssignRole(roleId, UpdatedAt);

        Assert.Contains(user.UserRoles, userRole => userRole.RoleId == roleId);
    }

    [Fact]
    public void AssignRole_DoesNotDuplicateRole()
    {
        var user = CreateUser();
        var roleId = Guid.NewGuid();

        user.AssignRole(roleId, UpdatedAt);
        user.AssignRole(roleId, UpdatedAt.AddDays(1));

        Assert.Single(user.UserRoles);
    }

    [Fact]
    public void RemoveRole_RemovesRole()
    {
        var user = CreateUser();
        var roleId = Guid.NewGuid();
        user.AssignRole(roleId, UpdatedAt);

        user.RemoveRole(roleId);

        Assert.Empty(user.UserRoles);
    }

    [Fact]
    public void Constructor_RejectsNonUtcDate()
    {
        var nonUtc = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.FromHours(-3));

        Assert.Throws<DomainException>(() =>
            new User(Guid.NewGuid(), "Luis", "Perez", "luis@example.com", "hash", nonUtc));
    }

    private static User CreateUser()
    {
        return new User(Guid.NewGuid(), "  Luis  ", "  Perez  ", "luis@example.com", "hash", CreatedAt);
    }
}
