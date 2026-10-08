using System.Text.Json.Nodes;
using AcademicSurveySystem.Application.Audit;
using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AcademicSurveySystem.Infrastructure.Audit;

public sealed class AuditQueryService : IAuditQueryService
{
    private static readonly IReadOnlyDictionary<string, string[]> ModuleAliases =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["academic"] = ["academic_catalog"],
            ["surveys"] = ["survey_templates", "survey_assignments"],
            ["sessions"] = ["survey_sessions"]
        };

    private readonly ApplicationDbContext _dbContext;

    public AuditQueryService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApplicationResult<AuditEntriesPageDto>> GetEntriesAsync(
        AuditEntryFilter filter,
        CancellationToken cancellationToken)
    {
        var validationErrors = Validate(filter);

        if (validationErrors.Count > 0)
        {
            return ApplicationResult<AuditEntriesPageDto>.Validation(validationErrors);
        }

        var query = _dbContext.AuditEntries.AsNoTracking().AsQueryable();

        if (filter.FromUtc is not null)
        {
            query = query.Where(entry => entry.OccurredAtUtc >= filter.FromUtc.Value);
        }

        if (filter.ToUtc is not null)
        {
            query = query.Where(entry => entry.OccurredAtUtc <= filter.ToUtc.Value);
        }

        if (filter.ActorUserId is not null)
        {
            query = query.Where(entry => entry.ActorUserId == filter.ActorUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Module))
        {
            var modules = ResolveModules(filter.Module);
            query = query.Where(entry => modules.Contains(entry.Module));
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            var action = filter.Action.Trim();
            query = query.Where(entry => entry.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(filter.EntityType))
        {
            var entityType = filter.EntityType.Trim();
            query = query.Where(entry => entry.EntityType == entityType);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(entry =>
                entry.Description.ToLower().Contains(search)
                || (entry.ActorDisplayName != null && entry.ActorDisplayName.ToLower().Contains(search)));
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)filter.PageSize));
        var rows = await query
            .OrderByDescending(entry => entry.OccurredAtUtc)
            .ThenByDescending(entry => entry.Id)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(entry => new
            {
                entry.Id,
                entry.OccurredAtUtc,
                entry.ActorUserId,
                entry.ActorDisplayName,
                entry.Action,
                entry.Module,
                entry.EntityType,
                entry.EntityId,
                entry.Description,
                entry.MetadataJson
            })
            .ToArrayAsync(cancellationToken);

        var items = rows
            .Select(row => new AuditEntryDto(
                row.Id,
                row.OccurredAtUtc,
                row.ActorUserId,
                row.ActorDisplayName,
                row.Action,
                row.Module,
                row.EntityType,
                row.EntityId,
                row.Description,
                ParseMetadata(row.MetadataJson)))
            .ToArray();

        return ApplicationResult<AuditEntriesPageDto>.Success(new AuditEntriesPageDto(
            items,
            filter.Page,
            filter.PageSize,
            totalItems,
            totalPages));
    }

    private static IReadOnlyCollection<ApplicationError> Validate(AuditEntryFilter filter)
    {
        var errors = new List<ApplicationError>();

        if (filter.Page < 1)
        {
            errors.Add(new ApplicationError("Audit.PageInvalid", "Page must be greater than or equal to 1."));
        }

        if (filter.PageSize is < 1 or > 100)
        {
            errors.Add(new ApplicationError("Audit.PageSizeInvalid", "PageSize must be between 1 and 100."));
        }

        if (filter.FromUtc is not null && filter.FromUtc.Value.Offset != TimeSpan.Zero)
        {
            errors.Add(new ApplicationError("Audit.FromUtcInvalid", "FromUtc must be UTC."));
        }

        if (filter.ToUtc is not null && filter.ToUtc.Value.Offset != TimeSpan.Zero)
        {
            errors.Add(new ApplicationError("Audit.ToUtcInvalid", "ToUtc must be UTC."));
        }

        if (filter.FromUtc is not null
            && filter.ToUtc is not null
            && filter.FromUtc > filter.ToUtc)
        {
            errors.Add(new ApplicationError("Audit.DateRangeInvalid", "FromUtc must be earlier than ToUtc."));
        }

        return errors;
    }

    private static string[] ResolveModules(string module)
    {
        var normalized = module.Trim();

        return ModuleAliases.TryGetValue(normalized, out var modules)
            ? modules
            : [normalized];
    }

    private static JsonNode? ParseMetadata(string? metadataJson)
    {
        return string.IsNullOrWhiteSpace(metadataJson)
            ? null
            : JsonNode.Parse(metadataJson);
    }
}
