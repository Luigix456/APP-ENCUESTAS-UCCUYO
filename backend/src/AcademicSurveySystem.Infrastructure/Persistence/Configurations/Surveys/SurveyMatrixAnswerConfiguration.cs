using AcademicSurveySystem.Domain.Surveys.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademicSurveySystem.Infrastructure.Persistence.Configurations.Surveys;

public sealed class SurveyMatrixAnswerConfiguration : IEntityTypeConfiguration<SurveyMatrixAnswer>
{
    public void Configure(EntityTypeBuilder<SurveyMatrixAnswer> builder)
    {
        builder.ToTable("survey_matrix_answers");

        builder.HasKey(answer => new
        {
            answer.SurveyAnswerId,
            answer.SurveyMatrixRowId
        });

        builder.Property(answer => answer.SurveyAnswerId)
            .HasColumnName("survey_answer_id")
            .IsRequired();

        builder.Property(answer => answer.SurveyMatrixRowId)
            .HasColumnName("survey_matrix_row_id")
            .IsRequired();

        builder.Property(answer => answer.SurveyQuestionOptionId)
            .HasColumnName("survey_question_option_id")
            .IsRequired();

        builder.HasIndex(answer => answer.SurveyMatrixRowId);
        builder.HasIndex(answer => answer.SurveyQuestionOptionId);

        builder.HasOne(answer => answer.SurveyMatrixRow)
            .WithMany()
            .HasForeignKey(answer => answer.SurveyMatrixRowId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(answer => answer.SurveyQuestionOption)
            .WithMany()
            .HasForeignKey(answer => answer.SurveyQuestionOptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
