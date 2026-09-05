using AcademicSurveySystem.Api.Authorization;
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
    [InlineData(nameof(SurveysController.Create))]
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
    [InlineData(nameof(SurveysController.ActivateOption))]
    [InlineData(nameof(SurveysController.DeactivateOption))]
    [InlineData(nameof(SurveysController.AddMatrixRow))]
    [InlineData(nameof(SurveysController.ActivateMatrixRow))]
    [InlineData(nameof(SurveysController.DeactivateMatrixRow))]
    public void WriteActions_RequireSurveyTemplateManagePermission(string actionName)
    {
        AssertActionRequiresPermission(
            typeof(SurveysController),
            actionName,
            "surveys.templates.manage");
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
    public void SurveysController_RequiresAuthorization()
    {
        var authorizeAttribute = typeof(SurveysController)
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
