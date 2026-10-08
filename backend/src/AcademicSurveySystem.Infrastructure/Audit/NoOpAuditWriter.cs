using AcademicSurveySystem.Application.Audit;

namespace AcademicSurveySystem.Infrastructure.Audit;

public sealed class NoOpAuditWriter : IAuditWriter
{
    public static readonly NoOpAuditWriter Instance = new();

    private NoOpAuditWriter()
    {
    }

    public Task WriteAsync(
        string action,
        string module,
        string entityType,
        Guid? entityId,
        string description,
        object? metadata = null,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task FlushAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
