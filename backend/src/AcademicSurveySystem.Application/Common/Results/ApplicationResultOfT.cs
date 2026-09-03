namespace AcademicSurveySystem.Application.Common.Results;

public sealed record ApplicationResult<T>(
    ApplicationResultStatus Status,
    T? Value,
    IReadOnlyCollection<ApplicationError> Errors)
    : ApplicationResult(Status, Errors)
{
    public static ApplicationResult<T> Success(T value) =>
        new(ApplicationResultStatus.Success, value, Array.Empty<ApplicationError>());

    public static new ApplicationResult<T> Validation(IReadOnlyCollection<ApplicationError> errors) =>
        new(ApplicationResultStatus.Validation, default, errors);

    public static new ApplicationResult<T> NotFound(string message) =>
        new(ApplicationResultStatus.NotFound, default, [new ApplicationError("NotFound", message)]);

    public static new ApplicationResult<T> Conflict(string message) =>
        new(ApplicationResultStatus.Conflict, default, [new ApplicationError("Conflict", message)]);

    public static new ApplicationResult<T> Failure(string message) =>
        new(ApplicationResultStatus.Failure, default, [new ApplicationError("Failure", message)]);
}
