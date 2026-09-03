namespace AcademicSurveySystem.Application.Identity.InitialAdministrator;

public interface IInitialAdministratorBootstrapper
{
    Task<InitialAdministratorBootstrapResult> BootstrapAsync(CancellationToken cancellationToken = default);
}
