namespace AcademicSurveySystem.Application.Surveys.Responses;

public sealed record SurveyResponseSubmissionDto(
    Guid ResponseId,
    DateTimeOffset SubmittedAtUtc);
