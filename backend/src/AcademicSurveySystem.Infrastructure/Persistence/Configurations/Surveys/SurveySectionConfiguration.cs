using AcademicSurveySystem.Domain.Surveys.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademicSurveySystem.Infrastructure.Persistence.Configurations.Surveys;

public sealed class SurveySectionConfiguration : IEntityTypeConfiguration<SurveySection>
{
    public void Configure(EntityTypeBuilder<SurveySection> builder)
    {
        builder.ToTable("survey_sections");

        builder.HasKey(section => section.Id);

        builder.Property(section => section.Id)
            .HasColumnName("id");

        builder.Property(section => section.SurveyId)
            .HasColumnName("survey_id")
            .IsRequired();

        builder.Property(section => section.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(section => section.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(section => section.Order)
            .HasColumnName("order")
            .IsRequired();

        builder.Property(section => section.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(section => section.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(section => section.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(section => section.SurveyId);

        builder.HasIndex(section => new
            {
                section.SurveyId,
                section.Order
            })
            .IsUnique();

        builder.HasOne(section => section.Survey)
            .WithMany(survey => survey.Sections)
            .HasForeignKey(section => section.SurveyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(section => section.Questions)
            .WithOne(question => question.SurveySection)
            .HasForeignKey(question => question.SurveySectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(section => section.Questions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
