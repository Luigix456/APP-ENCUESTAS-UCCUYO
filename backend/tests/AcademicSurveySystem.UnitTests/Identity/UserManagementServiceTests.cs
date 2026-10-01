using System.IdentityModel.Tokens.Jwt;
using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Common.Security;
using AcademicSurveySystem.Application.Identity.Authentication;
using AcademicSurveySystem.Application.Identity.UserManagement;
using AcademicSurveySystem.Domain.Identity;
using AcademicSurveySystem.Infrastructure.Authentication;
using AcademicSurveySystem.Infrastructure.Identity;
using AcademicSurveySystem.Infrastructure.Identity.Authentication;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace AcademicSurveySystem.UnitTests.Identity;

public sealed class UserManagementServiceTests
{
    private const string PlainPassword = "ValidPassword1!";
    private const string PasswordHash = "HASHED_PASSWORD";

    [Fact]
    public async Task CreateUserAsync_WithValidRequest_CreatesActiveUserWithHashedPassword()
    {
        using var context = CreateContext();
        var hasher = new FakePasswordHasher();
        var service = CreateService(context, hasher);

        var result = await service.CreateUserAsync(
            CreateRequest(email: " Director.TUDS@Institucion.edu.ar "),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal("director.tuds@institucion.edu.ar", result.Value.Email);
        Assert.Equal("Active", result.Value.Status);
        Assert.Single(result.Value.Roles);
        Assert.Equal("career_director", result.Value.Roles.Single().Code);
        Assert.Equal(1, hasher.HashCallCount);
        Assert.DoesNotContain(
            typeof(UserDto).GetProperties(),
            property => property.Name.Contains("Password", StringComparison.OrdinalIgnoreCase));

        var user = await context.Users.SingleAsync(item =>
            item.NormalizedEmail == "DIRECTOR.TUDS@INSTITUCION.EDU.AR");

        Assert.Equal(PasswordHash, user.PasswordHash);
        Assert.NotEqual(PlainPassword, user.PasswordHash);
        Assert.Equal("director.tuds@institucion.edu.ar", user.Email);
    }

    [Fact]
    public async Task CreateUserAsync_WithDuplicateEmail_ReturnsConflict()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var first = await service.CreateUserAsync(
            CreateRequest(email: "director.tuds@institucion.edu.ar"),
            CancellationToken.None);
        var duplicate = await service.CreateUserAsync(
            CreateRequest(email: " DIRECTOR.TUDS@INSTITUCION.EDU.AR "),
            CancellationToken.None);

        Assert.True(first.Succeeded);
        Assert.Equal(ApplicationResultStatus.Conflict, duplicate.Status);
    }

    [Fact]
    public async Task CreateUserAsync_WithInvalidPassword_ReturnsValidationWithoutHashing()
    {
        using var context = CreateContext();
        var hasher = new FakePasswordHasher();
        var service = CreateService(context, hasher);

        var result = await service.CreateUserAsync(
            CreateRequest(password: "short"),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Validation, result.Status);
        Assert.Equal(0, hasher.HashCallCount);
        Assert.Empty(context.Users);
    }

    [Fact]
    public async Task GetUsersAndGetUserById_ReturnExpectedDtos()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var created = await service.CreateUserAsync(CreateRequest(), CancellationToken.None);

