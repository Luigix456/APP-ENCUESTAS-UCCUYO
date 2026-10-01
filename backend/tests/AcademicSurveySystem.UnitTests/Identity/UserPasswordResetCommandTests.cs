using AcademicSurveySystem.Api.Maintenance;

namespace AcademicSurveySystem.UnitTests.Identity;

public sealed class UserPasswordResetCommandTests
{
    [Fact]
    public void PasswordsMatch_WithDifferentConfirmation_ReturnsFalse()
    {
        Assert.False(UserPasswordResetCommand.PasswordsMatch(
            "NewValidPassword1!",
            "OtherValidPassword1!"));
    }

    [Fact]
    public void PasswordsMatch_WithSamePassword_ReturnsTrue()
    {
        Assert.True(UserPasswordResetCommand.PasswordsMatch(
            "NewValidPassword1!",
            "NewValidPassword1!"));
    }

    [Fact]
    public void Parse_WithResetUserPassword_IdentifiesMaintenanceCommand()
    {
        var commandLine = MaintenanceCommandLine.Parse(["--reset-user-password"]);

        Assert.True(commandLine.ResetUserPassword);
        Assert.True(commandLine.IsMaintenanceCommand);
        Assert.False(commandLine.HasConflictingCommands);
    }

    [Fact]
    public void Parse_WithBootstrapAdmin_StillIdentifiesMaintenanceCommand()
    {
        var commandLine = MaintenanceCommandLine.Parse(["--bootstrap-admin"]);

        Assert.True(commandLine.BootstrapAdmin);
        Assert.True(commandLine.IsMaintenanceCommand);
        Assert.False(commandLine.HasConflictingCommands);
    }

    [Fact]
    public void Parse_WithMultipleMaintenanceCommands_IdentifiesConflict()
    {
        var commandLine = MaintenanceCommandLine.Parse([
            "--bootstrap-admin",
            "--reset-user-password"
        ]);

        Assert.True(commandLine.HasConflictingCommands);
    }
}
