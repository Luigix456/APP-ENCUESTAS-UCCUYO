using AcademicSurveySystem.Application.Common.Security;
using AcademicSurveySystem.Application.Identity.InitialAdministrator;
using AcademicSurveySystem.Domain.Identity;
using AcademicSurveySystem.Domain.Identity.Entities;
using AcademicSurveySystem.Infrastructure.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AcademicSurveySystem.UnitTests.Identity;

public sealed class InitialAdministratorBootstrapperTests
{
    [Fact]
    public async Task BootstrapAsync_FailsBeforeStoreAccess_WhenConfigurationIsIncomplete()
    {
        var store = new FakeInitialAdministratorStore();
        var hasher = new FakePasswordHasher();
        var bootstrapper = CreateBootstrapper(store, hasher, password: null);

        var result = await bootstrapper.BootstrapAsync();

        Assert.Equal(InitialAdministratorBootstrapStatus.Failed, result.Status);
        Assert.Equal(0, store.AccessCount);
        Assert.Equal(0, hasher.HashCallCount);
    }

    [Fact]
    public async Task BootstrapAsync_FailsBeforeHashing_WhenPasswordIsInvalid()
    {
        var store = new FakeInitialAdministratorStore();
        var hasher = new FakePasswordHasher();
        var bootstrapper = CreateBootstrapper(store, hasher, password: "short");

        var result = await bootstrapper.BootstrapAsync();

        Assert.Equal(InitialAdministratorBootstrapStatus.Failed, result.Status);
        Assert.Equal(0, store.AccessCount);
        Assert.Equal(0, hasher.HashCallCount);
    }

    [Fact]
    public async Task BootstrapAsync_Fails_WhenAdministratorRoleDoesNotExist()
    {
        var store = new FakeInitialAdministratorStore
        {
            AdministratorRoleExists = false
        };
        var hasher = new FakePasswordHasher();
        var bootstrapper = CreateBootstrapper(store, hasher);

        var result = await bootstrapper.BootstrapAsync();

        Assert.Equal(InitialAdministratorBootstrapStatus.Failed, result.Status);
        Assert.Contains("migrations", result.Message);
        Assert.Equal(0, hasher.HashCallCount);
    }

    [Fact]
    public async Task BootstrapAsync_ReturnsAlreadyExists_WhenAdministratorExists()
    {
        var store = new FakeInitialAdministratorStore
        {
            AnyAdministratorExists = true
        };
        var hasher = new FakePasswordHasher();
        var bootstrapper = CreateBootstrapper(store, hasher);

        var result = await bootstrapper.BootstrapAsync();

        Assert.Equal(InitialAdministratorBootstrapStatus.AlreadyExists, result.Status);
        Assert.Equal(0, hasher.HashCallCount);
        Assert.Empty(store.Users);
    }

    [Fact]
    public async Task BootstrapAsync_DoesNotModifyPassword_WhenAdministratorExists()
    {
        var store = new FakeInitialAdministratorStore
        {
            AnyAdministratorExists = true,
            ExistingAdministratorPasswordHash = "existing-hash"
        };
        var hasher = new FakePasswordHasher();
        var bootstrapper = CreateBootstrapper(store, hasher);

        var result = await bootstrapper.BootstrapAsync();

        Assert.Equal(InitialAdministratorBootstrapStatus.AlreadyExists, result.Status);
        Assert.Equal("existing-hash", store.ExistingAdministratorPasswordHash);
        Assert.Equal(0, hasher.HashCallCount);
    }

    [Fact]
    public async Task BootstrapAsync_Fails_WhenEmailBelongsToNonAdministratorUser()
    {
        var store = new FakeInitialAdministratorStore
        {
            UserWithNormalizedEmailExists = true
        };
        var hasher = new FakePasswordHasher();
        var bootstrapper = CreateBootstrapper(store, hasher);

        var result = await bootstrapper.BootstrapAsync();

        Assert.Equal(InitialAdministratorBootstrapStatus.Failed, result.Status);
        Assert.Contains("will not be elevated", result.Message);
        Assert.Equal(0, hasher.HashCallCount);
        Assert.Empty(store.Users);
    }

