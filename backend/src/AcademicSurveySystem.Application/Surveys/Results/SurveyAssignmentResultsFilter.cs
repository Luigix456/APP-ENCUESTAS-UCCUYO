namespace AcademicSurveySystem.Application.Surveys.Results;

public sealed record SurveyAssignmentResultsFilter(
    Guid? SurveyId,
    Guid? CareerId,
    Guid? SubjectId,
    Guid? AcademicCycleId,
    Guid? TeacherId);
