using AcademicSurveySystem.Domain.Surveys.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademicSurveySystem.Infrastructure.Persistence.Configurations.Surveys;

public sealed class SurveyQuestionOptionConfiguration : IEntityTypeConfiguration<SurveyQuestionOption>
{
    public void Configure(EntityTypeBuilder<SurveyQuestionOption> builder)
    {
        builder.ToTable("survey_question_options");

        builder.HasKey(option => option.Id);

        builder.Property(option => option.Id)
            .HasColumnName("id");

        builder.Property(option => option.SurveyQuestionId)
            .HasColumnName("survey_question_id")
            .IsRequired();

        builder.Property(option => option.Text)
            .HasColumnName("text")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(option => option.Value)
            .HasColumnName("value")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(option => option.Order)
            .HasColumnName("order")
            .IsRequired();

        builder.Property(option => option.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(option => option.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(option => option.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(option => option.SurveyQuestionId);

        builder.HasIndex(option => new
            {
                option.SurveyQuestionId,
                option.Order
            })
            .IsUnique();

        builder.HasOne(option => option.SurveyQuestion)
            .WithMany(question => question.Options)
            .HasForeignKey(option => option.SurveyQuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
