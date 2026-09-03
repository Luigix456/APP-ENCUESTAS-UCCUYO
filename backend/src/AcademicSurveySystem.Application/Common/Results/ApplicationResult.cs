namespace AcademicSurveySystem.Application.Common.Results;

public record ApplicationResult(
    ApplicationResultStatus Status,
    IReadOnlyCollection<ApplicationError> Errors)
{
    public bool Succeeded => Status == ApplicationResultStatus.Success;

    public static ApplicationResult Success() =>
        new(ApplicationResultStatus.Success, Array.Empty<ApplicationError>());

    public static ApplicationResult Validation(IReadOnlyCollection<ApplicationError> errors) =>
        new(ApplicationResultStatus.Validation, errors);

    public static ApplicationResult NotFound(string message) =>
        new(ApplicationResultStatus.NotFound, [new ApplicationError("NotFound", message)]);

    public static ApplicationResult Conflict(string message) =>
        new(ApplicationResultStatus.Conflict, [new ApplicationError("Conflict", message)]);

    public static ApplicationResult Failure(string message) =>
        new(ApplicationResultStatus.Failure, [new ApplicationError("Failure", message)]);
}
