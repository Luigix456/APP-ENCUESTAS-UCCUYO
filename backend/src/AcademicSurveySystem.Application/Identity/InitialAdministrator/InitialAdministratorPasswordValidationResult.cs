namespace AcademicSurveySystem.Application.Identity.InitialAdministrator;

public sealed record InitialAdministratorPasswordValidationResult(
    bool IsValid,
    IReadOnlyCollection<string> Errors)
{
    public static InitialAdministratorPasswordValidationResult Valid() =>
        new(true, Array.Empty<string>());

    public static InitialAdministratorPasswordValidationResult Invalid(
        IReadOnlyCollection<string> errors) =>
        new(false, errors);
}
