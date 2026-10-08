namespace AcademicSurveySystem.Application.Surveys.Results;

public sealed record SurveyHistoryQuery(
    Guid CareerId,
    Guid SubjectId,
    Guid TeacherId,
    Guid SurveyVersionGroupId);
