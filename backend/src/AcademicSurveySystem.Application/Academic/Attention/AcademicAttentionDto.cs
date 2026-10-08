namespace AcademicSurveySystem.Application.Academic.Attention;

public sealed record AcademicAttentionDto(
    IReadOnlyCollection<AcademicAttentionItemDto> Items);

public sealed record AcademicAttentionItemDto(
    string Code,
    string Severity,
    string Title,
    string Description,
    string EntityType,
    Guid? EntityId,
    string ActionCode,
    int Count);

public sealed record AcademicAttentionPermissions(
    bool IncludeSurveyAlerts);
