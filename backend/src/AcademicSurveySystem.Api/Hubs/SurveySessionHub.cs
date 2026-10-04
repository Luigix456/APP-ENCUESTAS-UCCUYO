using AcademicSurveySystem.Api.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AcademicSurveySystem.Api.Hubs;

[RequirePermission("surveys.sessions.manage")]
public sealed class SurveySessionHub : Hub
{
    public const string Route = "/hubs/survey-sessions";
    public const string ResponseProgressUpdatedEvent = "ResponseProgressUpdated";

    public Task JoinAssignmentGroup(Guid surveyAssignmentId)
    {
        if (surveyAssignmentId == Guid.Empty)
        {
            throw new HubException("SurveyAssignmentId is required.");
        }

        return Groups.AddToGroupAsync(
            Context.ConnectionId,
            BuildAssignmentGroupName(surveyAssignmentId));
    }

    public Task LeaveAssignmentGroup(Guid surveyAssignmentId)
    {
        if (surveyAssignmentId == Guid.Empty)
        {
            throw new HubException("SurveyAssignmentId is required.");
        }

        return Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            BuildAssignmentGroupName(surveyAssignmentId));
    }

    public static string BuildAssignmentGroupName(Guid surveyAssignmentId) =>
        $"assignment:{surveyAssignmentId:D}";
}
