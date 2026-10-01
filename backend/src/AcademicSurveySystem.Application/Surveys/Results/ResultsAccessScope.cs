namespace AcademicSurveySystem.Application.Surveys.Results;

public enum ResultsAccessScopeType
{
    Forbidden = 0,
    All = 1,
    Career = 2
}

public sealed record ResultsAccessScope(
    ResultsAccessScopeType Type,
    IReadOnlyCollection<Guid> CareerIds)
{
    public bool IsAllowed => Type != ResultsAccessScopeType.Forbidden;

    public static ResultsAccessScope Forbidden() =>
        new(ResultsAccessScopeType.Forbidden, []);

    public static ResultsAccessScope All() =>
        new(ResultsAccessScopeType.All, []);

    public static ResultsAccessScope Career(IReadOnlyCollection<Guid> careerIds) =>
        new(ResultsAccessScopeType.Career, careerIds);
}
