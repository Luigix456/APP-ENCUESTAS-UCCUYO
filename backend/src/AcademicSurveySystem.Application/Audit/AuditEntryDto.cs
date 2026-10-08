using System.Text.Json.Nodes;

namespace AcademicSurveySystem.Application.Audit;

public sealed record AuditEntryDto(
    Guid Id,
    DateTimeOffset OccurredAtUtc,
    Guid? ActorUserId,
    string? ActorDisplayName,
    string Action,
    string Module,
    string EntityType,
    Guid? EntityId,
    string Description,
    JsonNode? Metadata);

public sealed record AuditEntriesPageDto(
    IReadOnlyCollection<AuditEntryDto> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public sealed record AuditEntryFilter(
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    Guid? ActorUserId,
    string? Module,
    string? Action,
    string? EntityType,
    string? Search,
    int Page,
    int PageSize);
