namespace AcademicSurveySystem.Application.Surveys.Results;

public enum ResultsAccessDecisionStatus
{
    Allowed = 1,
    Forbidden = 2,
    SurveyAssignmentNotFound = 3,
    SurveySessionNotFound = 4
}

public sealed record ResultsAccessDecision(ResultsAccessDecisionStatus Status)
{
    public static ResultsAccessDecision Allowed() =>
        new(ResultsAccessDecisionStatus.Allowed);

    public static ResultsAccessDecision Forbidden() =>
        new(ResultsAccessDecisionStatus.Forbidden);

    public static ResultsAccessDecision SurveyAssignmentNotFound() =>
        new(ResultsAccessDecisionStatus.SurveyAssignmentNotFound);

    public static ResultsAccessDecision SurveySessionNotFound() =>
        new(ResultsAccessDecisionStatus.SurveySessionNotFound);
}
