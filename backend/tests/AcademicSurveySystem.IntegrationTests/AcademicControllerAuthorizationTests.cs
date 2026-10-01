using AcademicSurveySystem.Api.Authorization;
using AcademicSurveySystem.Api.Controllers.Academic;
using Microsoft.AspNetCore.Mvc;

namespace AcademicSurveySystem.IntegrationTests;

public sealed class AcademicControllerAuthorizationTests
{
    [Theory]
    [InlineData(typeof(AcademicUnitsController), nameof(AcademicUnitsController.GetAll), "academic.catalog.read")]
    [InlineData(typeof(AcademicUnitsController), nameof(AcademicUnitsController.GetById), "academic.catalog.read")]
    [InlineData(typeof(CareersController), nameof(CareersController.GetAll), "academic.catalog.read")]
    [InlineData(typeof(CareersController), nameof(CareersController.GetById), "academic.catalog.read")]
    [InlineData(typeof(CareersController), nameof(CareersController.GetTeachers), "academic.catalog.read")]
    [InlineData(typeof(AcademicCyclesController), nameof(AcademicCyclesController.GetAll), "academic.catalog.read")]
    [InlineData(typeof(AcademicCyclesController), nameof(AcademicCyclesController.GetById), "academic.catalog.read")]
    [InlineData(typeof(SubjectsController), nameof(SubjectsController.GetAll), "academic.catalog.read")]
    [InlineData(typeof(SubjectsController), nameof(SubjectsController.GetById), "academic.catalog.read")]
    [InlineData(typeof(TeachersController), nameof(TeachersController.GetAll), "academic.catalog.read")]
    [InlineData(typeof(TeachersController), nameof(TeachersController.GetById), "academic.catalog.read")]
    [InlineData(
        typeof(TeacherSubjectAssignmentsController),
        nameof(TeacherSubjectAssignmentsController.GetAll),
        "academic.catalog.read")]
    [InlineData(
        typeof(TeacherSubjectAssignmentsController),
        nameof(TeacherSubjectAssignmentsController.GetById),
        "academic.catalog.read")]
    public void ReadActions_RequireAcademicCatalogReadPermission(
        Type controllerType,
        string actionName,
        string expectedPermission)
    {
        AssertActionRequiresPermission(controllerType, actionName, expectedPermission);
    }

    [Theory]
    [InlineData(typeof(AcademicUnitsController), nameof(AcademicUnitsController.Create), "academic.catalog.manage")]
    [InlineData(typeof(AcademicUnitsController), nameof(AcademicUnitsController.Update), "academic.catalog.manage")]
    [InlineData(typeof(AcademicUnitsController), nameof(AcademicUnitsController.Activate), "academic.catalog.manage")]
    [InlineData(typeof(AcademicUnitsController), nameof(AcademicUnitsController.Deactivate), "academic.catalog.manage")]
    [InlineData(typeof(CareersController), nameof(CareersController.Create), "academic.catalog.manage")]
    [InlineData(typeof(CareersController), nameof(CareersController.Update), "academic.catalog.manage")]
    [InlineData(typeof(CareersController), nameof(CareersController.Activate), "academic.catalog.manage")]
    [InlineData(typeof(CareersController), nameof(CareersController.Deactivate), "academic.catalog.manage")]
    [InlineData(typeof(AcademicCyclesController), nameof(AcademicCyclesController.Create), "academic.catalog.manage")]
    [InlineData(typeof(AcademicCyclesController), nameof(AcademicCyclesController.Update), "academic.catalog.manage")]
    [InlineData(typeof(AcademicCyclesController), nameof(AcademicCyclesController.Activate), "academic.catalog.manage")]
    [InlineData(typeof(AcademicCyclesController), nameof(AcademicCyclesController.Deactivate), "academic.catalog.manage")]
    [InlineData(typeof(SubjectsController), nameof(SubjectsController.Create), "academic.catalog.manage")]
    [InlineData(typeof(SubjectsController), nameof(SubjectsController.Update), "academic.catalog.manage")]
    [InlineData(typeof(SubjectsController), nameof(SubjectsController.Activate), "academic.catalog.manage")]
    [InlineData(typeof(SubjectsController), nameof(SubjectsController.Deactivate), "academic.catalog.manage")]
    [InlineData(typeof(TeachersController), nameof(TeachersController.Create), "academic.catalog.manage")]
    [InlineData(typeof(TeachersController), nameof(TeachersController.Update), "academic.catalog.manage")]
    [InlineData(typeof(TeachersController), nameof(TeachersController.Activate), "academic.catalog.manage")]
    [InlineData(typeof(TeachersController), nameof(TeachersController.Deactivate), "academic.catalog.manage")]
    [InlineData(
        typeof(TeacherSubjectAssignmentsController),
        nameof(TeacherSubjectAssignmentsController.Create),
        "academic.catalog.manage")]
    [InlineData(
        typeof(TeacherSubjectAssignmentsController),
        nameof(TeacherSubjectAssignmentsController.Update),
        "academic.catalog.manage")]
    [InlineData(
        typeof(TeacherSubjectAssignmentsController),
        nameof(TeacherSubjectAssignmentsController.Activate),
        "academic.catalog.manage")]
    [InlineData(
        typeof(TeacherSubjectAssignmentsController),
        nameof(TeacherSubjectAssignmentsController.Deactivate),
        "academic.catalog.manage")]
    public void WriteActions_RequireAcademicCatalogManagePermission(
        Type controllerType,
        string actionName,
        string expectedPermission)
    {
        AssertActionRequiresPermission(controllerType, actionName, expectedPermission);
    }

    [Fact]
    public void AcademicControllers_HaveExpectedRoutes()
    {
        Assert.Equal(
            "api/academic/academic-units",
            typeof(AcademicUnitsController).GetCustomAttributes(typeof(RouteAttribute), inherit: false)
                .Cast<RouteAttribute>()
                .Single()
                .Template);

        Assert.Equal(
            "api/academic/careers",
            typeof(CareersController).GetCustomAttributes(typeof(RouteAttribute), inherit: false)
                .Cast<RouteAttribute>()
                .Single()
                .Template);

        Assert.Equal(
            "api/academic/academic-cycles",
            typeof(AcademicCyclesController).GetCustomAttributes(typeof(RouteAttribute), inherit: false)
                .Cast<RouteAttribute>()
                .Single()
                .Template);

        Assert.Equal(
            "api/academic/subjects",
            typeof(SubjectsController).GetCustomAttributes(typeof(RouteAttribute), inherit: false)
                .Cast<RouteAttribute>()
                .Single()
                .Template);

        Assert.Equal(
            "api/academic/teachers",
            typeof(TeachersController).GetCustomAttributes(typeof(RouteAttribute), inherit: false)
                .Cast<RouteAttribute>()
                .Single()
                .Template);

        Assert.Equal(
            "api/academic/teacher-subject-assignments",
            typeof(TeacherSubjectAssignmentsController)
                .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
                .Cast<RouteAttribute>()
                .Single()
                .Template);
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
