using AcademicSurveySystem.Application.Surveys.Responses;

namespace AcademicSurveySystem.Infrastructure.Surveys;

public sealed class NoOpSurveyResponseProgressPublisher : ISurveyResponseProgressPublisher
{
    public Task PublishAsync(
        SurveyResponseProgressDto progress,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
