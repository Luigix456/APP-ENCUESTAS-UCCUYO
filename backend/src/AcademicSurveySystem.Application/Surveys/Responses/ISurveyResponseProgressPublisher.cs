namespace AcademicSurveySystem.Application.Surveys.Responses;

public interface ISurveyResponseProgressPublisher
{
    Task PublishAsync(
        SurveyResponseProgressDto progress,
        CancellationToken cancellationToken = default);
}
