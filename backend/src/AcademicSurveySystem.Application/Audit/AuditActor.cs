namespace AcademicSurveySystem.Application.Audit;

public sealed record AuditActor(Guid? UserId, string DisplayName)
{
    public static AuditActor System() => new(null, "Sistema");
}
