using System.Security.Claims;
using AcademicSurveySystem.Api.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace AcademicSurveySystem.UnitTests.Authorization;

public sealed class PermissionAuthorizationHandlerTests
{
    [Fact]
    public async Task Handler_Succeeds_WhenPermissionClaimExists()
    {
        var requirement = new PermissionRequirement("identity.users.read");
        var context = new AuthorizationHandlerContext(
            [requirement],
            CreatePrincipal(isAuthenticated: true, "identity.users.read"),
            resource: null);
        var handler = new PermissionAuthorizationHandler();

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_DoesNotSucceed_WhenPermissionClaimIsMissing()
    {
        var requirement = new PermissionRequirement("identity.users.read");
        var context = new AuthorizationHandlerContext(
            [requirement],
            CreatePrincipal(isAuthenticated: true),
            resource: null);
        var handler = new PermissionAuthorizationHandler();

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_DoesNotSucceed_WhenUserIsNotAuthenticated()
    {
        var requirement = new PermissionRequirement("identity.users.read");
        var context = new AuthorizationHandlerContext(
            [requirement],
            CreatePrincipal(isAuthenticated: false, "identity.users.read"),
            resource: null);
        var handler = new PermissionAuthorizationHandler();

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private static ClaimsPrincipal CreatePrincipal(
        bool isAuthenticated,
        string? permission = null)
    {
        var claims = new List<Claim>();

        if (permission is not null)
        {
            claims.Add(new Claim(PermissionAuthorizationHandler.PermissionClaimType, permission));
        }

        var identity = isAuthenticated
            ? new ClaimsIdentity(claims, authenticationType: "Test")
            : new ClaimsIdentity(claims);

        return new ClaimsPrincipal(identity);
    }
}
