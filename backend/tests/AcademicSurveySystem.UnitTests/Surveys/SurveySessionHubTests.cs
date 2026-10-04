using AcademicSurveySystem.Api.Authorization;
using AcademicSurveySystem.Api.Hubs;
using AcademicSurveySystem.Application.Surveys.Responses;

namespace AcademicSurveySystem.UnitTests.Surveys;

public sealed class SurveySessionHubTests
{
    [Fact]
    public void SurveySessionHub_RequiresSessionManagementPermission()
    {
        var attribute = Assert.Single(
            typeof(SurveySessionHub).GetCustomAttributes(typeof(RequirePermissionAttribute), inherit: false)
                .Cast<RequirePermissionAttribute>());

        Assert.Equal(
            PermissionAuthorizationExtensions.GetPolicyName("surveys.sessions.manage"),
            attribute.Policy);
    }

    [Fact]
    public void SurveySessionHub_UsesAssignmentGroupName()
    {
        var assignmentId = Guid.Parse("a4c3ec56-e3ca-4980-991d-30413e5027f7");

        var groupName = SurveySessionHub.BuildAssignmentGroupName(assignmentId);

        Assert.Equal("assignment:a4c3ec56-e3ca-4980-991d-30413e5027f7", groupName);
    }

    [Fact]
    public void SurveyResponseProgressDto_DoesNotExposeAnswerOrStudentData()
    {
        var propertyNames = typeof(SurveyResponseProgressDto)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.Equal(
            [
                nameof(SurveyResponseProgressDto.SurveyAssignmentId),
                nameof(SurveyResponseProgressDto.ExpectedRespondentCount),
                nameof(SurveyResponseProgressDto.ResponseCount),
                nameof(SurveyResponseProgressDto.RemainingCount),
                nameof(SurveyResponseProgressDto.ParticipationPercentage)
            ],
            propertyNames);
        Assert.DoesNotContain(propertyNames, name => name.Contains("Answer", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, name => name.Contains("Student", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, name => name.Contains("ResponseId", StringComparison.OrdinalIgnoreCase));
    }
}
