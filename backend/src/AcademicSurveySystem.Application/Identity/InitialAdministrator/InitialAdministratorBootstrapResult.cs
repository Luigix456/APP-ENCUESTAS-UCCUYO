namespace AcademicSurveySystem.Application.Identity.InitialAdministrator;

public sealed record InitialAdministratorBootstrapResult(
    InitialAdministratorBootstrapStatus Status,
    string Message)
{
    public bool Succeeded =>
        Status is InitialAdministratorBootstrapStatus.Created
            or InitialAdministratorBootstrapStatus.AlreadyExists;

    public static InitialAdministratorBootstrapResult Created() =>
        new(InitialAdministratorBootstrapStatus.Created, "Initial administrator was created.");

    public static InitialAdministratorBootstrapResult AlreadyExists() =>
        new(InitialAdministratorBootstrapStatus.AlreadyExists, "An administrator already exists.");

    public static InitialAdministratorBootstrapResult Failed(string message) =>
        new(InitialAdministratorBootstrapStatus.Failed, message);
}