    [Fact]
    public async Task BootstrapAsync_CreatesUserAndUserRole_WhenAdministratorDoesNotExist()
    {
        var store = new FakeInitialAdministratorStore();
        var hasher = new FakePasswordHasher();
        var bootstrapper = CreateBootstrapper(store, hasher);

        var result = await bootstrapper.BootstrapAsync();

        Assert.Equal(InitialAdministratorBootstrapStatus.Created, result.Status);
        var user = Assert.Single(store.Users);
        Assert.Equal("HASHED_PASSWORD", user.PasswordHash);
        Assert.Contains(user.UserRoles, userRole =>
            userRole.RoleId == IdentityCatalog.RoleIds.AdministratorId);
        Assert.True(store.TransactionCommitted);
    }

    [Fact]
    public async Task BootstrapAsync_RollsBack_WhenTransactionFails()
    {
        var store = new FakeInitialAdministratorStore
        {
            ThrowDuringTransaction = true
        };
        var hasher = new FakePasswordHasher();
        var bootstrapper = CreateBootstrapper(store, hasher);

        var result = await bootstrapper.BootstrapAsync();

        Assert.Equal(InitialAdministratorBootstrapStatus.Failed, result.Status);
        Assert.True(store.TransactionRolledBack);
        Assert.False(store.TransactionCommitted);
        Assert.Empty(store.Users);
    }

    private static InitialAdministratorBootstrapper CreateBootstrapper(
        FakeInitialAdministratorStore store,
        FakePasswordHasher hasher,
        string? firstName = "Initial",
        string? lastName = "Admin",
        string? email = "admin@institucion.edu.ar",
        string? password = "ValidPassword1!")
    {
        return new InitialAdministratorBootstrapper(
            store,
            hasher,
            Options.Create(new InitialAdministratorOptions
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Password = password
            }),
            NullLogger<InitialAdministratorBootstrapper>.Instance);
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public int HashCallCount { get; private set; }

        public string Hash(string password)
        {
            HashCallCount++;
            return "HASHED_PASSWORD";
        }

        public PasswordVerificationResult Verify(string password, string passwordHash)
        {
            return PasswordVerificationResult.Success;
        }
    }

    private sealed class FakeInitialAdministratorStore : IInitialAdministratorStore
    {
        public bool AdministratorRoleExists { get; init; } = true;
        public bool AnyAdministratorExists { get; init; }
        public bool UserWithNormalizedEmailExists { get; init; }
        public bool ThrowDuringTransaction { get; init; }
        public bool TransactionCommitted { get; private set; }
        public bool TransactionRolledBack { get; private set; }
        public int AccessCount { get; private set; }
        public string ExistingAdministratorPasswordHash { get; set; } = string.Empty;
        public List<User> Users { get; } = [];

        public Task<bool> AdministratorRoleExistsAsync(CancellationToken cancellationToken)
        {
            AccessCount++;
            return Task.FromResult(AdministratorRoleExists);
        }

        public Task<bool> AnyAdministratorExistsAsync(CancellationToken cancellationToken)
        {
            AccessCount++;
            return Task.FromResult(AnyAdministratorExists);
        }

        public Task<bool> UserExistsWithNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken)
        {
            AccessCount++;
            return Task.FromResult(UserWithNormalizedEmailExists);
        }

        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken)
        {
            AccessCount++;
            var existingUsers = Users.ToArray();

            try
            {
                await operation(cancellationToken);

                if (ThrowDuringTransaction)
                {
                    throw new InvalidOperationException("Simulated persistence failure.");
                }

                TransactionCommitted = true;
            }
            catch
            {
                Users.Clear();
                Users.AddRange(existingUsers);
                TransactionRolledBack = true;
                throw;
            }
        }

        public Task AddUserAsync(User user, CancellationToken cancellationToken)
        {
            AccessCount++;
            Users.Add(user);
            return Task.CompletedTask;
        }
    }
}
