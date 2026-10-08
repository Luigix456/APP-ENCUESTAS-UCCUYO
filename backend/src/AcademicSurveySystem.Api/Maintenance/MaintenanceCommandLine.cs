namespace AcademicSurveySystem.Api.Maintenance;

public sealed record MaintenanceCommandLine(
    bool BootstrapAdmin,
    bool ResetAdminPassword,
    bool ResetUserPassword,
    bool SeedUccuyoAcademicCatalog)
{
    public bool IsMaintenanceCommand =>
        BootstrapAdmin || ResetAdminPassword || ResetUserPassword || SeedUccuyoAcademicCatalog;

    public bool HasConflictingCommands =>
        new[] { BootstrapAdmin, ResetAdminPassword, ResetUserPassword, SeedUccuyoAcademicCatalog }
            .Count(item => item) > 1;

    public static MaintenanceCommandLine Parse(string[] args)
    {
        return new MaintenanceCommandLine(
            args.Contains("--bootstrap-admin", StringComparer.Ordinal),
            args.Contains("--reset-admin-password", StringComparer.Ordinal),
            args.Contains("--reset-user-password", StringComparer.Ordinal),
            args.Contains("--seed-uccuyo-academic-catalog", StringComparer.Ordinal));
    }
}
