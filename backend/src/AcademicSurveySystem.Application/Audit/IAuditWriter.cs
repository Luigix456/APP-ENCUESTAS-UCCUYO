namespace AcademicSurveySystem.Application.Audit;

public interface IAuditWriter
{
    Task WriteAsync(
        string action,
        string module,
        string entityType,
        Guid? entityId,
        string description,
        object? metadata = null,
        CancellationToken cancellationToken = default);

    Task FlushAsync(CancellationToken cancellationToken = default);
}
