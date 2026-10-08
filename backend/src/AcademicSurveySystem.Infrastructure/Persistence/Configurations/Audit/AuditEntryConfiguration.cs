using AcademicSurveySystem.Domain.Audit.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademicSurveySystem.Infrastructure.Persistence.Configurations.Audit;

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_entries");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id)
            .HasColumnName("id");

        builder.Property(entry => entry.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .IsRequired();

        builder.Property(entry => entry.ActorUserId)
            .HasColumnName("actor_user_id");

        builder.Property(entry => entry.ActorDisplayName)
            .HasColumnName("actor_display_name")
            .HasMaxLength(240);

        builder.Property(entry => entry.Action)
            .HasColumnName("action")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(entry => entry.Module)
            .HasColumnName("module")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(entry => entry.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(entry => entry.EntityId)
            .HasColumnName("entity_id");

        builder.Property(entry => entry.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(entry => entry.MetadataJson)
            .HasColumnName("metadata_json")
            .HasColumnType("jsonb");

        builder.HasIndex(entry => entry.OccurredAtUtc);
        builder.HasIndex(entry => new { entry.ActorUserId, entry.OccurredAtUtc });
        builder.HasIndex(entry => new { entry.Module, entry.OccurredAtUtc });
        builder.HasIndex(entry => new { entry.EntityType, entry.EntityId, entry.OccurredAtUtc });
    }
}
