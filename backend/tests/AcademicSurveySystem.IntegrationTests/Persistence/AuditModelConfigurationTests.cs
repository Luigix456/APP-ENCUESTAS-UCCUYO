using AcademicSurveySystem.Domain.Audit.Entities;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AcademicSurveySystem.IntegrationTests.Persistence;

public sealed class AuditModelConfigurationTests
{
    [Fact]
    public void AuditEntry_IsMappedToExpectedTableAndIndexes()
    {
        using var context = CreateContext();
        var entity = GetEntity<AuditEntry>(context);

        Assert.Equal("audit_entries", entity.GetTableName());
        AssertIndex(entity, nameof(AuditEntry.OccurredAtUtc));
        AssertIndex(entity, nameof(AuditEntry.ActorUserId), nameof(AuditEntry.OccurredAtUtc));
        AssertIndex(entity, nameof(AuditEntry.Module), nameof(AuditEntry.OccurredAtUtc));
        AssertIndex(
            entity,
            nameof(AuditEntry.EntityType),
            nameof(AuditEntry.EntityId),
            nameof(AuditEntry.OccurredAtUtc));
    }

    [Fact]
    public void AuditEntry_MetadataUsesJsonbColumn()
    {
        using var context = CreateContext();
        var property = GetEntity<AuditEntry>(context).FindProperty(nameof(AuditEntry.MetadataJson));

        Assert.NotNull(property);
        Assert.Equal("jsonb", property.GetColumnType());
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=academic_survey_db;Username=postgres;Password=postgres")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static IEntityType GetEntity<TEntity>(ApplicationDbContext context)
    {
        return context.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"Entity {typeof(TEntity).Name} was not found.");
    }

    private static void AssertIndex(IEntityType entity, params string[] propertyNames)
    {
        var index = entity.GetIndexes().SingleOrDefault(item =>
            item.Properties.Select(property => property.Name).SequenceEqual(propertyNames));

        Assert.NotNull(index);
    }
}