        var users = await service.GetUsersAsync(includeInactive: false, CancellationToken.None);
        var user = await service.GetUserByIdAsync(created.Value!.Id, CancellationToken.None);
        var missing = await service.GetUserByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(users.Succeeded);
        Assert.Single(users.Value!);
        Assert.True(user.Succeeded);
        Assert.Equal(created.Value.Id, user.Value!.Id);
        Assert.Equal(ApplicationResultStatus.NotFound, missing.Status);
    }

    [Fact]
    public async Task UpdateUserAsync_UpdatesNameAndEmail()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var created = await service.CreateUserAsync(CreateRequest(), CancellationToken.None);

        var result = await service.UpdateUserAsync(
            created.Value!.Id,
            new UpdateUserRequest("Maria", "Gomez", "maria.gomez@institucion.edu.ar"),
            CancellationToken.None);

        Assert.True(result.Succeeded);

        var user = await context.Users.SingleAsync();
        Assert.Equal("Maria", user.FirstName);
        Assert.Equal("Gomez", user.LastName);
        Assert.Equal("maria.gomez@institucion.edu.ar", user.Email);
        Assert.Equal("MARIA.GOMEZ@INSTITUCION.EDU.AR", user.NormalizedEmail);
    }

    [Fact]
    public async Task ActivateAndDeactivateUser_UpdateStatusAndInactiveUserCannotLogin()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var created = await service.CreateUserAsync(CreateRequest(), CancellationToken.None);

        var deactivate = await service.DeactivateUserAsync(created.Value!.Id, Guid.NewGuid(), CancellationToken.None);
        var inactiveLogin = await CreateAuthenticationService(context).LoginAsync(
            new LoginRequest(created.Value.Email, PlainPassword),
            CancellationToken.None);
        var activate = await service.ActivateUserAsync(created.Value.Id, CancellationToken.None);

        Assert.True(deactivate.Succeeded);
        Assert.Equal(AuthenticationFailureReason.UserInactive, inactiveLogin.FailureReason);
        Assert.True(activate.Succeeded);
        Assert.Equal("Active", (await service.GetUserByIdAsync(created.Value.Id, CancellationToken.None)).Value!.Status);
    }

    [Fact]
    public async Task DeactivateUserAsync_WithCurrentUser_ReturnsConflictAndKeepsUserActive()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var created = await service.CreateUserAsync(CreateRequest(), CancellationToken.None);

        var result = await service.DeactivateUserAsync(
            created.Value!.Id,
            created.Value.Id,
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Conflict, result.Status);
        Assert.Equal("Active", (await service.GetUserByIdAsync(created.Value.Id, CancellationToken.None)).Value!.Status);
    }

    [Fact]
    public async Task DeactivateUserAsync_WithLastActiveAdministrator_ReturnsConflict()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var created = await service.CreateUserAsync(CreateRequest(), CancellationToken.None);
        var makeAdministrator = await service.ReplaceUserRolesAsync(
            created.Value!.Id,
            new UpdateUserRolesRequest([IdentityCatalog.RoleIds.AdministratorId]),
            CancellationToken.None);

        var result = await service.DeactivateUserAsync(
            created.Value.Id,
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(makeAdministrator.Succeeded);
        Assert.Equal(ApplicationResultStatus.Conflict, result.Status);
        Assert.Equal("Active", (await service.GetUserByIdAsync(created.Value.Id, CancellationToken.None)).Value!.Status);
    }

    [Fact]
    public async Task GetRolesAsync_ReturnsSeededRolesWithPermissions()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetRolesAsync(CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Contains(result.Value!, role =>
            role.Code == "career_director"
            && role.Permissions.Contains("results.read_career"));
    }

    [Fact]
    public async Task ReplaceUserRolesAsync_ReplacesRolesAtomically()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var created = await service.CreateUserAsync(CreateRequest(), CancellationToken.None);
        var deanRole = IdentityCatalog.GetRole("dean");

        var replace = await service.ReplaceUserRolesAsync(
            created.Value!.Id,
            new UpdateUserRolesRequest([deanRole.Id]),
            CancellationToken.None);
        var missingRole = await service.ReplaceUserRolesAsync(
            created.Value.Id,
            new UpdateUserRolesRequest([Guid.NewGuid()]),
            CancellationToken.None);
        var emptyRoles = await service.ReplaceUserRolesAsync(
            created.Value.Id,
            new UpdateUserRolesRequest([]),
            CancellationToken.None);

        Assert.True(replace.Succeeded);
        Assert.Single(replace.Value!.Roles);
        Assert.Equal("dean", replace.Value.Roles.Single().Code);
        Assert.Equal(ApplicationResultStatus.NotFound, missingRole.Status);
        Assert.True(emptyRoles.Succeeded);
        Assert.Empty(emptyRoles.Value!.Roles);
    }

    [Fact]
    public async Task ReplaceUserRolesAsync_DoesNotRemoveLastActiveAdministrator()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var created = await service.CreateUserAsync(CreateRequest(), CancellationToken.None);
        var makeAdministrator = await service.ReplaceUserRolesAsync(
            created.Value!.Id,
            new UpdateUserRolesRequest([IdentityCatalog.RoleIds.AdministratorId]),
            CancellationToken.None);

        var result = await service.ReplaceUserRolesAsync(
            created.Value.Id,
            new UpdateUserRolesRequest([]),
            CancellationToken.None);

        Assert.True(makeAdministrator.Succeeded);
        Assert.Equal(ApplicationResultStatus.Conflict, result.Status);
        Assert.Contains(
            context.UserRoles,
            userRole => userRole.UserId == created.Value.Id
                && userRole.RoleId == IdentityCatalog.RoleIds.AdministratorId);
    }

    [Fact]
    public async Task CareerDirectorLogin_ReturnsReadCareerPermissionAndUserIdClaims()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var created = await service.CreateUserAsync(CreateRequest(), CancellationToken.None);

        var login = await CreateAuthenticationService(context).LoginAsync(
            new LoginRequest(created.Value!.Email, PlainPassword),
            CancellationToken.None);

        Assert.True(login.Succeeded);
        Assert.Contains("career_director", login.Response!.User.Roles);
        Assert.Contains("results.read_career", login.Response.User.Permissions);
        Assert.DoesNotContain("results.read_all", login.Response.User.Permissions);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(login.Response.AccessToken);
        Assert.Contains(token.Claims, claim =>
            claim.Type == JwtRegisteredClaimNames.Sub && claim.Value == created.Value.Id.ToString());
        Assert.Contains(token.Claims, claim =>
            claim.Type == JwtTokenGenerator.NameIdentifierClaimType
            && claim.Value == created.Value.Id.ToString());
    }

    private static CreateUserRequest CreateRequest(
        string email = "director.tuds@institucion.edu.ar",
        string password = PlainPassword)
    {
        return new CreateUserRequest(
            "Maria",
            "Gomez",
            email,
            password,
            [new Guid("44444444-4444-4444-4444-444444444444")]);
    }

    private static UserManagementService CreateService(
        ApplicationDbContext context,
        FakePasswordHasher? hasher = null)
    {
        return new UserManagementService(context, hasher ?? new FakePasswordHasher());
    }

    private static AuthenticationService CreateAuthenticationService(ApplicationDbContext context)
    {
        return new AuthenticationService(
            new EfAuthenticationUserStore(context),
            new FakePasswordHasher(),
            new JwtTokenGenerator(Options.Create(new JwtOptions
            {
                Issuer = "AcademicSurveySystem.Tests",
                Audience = "AcademicSurveySystem.Tests",
                SigningKey = "TEST_SIGNING_KEY_WITH_AT_LEAST_32_CHARS",
                AccessTokenExpirationMinutes = 60
            })));
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

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public int HashCallCount { get; private set; }

        public string Hash(string password)
        {
            HashCallCount++;
            return PasswordHash;
        }

        public PasswordVerificationResult Verify(string password, string passwordHash)
        {
            return password == PlainPassword && passwordHash == PasswordHash
                ? PasswordVerificationResult.Success
                : PasswordVerificationResult.Failed;
        }
    }
}
