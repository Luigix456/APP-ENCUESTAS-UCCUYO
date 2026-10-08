namespace AcademicSurveySystem.Application.Dashboard;

public sealed class DashboardOptions
{
    public decimal LowParticipationThresholdPercentage { get; set; } = 50m;

    public bool IsValid()
    {
        return LowParticipationThresholdPercentage is > 0m and <= 100m;
    }
}
