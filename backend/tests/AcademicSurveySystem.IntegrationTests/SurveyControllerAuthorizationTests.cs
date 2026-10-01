using AcademicSurveySystem.Api.Authorization;
using AcademicSurveySystem.Api.Controllers;
using AcademicSurveySystem.Api.Controllers.Public;
using AcademicSurveySystem.Api.Controllers.Surveys;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicSurveySystem.IntegrationTests;

public sealed class SurveyControllerAuthorizationTests
{
    [Theory]
    [InlineData(nameof(SurveysController.GetAll))]
    [InlineData(nameof(SurveysController.GetById))]
    public void ReadActions_RequireSurveyTemplateReadPermission(string actionName)
    {
        AssertActionRequiresPermission(
            typeof(SurveysController),
            actionName,
            "surveys.templates.read");
    }

    [Theory]
    [InlineData(nameof(IdentityUsersController.GetAll))]
    [InlineData(nameof(IdentityUsersController.GetById))]
    [InlineData(nameof(IdentityUsersController.GetCareers))]
    public void ReadUserActions_RequireIdentityUsersReadPermission(string actionName)
    {
        AssertActionRequiresPermission(
            typeof(IdentityUsersController),
            actionName,
            "identity.users.read");
    }

    [Fact]
    public void CreateUserAction_RequiresIdentityUsersCreatePermission()
    {
        AssertActionRequiresPermission(
            typeof(IdentityUsersController),
            nameof(IdentityUsersController.Create),
            "identity.users.create");
    }

    [Theory]
    [InlineData(nameof(IdentityUsersController.Update))]
    [InlineData(nameof(IdentityUsersController.Activate))]
    [InlineData(nameof(IdentityUsersController.Deactivate))]
    [InlineData(nameof(IdentityUsersController.Delete))]
    [InlineData(nameof(IdentityUsersController.ResetPassword))]
    public void UpdateUserActions_RequireIdentityUsersUpdatePermission(string actionName)
    {
        AssertActionRequiresPermission(
            typeof(IdentityUsersController),
            actionName,
            "identity.users.update");
    }

    [Fact]
    public void ReplaceUserRolesAction_RequiresIdentityUsersAssignRolesPermission()
    {
        AssertActionRequiresPermission(
            typeof(IdentityUsersController),
            nameof(IdentityUsersController.ReplaceRoles),
            "identity.users.assign_roles");
    }

    [Fact]
    public void GetRolesAction_RequiresIdentityRolesReadPermission()
    {
        AssertActionRequiresPermission(
            typeof(IdentityRolesController),
            nameof(IdentityRolesController.GetAll),
            "identity.roles.read");
    }

    [Theory]
    [InlineData(nameof(SurveyAssignmentsController.GetAll))]
    [InlineData(nameof(SurveyAssignmentsController.GetById))]
    public void SurveyAssignmentReadActions_RequireSurveyTemplateReadPermission(string actionName)
    {
        AssertActionRequiresPermission(
            typeof(SurveyAssignmentsController),
            actionName,
            "surveys.templates.read");
    }

    [Theory]
    [InlineData(nameof(IdentityUsersController.ReplaceCareers))]
    [InlineData(nameof(SurveysController.Create))]
    [InlineData(nameof(SurveysController.GetOrCreateEditableVersion))]
    [InlineData(nameof(SurveysController.Update))]
    [InlineData(nameof(SurveysController.Publish))]
    [InlineData(nameof(SurveysController.Archive))]
    [InlineData(nameof(SurveysController.Activate))]
    [InlineData(nameof(SurveysController.Deactivate))]
    [InlineData(nameof(SurveysController.AddSection))]
    [InlineData(nameof(SurveysController.UpdateSection))]
    [InlineData(nameof(SurveysController.ActivateSection))]
    [InlineData(nameof(SurveysController.DeactivateSection))]
    [InlineData(nameof(SurveysController.AddQuestion))]
    [InlineData(nameof(SurveysController.UpdateQuestion))]
    [InlineData(nameof(SurveysController.ActivateQuestion))]
    [InlineData(nameof(SurveysController.DeactivateQuestion))]
    [InlineData(nameof(SurveysController.AddOption))]
    [InlineData(nameof(SurveysController.UpdateOption))]
    [InlineData(nameof(SurveysController.ActivateOption))]
    [InlineData(nameof(SurveysController.DeactivateOption))]
    [InlineData(nameof(SurveysController.AddMatrixRow))]
    [InlineData(nameof(SurveysController.UpdateMatrixRow))]
    [InlineData(nameof(SurveysController.ActivateMatrixRow))]
    [InlineData(nameof(SurveysController.DeactivateMatrixRow))]
    public void WriteActions_RequireSurveyTemplateManagePermission(string actionName)
    {
        if (actionName == nameof(IdentityUsersController.ReplaceCareers))
        {
            AssertActionRequiresPermission(
                typeof(IdentityUsersController),
                actionName,
                "identity.users.update");
            return;
        }

        AssertActionRequiresPermission(
            typeof(SurveysController),
            actionName,
            "surveys.templates.manage");
    }

