using AcademicSurveySystem.Domain.Surveys.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademicSurveySystem.Infrastructure.Persistence.Configurations.Surveys;

public sealed class SurveyQuestionConfiguration : IEntityTypeConfiguration<SurveyQuestion>
{
    public void Configure(EntityTypeBuilder<SurveyQuestion> builder)
    {
        builder.ToTable("survey_questions");

        builder.HasKey(question => question.Id);

        builder.Property(question => question.Id)
            .HasColumnName("id");

        builder.Property(question => question.QuestionLineageId)
            .HasColumnName("question_lineage_id")
            .IsRequired();

        builder.Property(question => question.SurveySectionId)
            .HasColumnName("survey_section_id")
            .IsRequired();

        builder.Property(question => question.Text)
            .HasColumnName("text")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(question => question.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(question => question.IsRequired)
            .HasColumnName("is_required")
            .IsRequired();

        builder.Property(question => question.AllowsComment)
            .HasColumnName("allows_comment")
            .IsRequired();

        builder.Property(question => question.AllowsOtherOption)
            .HasColumnName("allows_other_option")
            .IsRequired();

        builder.Property(question => question.RatingMin)
            .HasColumnName("rating_min");

        builder.Property(question => question.RatingMax)
            .HasColumnName("rating_max");

        builder.Property(question => question.Order)
            .HasColumnName("order")
            .IsRequired();

        builder.Property(question => question.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(question => question.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(question => question.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(question => question.SurveySectionId);

        builder.HasIndex(question => question.QuestionLineageId);

        builder.HasIndex(question => new
            {
                question.SurveySectionId,
                question.QuestionLineageId
            });

        builder.HasIndex(question => new
            {
                question.SurveySectionId,
                question.Order
            })
            .IsUnique();

        builder.HasOne(question => question.SurveySection)
            .WithMany(section => section.Questions)
            .HasForeignKey(question => question.SurveySectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(question => question.Options)
            .WithOne(option => option.SurveyQuestion)
            .HasForeignKey(option => option.SurveyQuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(question => question.MatrixRows)
            .WithOne(matrixRow => matrixRow.SurveyQuestion)
            .HasForeignKey(matrixRow => matrixRow.SurveyQuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(question => question.Options)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(question => question.MatrixRows)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
