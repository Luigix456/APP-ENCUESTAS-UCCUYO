using System.Text.Json;
using AcademicSurveySystem.Application.Audit;
using AcademicSurveySystem.Domain.Audit.Entities;
using AcademicSurveySystem.Infrastructure.Persistence;

namespace AcademicSurveySystem.Infrastructure.Audit;

public sealed class AuditWriter : IAuditWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] ForbiddenMetadataTerms =
    [
        "password",
        "hash",
        "jwt",
        "authorization",
        "accesscode",
        "access_code",
        "comment",
        "answer",
        "othertext",
        "other_text",
        "connectionstring",
        "connection_string",
        "token",
        "secret"
    ];

    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditActorProvider _actorProvider;

    public AuditWriter(
        ApplicationDbContext dbContext,
        IAuditActorProvider actorProvider)
    {
        _dbContext = dbContext;
        _actorProvider = actorProvider;
    }

    public Task WriteAsync(
        string action,
        string module,
        string entityType,
        Guid? entityId,
        string description,
        object? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var actor = _actorProvider.Current;
        var metadataJson = SerializeMetadata(metadata);
        var entry = new AuditEntry(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            actor.UserId,
            actor.DisplayName,
            action,
            module,
            entityType,
            entityId,
            description,
            metadataJson);

        _dbContext.AuditEntries.Add(entry);
        return Task.CompletedTask;
    }

    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string? SerializeMetadata(object? metadata)
    {
        if (metadata is null)
        {
            return null;
        }

        var json = JsonSerializer.Serialize(metadata, JsonOptions);
        var normalized = json.ToLowerInvariant();

        if (ForbiddenMetadataTerms.Any(term => normalized.Contains(term, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Audit metadata contains a forbidden sensitive field.");
        }

        return json;
    }
}
