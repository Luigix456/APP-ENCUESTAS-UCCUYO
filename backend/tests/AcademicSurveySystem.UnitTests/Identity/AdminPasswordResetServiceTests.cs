using AcademicSurveySystem.Application.Common.Security;
using AcademicSurveySystem.Application.Identity.AdminPasswordReset;
using AcademicSurveySystem.Domain.Identity;
using AcademicSurveySystem.Domain.Identity.Entities;
using AcademicSurveySystem.Infrastructure.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AcademicSurveySystem.UnitTests.Identity;

public sealed class AdminPasswordResetServiceTests
{
    private const string ValidPassword = "NewValidPassword1!";
    private const string HashedPassword = "HASHED_NEW_PASSWORD";

    [Fact]
    public async Task ResetAsync_FailsBeforeStoreAccess_WhenConfigurationIsIncomplete()
    {
        var store = new FakeAdminPasswordResetStore();
        var hasher = new FakePasswordHasher();
        var service = CreateService(store, hasher, email: null);

        var result = await service.ResetAsync();

        Assert.Equal(AdminPasswordResetStatus.Failed, result.Status);
        Assert.Equal(0, store.FindUserCallCount);
        Assert.Equal(0, hasher.HashCallCount);
    }

    [Fact]
    public async Task ResetAsync_FailsBeforeStoreAccess_WhenNewPasswordIsInvalid()
    {
        var store = new FakeAdminPasswordResetStore();
        var hasher = new FakePasswordHasher();
        var service = CreateService(store, hasher, newPassword: "short");

        var result = await service.ResetAsync();

        Assert.Equal(AdminPasswordResetStatus.Failed, result.Status);
        Assert.Contains("invalid", result.Message);
        Assert.DoesNotContain("short", result.Message);
        Assert.Equal(0, store.FindUserCallCount);
        Assert.Equal(0, hasher.HashCallCount);
    }

    [Fact]
    public async Task ResetAsync_Fails_WhenUserDoesNotExist()
    {
        var store = new FakeAdminPasswordResetStore();
        var hasher = new FakePasswordHasher();
        var service = CreateService(store, hasher);

        var result = await service.ResetAsync();

        Assert.Equal(AdminPasswordResetStatus.Failed, result.Status);
        Assert.Contains("User was not found", result.Message);
        Assert.Equal("ADMIN@INSTITUCION.EDU.AR", store.LastNormalizedEmail);
        Assert.Equal(0, hasher.HashCallCount);
    }

    [Fact]
    public async Task ResetAsync_FailsAndDoesNotHash_WhenUserIsNotAdministrator()
    {
        var user = CreateUser(assignAdministratorRole: false);
        var originalHash = user.PasswordHash;
        var store = new FakeAdminPasswordResetStore(user);
        var hasher = new FakePasswordHasher();
        var service = CreateService(store, hasher);

        var result = await service.ResetAsync();

        Assert.Equal(AdminPasswordResetStatus.Failed, result.Status);
        Assert.Contains("not an administrator", result.Message);
        Assert.Equal(originalHash, user.PasswordHash);
        Assert.Equal(0, hasher.HashCallCount);
        Assert.Equal(0, store.SaveChangesCallCount);
    }

    [Fact]
    public async Task ResetAsync_UpdatesPasswordHashAndUpdatedAt_WhenUserIsAdministrator()
    {
        var user = CreateUser(assignAdministratorRole: true);
        var originalUpdatedAt = user.UpdatedAtUtc;
        var store = new FakeAdminPasswordResetStore(user);
        var hasher = new FakePasswordHasher();
        var service = CreateService(store, hasher);

        var result = await service.ResetAsync();

        Assert.Equal(AdminPasswordResetStatus.Updated, result.Status);
        Assert.Equal(HashedPassword, user.PasswordHash);
        Assert.True(user.UpdatedAtUtc > originalUpdatedAt);
        Assert.Equal(1, hasher.HashCallCount);
        Assert.Equal(1, store.SaveChangesCallCount);
        Assert.True(store.TransactionCommitted);
    }

    [Fact]
    public async Task ResetAsync_DoesNotPrintPasswordOrHashInResult()
    {
        var user = CreateUser(assignAdministratorRole: true);
        var store = new FakeAdminPasswordResetStore(user);
        var hasher = new FakePasswordHasher();
        var service = CreateService(store, hasher);

        var result = await service.ResetAsync();

        Assert.DoesNotContain(ValidPassword, result.Message);
        Assert.DoesNotContain(HashedPassword, result.Message);
    }

    private static AdminPasswordResetService CreateService(
        FakeAdminPasswordResetStore store,
        FakePasswordHasher hasher,
        string? email = "admin@institucion.edu.ar",
        string? newPassword = ValidPassword)
    {
        return new AdminPasswordResetService(
            store,
            hasher,
            Options.Create(new AdminPasswordResetOptions
            {
                Email = email,
                NewPassword = newPassword
            }),
            NullLogger<AdminPasswordResetService>.Instance);
    }

    private static User CreateUser(bool assignAdministratorRole)
    {
        var createdAtUtc = DateTimeOffset.UtcNow.AddDays(-1);
        var user = new User(
            Guid.NewGuid(),
            "Admin",
            "User",
            "admin@institucion.edu.ar",
            "EXISTING_HASH",
            createdAtUtc);

        if (assignAdministratorRole)
        {
            user.AssignRole(IdentityCatalog.RoleIds.AdministratorId, createdAtUtc);
        }

        return user;
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public int HashCallCount { get; private set; }

        public string Hash(string password)
        {
            HashCallCount++;
            return HashedPassword;
        }

        public PasswordVerificationResult Verify(string password, string passwordHash)
        {
            return PasswordVerificationResult.Success;
        }
    }

    private sealed class FakeAdminPasswordResetStore : IAdminPasswordResetStore
    {
        private readonly User? _user;

        public FakeAdminPasswordResetStore(User? user = null)
        {
            _user = user;
        }

        public int FindUserCallCount { get; private set; }
        public int SaveChangesCallCount { get; private set; }
        public bool TransactionCommitted { get; private set; }
        public string? LastNormalizedEmail { get; private set; }

        public Task<User?> FindUserByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken)
        {
            FindUserCallCount++;
            LastNormalizedEmail = normalizedEmail;

            return Task.FromResult(
                _user?.NormalizedEmail == normalizedEmail
                    ? _user
                    : null);
        }

        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken)
        {
            await operation(cancellationToken);
            TransactionCommitted = true;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }
}
