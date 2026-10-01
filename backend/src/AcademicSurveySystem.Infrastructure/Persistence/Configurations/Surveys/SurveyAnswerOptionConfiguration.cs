using AcademicSurveySystem.Domain.Surveys.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademicSurveySystem.Infrastructure.Persistence.Configurations.Surveys;

public sealed class SurveyAnswerOptionConfiguration : IEntityTypeConfiguration<SurveyAnswerOption>
{
    public void Configure(EntityTypeBuilder<SurveyAnswerOption> builder)
    {
        builder.ToTable("survey_answer_options");

        builder.HasKey(option => new
        {
            option.SurveyAnswerId,
            option.SurveyQuestionOptionId
        });

        builder.Property(option => option.SurveyAnswerId)
            .HasColumnName("survey_answer_id")
            .IsRequired();

        builder.Property(option => option.SurveyQuestionOptionId)
            .HasColumnName("survey_question_option_id")
            .IsRequired();

        builder.HasIndex(option => option.SurveyQuestionOptionId);

        builder.HasOne(option => option.SurveyQuestionOption)
            .WithMany()
            .HasForeignKey(option => option.SurveyQuestionOptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
