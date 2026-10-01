namespace AcademicSurveySystem.Infrastructure.Identity;

public sealed class AdminPasswordResetOptions
{
    public string? Email { get; set; }
    public string? NewPassword { get; set; }
}
