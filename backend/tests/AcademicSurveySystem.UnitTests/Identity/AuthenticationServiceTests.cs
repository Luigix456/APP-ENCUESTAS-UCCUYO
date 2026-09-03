using AcademicSurveySystem.Application.Common.Authentication;
using AcademicSurveySystem.Application.Common.Security;
using AcademicSurveySystem.Application.Identity.Authentication;
using AcademicSurveySystem.Domain.Identity.Enums;
using AcademicSurveySystem.Infrastructure.Identity.Authentication;

namespace AcademicSurveySystem.UnitTests.Identity;

public sealed class AuthenticationServiceTests
{
    private const string Email = "admin@institucion.edu.ar";
    private const string Password = "ValidPassword1!";

    [Fact]
    public async Task LoginAsync_ReturnsInvalidCredentials_WhenUserDoesNotExist()
    {
        var store = new FakeAuthenticationUserStore();
        var service = CreateService(store);

        var result = await service.LoginAsync(new LoginRequest(Email, Password), CancellationToken.None);

        Assert.Equal(AuthenticationFailureReason.InvalidCredentials, result.FailureReason);
    }

    [Fact]
    public async Task LoginAsync_ReturnsInvalidCredentials_WhenPasswordIsIncorrect()
    {
        var store = new FakeAuthenticationUserStore { User = CreateUser() };
        var hasher = new FakePasswordHasher(PasswordVerificationResult.Failed);
        var service = CreateService(store, hasher);

        var result = await service.LoginAsync(new LoginRequest(Email, Password), CancellationToken.None);

        Assert.Equal(AuthenticationFailureReason.InvalidCredentials, result.FailureReason);
    }

    [Fact]
    public async Task LoginAsync_ReturnsUserInactive_WhenUserIsInactive()
    {
        var store = new FakeAuthenticationUserStore { User = CreateUser(status: UserStatus.Inactive) };
        var service = CreateService(store);

        var result = await service.LoginAsync(new LoginRequest(Email, Password), CancellationToken.None);

        Assert.Equal(AuthenticationFailureReason.UserInactive, result.FailureReason);
    }

    [Fact]
    public async Task LoginAsync_ReturnsUserBlocked_WhenUserIsBlocked()
    {
        var store = new FakeAuthenticationUserStore { User = CreateUser(status: UserStatus.Blocked) };
        var service = CreateService(store);

        var result = await service.LoginAsync(new LoginRequest(Email, Password), CancellationToken.None);

        Assert.Equal(AuthenticationFailureReason.UserBlocked, result.FailureReason);
    }

    [Fact]
    public async Task LoginAsync_ReturnsSuccess_WhenActiveUserAndPasswordAreValid()
    {
        var store = new FakeAuthenticationUserStore { User = CreateUser() };
        var service = CreateService(store);

        var result = await service.LoginAsync(new LoginRequest(Email, Password), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Response);
        Assert.Equal("Bearer", result.Response.TokenType);
    }

    [Fact]
    public async Task LoginAsync_ReturnsRolesAndPermissionsWithoutDuplicates()
    {
        var store = new FakeAuthenticationUserStore
        {
            User = CreateUser(
                roles: ["administrator", "administrator"],
                permissions: ["identity.users.read", "identity.users.read"])
        };
        var service = CreateService(store);

        var result = await service.LoginAsync(new LoginRequest(Email, Password), CancellationToken.None);

        Assert.Equal(["administrator"], result.Response!.User.Roles);
        Assert.Equal(["identity.users.read"], result.Response.User.Permissions);
    }

    [Fact]
    public async Task LoginAsync_UpdatesPasswordHash_WhenRehashIsNeeded()
    {
        var store = new FakeAuthenticationUserStore { User = CreateUser(passwordHash: "OLD_HASH") };
        var hasher = new FakePasswordHasher(
            PasswordVerificationResult.SuccessRehashNeeded,
            hashValue: "NEW_HASH");
        var service = CreateService(store, hasher);

        var result = await service.LoginAsync(new LoginRequest(Email, Password), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("NEW_HASH", store.UpdatedPasswordHash);
    }

    [Fact]
    public async Task LoginAsync_DoesNotExposePasswordHash()
    {
        var store = new FakeAuthenticationUserStore { User = CreateUser(passwordHash: "SECRET_HASH") };
        var service = CreateService(store);

        var result = await service.LoginAsync(new LoginRequest(Email, Password), CancellationToken.None);

        Assert.NotNull(result.Response);
        Assert.DoesNotContain(
            "SECRET_HASH",
            System.Text.Json.JsonSerializer.Serialize(result.Response));
    }

    private static AuthenticationService CreateService(
        FakeAuthenticationUserStore store,
        FakePasswordHasher? passwordHasher = null,
        FakeJwtTokenGenerator? jwtTokenGenerator = null)
    {
        return new AuthenticationService(
            store,
            passwordHasher ?? new FakePasswordHasher(PasswordVerificationResult.Success),
            jwtTokenGenerator ?? new FakeJwtTokenGenerator());
    }

    private static AuthenticationUserSnapshot CreateUser(
        UserStatus status = UserStatus.Active,
        string passwordHash = "HASH",
        IReadOnlyCollection<string>? roles = null,
        IReadOnlyCollection<string>? permissions = null)
    {
        return new AuthenticationUserSnapshot(
            Guid.NewGuid(),
            "Initial",
            "Admin",
            Email,
            passwordHash,
            status,
            roles ?? ["administrator"],
            permissions ?? ["identity.users.read"]);
    }

    private sealed class FakeAuthenticationUserStore : IAuthenticationUserStore
    {
        public AuthenticationUserSnapshot? User { get; init; }
        public string? UpdatedPasswordHash { get; private set; }

        public Task<AuthenticationUserSnapshot?> FindByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(User);
        }

        public Task UpdatePasswordHashAsync(
            Guid userId,
            string passwordHash,
            DateTimeOffset updatedAtUtc,
            CancellationToken cancellationToken)
        {
            UpdatedPasswordHash = passwordHash;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        private readonly PasswordVerificationResult _verificationResult;
        private readonly string _hashValue;

        public FakePasswordHasher(
            PasswordVerificationResult verificationResult,
            string hashValue = "HASH")
        {
            _verificationResult = verificationResult;
            _hashValue = hashValue;
        }

        public string Hash(string password) => _hashValue;

        public PasswordVerificationResult Verify(string password, string passwordHash) =>
            _verificationResult;
    }

    private sealed class FakeJwtTokenGenerator : IJwtTokenGenerator
    {
        public JwtTokenResult GenerateToken(AuthenticatedUser user) =>
            new("TOKEN", "Bearer", DateTimeOffset.UtcNow.AddMinutes(60));
    }
}
