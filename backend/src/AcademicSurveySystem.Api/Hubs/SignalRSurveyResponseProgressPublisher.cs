using AcademicSurveySystem.Application.Surveys.Responses;
using Microsoft.AspNetCore.SignalR;

namespace AcademicSurveySystem.Api.Hubs;

public sealed class SignalRSurveyResponseProgressPublisher : ISurveyResponseProgressPublisher
{
    private readonly IHubContext<SurveySessionHub> _hubContext;

    public SignalRSurveyResponseProgressPublisher(IHubContext<SurveySessionHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PublishAsync(
        SurveyResponseProgressDto progress,
        CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .Group(SurveySessionHub.BuildAssignmentGroupName(progress.SurveyAssignmentId))
            .SendAsync(
                SurveySessionHub.ResponseProgressUpdatedEvent,
                progress,
                cancellationToken);
    }
}
