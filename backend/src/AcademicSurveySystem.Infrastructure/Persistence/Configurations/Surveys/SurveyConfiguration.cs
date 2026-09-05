using AcademicSurveySystem.Domain.Identity.Entities;
using AcademicSurveySystem.Domain.Surveys.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademicSurveySystem.Infrastructure.Persistence.Configurations.Surveys;

public sealed class SurveyConfiguration : IEntityTypeConfiguration<Survey>
{
    public void Configure(EntityTypeBuilder<Survey> builder)
    {
        builder.ToTable("surveys");

        builder.HasKey(survey => survey.Id);

        builder.Property(survey => survey.Id)
            .HasColumnName("id");

        builder.Property(survey => survey.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(survey => survey.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(survey => survey.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(survey => survey.Target)
            .HasColumnName("target")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(survey => survey.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(survey => survey.IsAnonymous)
            .HasColumnName("is_anonymous")
            .IsRequired();

        builder.Property(survey => survey.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(survey => survey.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(survey => survey.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(survey => survey.CreatedByUserId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(survey => survey.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(survey => survey.Sections)
            .WithOne(section => section.Survey)
            .HasForeignKey(section => section.SurveyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(survey => survey.Sections)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
