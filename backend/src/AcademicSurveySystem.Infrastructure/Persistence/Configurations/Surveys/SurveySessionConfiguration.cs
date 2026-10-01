using AcademicSurveySystem.Domain.Identity.Entities;
using AcademicSurveySystem.Domain.Surveys.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademicSurveySystem.Infrastructure.Persistence.Configurations.Surveys;

public sealed class SurveySessionConfiguration : IEntityTypeConfiguration<SurveySession>
{
    public void Configure(EntityTypeBuilder<SurveySession> builder)
    {
        builder.ToTable("survey_sessions");

        builder.HasKey(session => session.Id);

        builder.Property(session => session.Id)
            .HasColumnName("id");

        builder.Property(session => session.SurveyAssignmentId)
            .HasColumnName("survey_assignment_id")
            .IsRequired();

        builder.Property(session => session.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(session => session.AccessCode)
            .HasColumnName("access_code")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(session => session.Title)
            .HasColumnName("title")
            .HasMaxLength(200);

        builder.Property(session => session.Location)
            .HasColumnName("location")
            .HasMaxLength(200);

        builder.Property(session => session.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(session => session.ExpiresAtUtc)
            .HasColumnName("expires_at_utc")
            .IsRequired();

        builder.Property(session => session.OpenedAtUtc)
            .HasColumnName("opened_at_utc");

        builder.Property(session => session.ClosedAtUtc)
            .HasColumnName("closed_at_utc");

        builder.Property(session => session.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(session => session.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(session => session.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(session => session.AccessCode)
            .IsUnique();
        builder.HasIndex(session => session.SurveyAssignmentId);
        builder.HasIndex(session => session.CreatedByUserId);
        builder.HasIndex(session => session.Status);
        builder.HasIndex(session => session.ExpiresAtUtc);

        builder.HasOne(session => session.SurveyAssignment)
            .WithMany()
            .HasForeignKey(session => session.SurveyAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(session => session.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
