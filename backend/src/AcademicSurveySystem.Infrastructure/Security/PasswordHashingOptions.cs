namespace AcademicSurveySystem.Infrastructure.Security;

public sealed class PasswordHashingOptions
{
    public const int MinimumWorkFactor = 10;
    public const int MaximumWorkFactor = 16;
    public const int DefaultWorkFactor = 12;

    public int WorkFactor { get; set; } = DefaultWorkFactor;

    public void Validate()
    {
        if (WorkFactor is < MinimumWorkFactor or > MaximumWorkFactor)
        {
            throw new InvalidOperationException(
                $"Password hashing WorkFactor must be between {MinimumWorkFactor} and {MaximumWorkFactor}.");
        }
    }
}
