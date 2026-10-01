namespace AcademicSurveySystem.Api.Maintenance;

public sealed record MaintenanceCommandLine(
    bool BootstrapAdmin,
    bool ResetAdminPassword,
    bool ResetUserPassword)
{
    public bool IsMaintenanceCommand => BootstrapAdmin || ResetAdminPassword || ResetUserPassword;

    public bool HasConflictingCommands =>
        new[] { BootstrapAdmin, ResetAdminPassword, ResetUserPassword }.Count(item => item) > 1;

    public static MaintenanceCommandLine Parse(string[] args)
    {
        return new MaintenanceCommandLine(
            args.Contains("--bootstrap-admin", StringComparer.Ordinal),
            args.Contains("--reset-admin-password", StringComparer.Ordinal),
            args.Contains("--reset-user-password", StringComparer.Ordinal));
    }
}