    [Theory]
    [InlineData(nameof(SurveyAssignmentsController.Create))]
    [InlineData(nameof(SurveyAssignmentsController.Activate))]
    [InlineData(nameof(SurveyAssignmentsController.Deactivate))]
    public void SurveyAssignmentWriteActions_RequireSurveyTemplateManagePermission(string actionName)
    {
        AssertActionRequiresPermission(
            typeof(SurveyAssignmentsController),
            actionName,
            "surveys.templates.manage");
    }

    [Theory]
    [InlineData(nameof(SurveySessionsController.GetAll))]
    [InlineData(nameof(SurveySessionsController.GetById))]
    [InlineData(nameof(SurveySessionsController.Create))]
    [InlineData(nameof(SurveySessionsController.Update))]
    [InlineData(nameof(SurveySessionsController.Open))]
    [InlineData(nameof(SurveySessionsController.Close))]
    [InlineData(nameof(SurveySessionsController.Cancel))]
    [InlineData(nameof(SurveySessionsController.Activate))]
    [InlineData(nameof(SurveySessionsController.Deactivate))]
    public void SurveySessionActions_RequireSurveySessionManagePermission(string actionName)
    {
        AssertActionRequiresPermission(
            typeof(SurveySessionsController),
            actionName,
            "surveys.sessions.manage");
    }

    [Fact]
    public void SurveysController_HasExpectedRoute()
    {
        var route = typeof(SurveysController)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Single();

        Assert.Equal("api/surveys", route.Template);
    }

    [Fact]
    public void SurveyAssignmentsController_HasExpectedRoute()
    {
        var route = typeof(SurveyAssignmentsController)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Single();

        Assert.Equal("api/survey-assignments", route.Template);
    }

    [Fact]
    public void SurveySessionsController_HasExpectedRoute()
    {
        var route = typeof(SurveySessionsController)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Single();

        Assert.Equal("api/survey-sessions", route.Template);
    }

    [Fact]
    public void PublicSurveySessionsController_HasExpectedRoute()
    {
        var route = typeof(PublicSurveySessionsController)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Single();

        Assert.Equal("api/public/survey-sessions", route.Template);
    }

    [Fact]
    public void ResultsController_HasExpectedRoute()
    {
        var route = typeof(ResultsController)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Single();

        Assert.Equal("api/results", route.Template);
    }

    [Fact]
    public void IdentityUsersController_HasExpectedRoute()
    {
        var route = typeof(IdentityUsersController)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Single();

        Assert.Equal("api/identity/users", route.Template);
    }

    [Fact]
    public void IdentityRolesController_HasExpectedRoute()
    {
        var route = typeof(IdentityRolesController)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Single();

        Assert.Equal("api/identity/roles", route.Template);
    }

    [Fact]
    public void SurveysController_RequiresAuthorization()
    {
        var authorizeAttribute = typeof(SurveysController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Null(authorizeAttribute.Policy);
    }

    [Fact]
    public void SurveyAssignmentsController_RequiresAuthorization()
    {
        var authorizeAttribute = typeof(SurveyAssignmentsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Null(authorizeAttribute.Policy);
    }

    [Fact]
    public void SurveySessionsController_RequiresAuthorization()
    {
        var authorizeAttribute = typeof(SurveySessionsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Null(authorizeAttribute.Policy);
    }

    [Fact]
    public void PublicSurveySessionsController_DoesNotRequireAuthorization()
    {
        var authorizeAttributes = typeof(PublicSurveySessionsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>();

        var allowAnonymousAttribute = typeof(PublicSurveySessionsController)
            .GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true)
            .Cast<AllowAnonymousAttribute>()
            .SingleOrDefault();

        Assert.Empty(authorizeAttributes);
        Assert.NotNull(allowAnonymousAttribute);
    }

    [Fact]
    public void ResultsController_RequiresAuthorization()
    {
        var authorizeAttribute = typeof(ResultsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Null(authorizeAttribute.Policy);
    }

    [Fact]
    public void IdentityUsersController_RequiresAuthorization()
    {
        var authorizeAttribute = typeof(IdentityUsersController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Null(authorizeAttribute.Policy);
    }

    [Fact]
    public void IdentityRolesController_RequiresAuthorization()
    {
        var authorizeAttribute = typeof(IdentityRolesController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Null(authorizeAttribute.Policy);
    }

    private static void AssertActionRequiresPermission(
        Type controllerType,
        string actionName,
        string expectedPermission)
    {
        var method = controllerType.GetMethods()
            .Single(methodInfo => methodInfo.Name == actionName);

        var attribute = method
            .GetCustomAttributes(typeof(RequirePermissionAttribute), inherit: false)
            .Cast<RequirePermissionAttribute>()
            .Single();

        Assert.Equal($"Permission:{expectedPermission}", attribute.Policy);
    }
}
