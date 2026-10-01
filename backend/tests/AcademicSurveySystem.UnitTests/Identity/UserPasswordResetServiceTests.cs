using AcademicSurveySystem.Application.Common.Security;
using AcademicSurveySystem.Application.Identity.UserPasswordReset;
using AcademicSurveySystem.Domain.Academic.Entities;
using AcademicSurveySystem.Domain.Academic.Enums;
using AcademicSurveySystem.Domain.Identity;
using AcademicSurveySystem.Domain.Identity.Entities;
using AcademicSurveySystem.Domain.Identity.Enums;
using AcademicSurveySystem.Infrastructure.Identity;
using AcademicSurveySystem.Infrastructure.Persistence;
using AcademicSurveySystem.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AcademicSurveySystem.UnitTests.Identity;

public sealed class UserPasswordResetServiceTests
{
    private const string OldPassword = "OldValidPassword1!";
    private const string NewPassword = "NewValidPassword1!";
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ResetAsync_WithValidRequest_UpdatesPasswordHash()
    {
        using var context = CreateContext();
        var hasher = CreateHasher();
        var user = CreateUser(hasher.Hash(OldPassword));
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var originalHash = user.PasswordHash;
        var service = CreateService(context, hasher);

        var result = await service.ResetAsync(user.Email, NewPassword);

        Assert.True(result.Succeeded);
        Assert.NotEqual(originalHash, user.PasswordHash);
        Assert.True(user.UpdatedAtUtc > user.CreatedAtUtc);
    }

    [Fact]
    public async Task ResetAsync_NewPasswordVerifiesAndOldPasswordStopsVerifying()
    {
        using var context = CreateContext();
        var hasher = CreateHasher();
        var user = CreateUser(hasher.Hash(OldPassword));
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var service = CreateService(context, hasher);

        var result = await service.ResetAsync(user.Email, NewPassword);

        Assert.True(result.Succeeded);
        Assert.Equal(PasswordVerificationResult.Success, hasher.Verify(NewPassword, user.PasswordHash));
        Assert.Equal(PasswordVerificationResult.Failed, hasher.Verify(OldPassword, user.PasswordHash));
    }

    [Fact]
    public async Task ResetByUserIdAsync_NewPasswordVerifiesAndOldPasswordStopsVerifying()
    {
        using var context = CreateContext();
        var hasher = CreateHasher();
        var user = CreateUser(hasher.Hash(OldPassword));
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var service = CreateService(context, hasher);

        var result = await service.ResetByUserIdAsync(user.Id, NewPassword);

        Assert.True(result.Succeeded);
        Assert.Equal(PasswordVerificationResult.Success, hasher.Verify(NewPassword, user.PasswordHash));
        Assert.Equal(PasswordVerificationResult.Failed, hasher.Verify(OldPassword, user.PasswordHash));
    }

    [Fact]
    public async Task ResetAsync_WithMissingUser_ReturnsControlledResultAndDoesNotCreateUser()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.ResetAsync("missing@institucion.edu.ar", NewPassword);

        Assert.Equal(UserPasswordResetStatus.UserNotFound, result.Status);
        Assert.Empty(context.Users);
    }

    [Fact]
    public async Task ResetByUserIdAsync_WithMissingUser_ReturnsControlledResult()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.ResetByUserIdAsync(Guid.NewGuid(), NewPassword);

        Assert.Equal(UserPasswordResetStatus.UserNotFound, result.Status);
        Assert.Empty(context.Users);
    }

    [Fact]
    public async Task ResetAsync_WithInvalidPassword_ReturnsControlledResultWithoutChangingHash()
    {
        using var context = CreateContext();
        var hasher = CreateHasher();
        var user = CreateUser(hasher.Hash(OldPassword));
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var originalHash = user.PasswordHash;
        var service = CreateService(context, hasher);

        var result = await service.ResetAsync(user.Email, "short");

        Assert.Equal(UserPasswordResetStatus.InvalidPassword, result.Status);
        Assert.Equal(originalHash, user.PasswordHash);
    }

    [Fact]
    public async Task ResetByUserIdAsync_WithInvalidPassword_DoesNotChangeHash()
    {
        using var context = CreateContext();
        var hasher = CreateHasher();
        var user = CreateUser(hasher.Hash(OldPassword));
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var originalHash = user.PasswordHash;
        var service = CreateService(context, hasher);

        var result = await service.ResetByUserIdAsync(user.Id, "short");

        Assert.Equal(UserPasswordResetStatus.InvalidPassword, result.Status);
        Assert.Equal(originalHash, user.PasswordHash);
    }

    [Theory]
    [InlineData("RESET.USER@INSTITUCION.EDU.AR")]
    [InlineData(" reset.user@institucion.edu.ar ")]
    public async Task ResetAsync_NormalizesEmailForLookup(string email)
    {
        using var context = CreateContext();
        var hasher = CreateHasher();
        var user = CreateUser(hasher.Hash(OldPassword));
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var service = CreateService(context, hasher);

        var result = await service.ResetAsync(email, NewPassword);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ResetAsync_InactiveUserKeepsStatus()
    {
        using var context = CreateContext();
        var hasher = CreateHasher();
        var user = CreateUser(hasher.Hash(OldPassword));
        user.Deactivate(CreatedAtUtc.AddMinutes(1));
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var service = CreateService(context, hasher);

        var result = await service.ResetAsync(user.Email, NewPassword);

        Assert.True(result.Succeeded);
        Assert.Equal(UserStatus.Inactive, user.Status);
    }

    [Fact]
    public async Task ResetAsync_DoesNotModifyRolesOrUserCareers()
    {
        using var context = CreateContext();
        var hasher = CreateHasher();
        var user = CreateUser(hasher.Hash(OldPassword));
        var role = IdentityCatalog.Roles.Single(item => item.Code == "career_director");
        var academicUnit = new AcademicUnit(
            Guid.NewGuid(),
            "academic-unit",
            "Academic Unit",
            CreatedAtUtc);
        var career = new Career(
            Guid.NewGuid(),
            academicUnit.Id,
            "tuds",
            "TUDS",
            CareerType.Undergraduate,
            CreatedAtUtc);
        user.AssignRole(role.Id, CreatedAtUtc);
        user.AssignCareer(career.Id, CreatedAtUtc);
        context.AddRange(user, academicUnit, career);
        await context.SaveChangesAsync();
        var service = CreateService(context, hasher);

        var result = await service.ResetAsync(user.Email, NewPassword);

        Assert.True(result.Succeeded);
        Assert.Single(context.UserRoles.Where(item => item.UserId == user.Id));
        Assert.Single(context.UserCareers.Where(item => item.UserId == user.Id));
    }

    private static UserPasswordResetService CreateService(
        ApplicationDbContext context,
        IPasswordHasher? hasher = null)
    {
        return new UserPasswordResetService(
            context,
            hasher ?? CreateHasher(),
            NullLogger<UserPasswordResetService>.Instance);
    }

    private static BcryptPasswordHasher CreateHasher()
    {
        return new BcryptPasswordHasher(Options.Create(new PasswordHashingOptions
        {
            WorkFactor = PasswordHashingOptions.MinimumWorkFactor
        }));
    }

    private static User CreateUser(string passwordHash)
    {
        return new User(
            Guid.NewGuid(),
            "Reset",
            "User",
            "reset.user@institucion.edu.ar",
            passwordHash,
            CreatedAtUtc);
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
}
