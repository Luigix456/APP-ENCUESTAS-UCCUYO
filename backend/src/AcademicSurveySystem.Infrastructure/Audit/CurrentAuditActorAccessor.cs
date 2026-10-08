using AcademicSurveySystem.Application.Audit;

namespace AcademicSurveySystem.Infrastructure.Audit;

public sealed class CurrentAuditActorAccessor : IAuditActorAccessor
{
    private AuditActor? _current;

    public AuditActor Current => _current ?? AuditActor.System();

    public void SetCurrent(AuditActor actor)
    {
        _current = actor;
    }

    public void Clear()
    {
        _current = null;
    }
}
