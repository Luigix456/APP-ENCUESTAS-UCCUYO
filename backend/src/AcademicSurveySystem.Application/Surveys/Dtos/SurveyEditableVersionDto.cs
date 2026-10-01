namespace AcademicSurveySystem.Application.Surveys.Dtos;

public sealed record SurveyEditableVersionDto(
    SurveyDetailDto Survey,
    bool CreatedNewVersion,
    Guid SourceSurveyId,
    Guid VersionGroupId,
    int VersionNumber);
