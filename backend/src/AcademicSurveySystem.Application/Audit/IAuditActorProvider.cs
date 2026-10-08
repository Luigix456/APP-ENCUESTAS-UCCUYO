namespace AcademicSurveySystem.Application.Audit;

public interface IAuditActorProvider
{
    AuditActor Current { get; }
}

public interface IAuditActorAccessor : IAuditActorProvider
{
    void SetCurrent(AuditActor actor);
    void Clear();
}
