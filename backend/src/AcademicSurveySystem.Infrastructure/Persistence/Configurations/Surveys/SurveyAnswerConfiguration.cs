using AcademicSurveySystem.Domain.Surveys.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademicSurveySystem.Infrastructure.Persistence.Configurations.Surveys;

public sealed class SurveyAnswerConfiguration : IEntityTypeConfiguration<SurveyAnswer>
{
    public void Configure(EntityTypeBuilder<SurveyAnswer> builder)
    {
        builder.ToTable("survey_answers");

        builder.HasKey(answer => answer.Id);

        builder.Property(answer => answer.Id)
            .HasColumnName("id");

        builder.Property(answer => answer.SurveyResponseId)
            .HasColumnName("survey_response_id")
            .IsRequired();

        builder.Property(answer => answer.SurveyQuestionId)
            .HasColumnName("survey_question_id")
            .IsRequired();

        builder.Property(answer => answer.TextValue)
            .HasColumnName("text_value")
            .HasMaxLength(4000);

        builder.Property(answer => answer.NumericValue)
            .HasColumnName("numeric_value");

        builder.Property(answer => answer.Comment)
            .HasColumnName("comment")
            .HasMaxLength(1000);

        builder.Property(answer => answer.OtherText)
            .HasColumnName("other_text")
            .HasMaxLength(1000);

        builder.HasIndex(answer => answer.SurveyResponseId);
        builder.HasIndex(answer => answer.SurveyQuestionId);

        builder.HasIndex(answer => new
            {
                answer.SurveyResponseId,
                answer.SurveyQuestionId
            })
            .IsUnique();

        builder.HasOne(answer => answer.SurveyQuestion)
            .WithMany()
            .HasForeignKey(answer => answer.SurveyQuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(answer => answer.SelectedOptions)
            .WithOne(option => option.SurveyAnswer)
            .HasForeignKey(option => option.SurveyAnswerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(answer => answer.MatrixAnswers)
            .WithOne(matrixAnswer => matrixAnswer.SurveyAnswer)
            .HasForeignKey(matrixAnswer => matrixAnswer.SurveyAnswerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(answer => answer.SelectedOptions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(answer => answer.MatrixAnswers)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
