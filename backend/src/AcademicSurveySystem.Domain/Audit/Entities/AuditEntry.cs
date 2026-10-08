using AcademicSurveySystem.Domain.Common;

namespace AcademicSurveySystem.Domain.Audit.Entities;

public sealed class AuditEntry
{
    private AuditEntry()
    {
        Action = null!;
        Module = null!;
        EntityType = null!;
        Description = null!;
    }

    public AuditEntry(
        Guid id,
        DateTimeOffset occurredAtUtc,
        Guid? actorUserId,
        string? actorDisplayName,
        string action,
        string module,
        string entityType,
        Guid? entityId,
        string description,
        string? metadataJson)
    {
        EnsureRequired(id, nameof(Id));
        EnsureUtc(occurredAtUtc, nameof(OccurredAtUtc));

        Id = id;
        OccurredAtUtc = occurredAtUtc;
        ActorUserId = actorUserId;
        ActorDisplayName = NormalizeOptionalText(actorDisplayName, nameof(ActorDisplayName), 240);
        Action = NormalizeRequiredText(action, nameof(Action), 120);
        Module = NormalizeRequiredText(module, nameof(Module), 80);
        EntityType = NormalizeRequiredText(entityType, nameof(EntityType), 120);
        EntityId = entityId;
        Description = NormalizeRequiredText(description, nameof(Description), 500);
        MetadataJson = NormalizeOptionalText(metadataJson, nameof(MetadataJson), 4000);
    }

    public Guid Id { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string? ActorDisplayName { get; private set; }
    public string Action { get; private set; }
    public string Module { get; private set; }
    public string EntityType { get; private set; }
    public Guid? EntityId { get; private set; }
    public string Description { get; private set; }
    public string? MetadataJson { get; private set; }

    private static void EnsureRequired(Guid value, string fieldName)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException($"{fieldName} is required.");
        }
    }

    private static void EnsureUtc(DateTimeOffset value, string fieldName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException($"{fieldName} must be UTC.");
        }
    }

    private static string NormalizeRequiredText(string value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{fieldName} must be {maxLength} characters or fewer.");
        }

        return normalized;
    }

    private static string? NormalizeOptionalText(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{fieldName} must be {maxLength} characters or fewer.");
        }

        return normalized;
    }
}
