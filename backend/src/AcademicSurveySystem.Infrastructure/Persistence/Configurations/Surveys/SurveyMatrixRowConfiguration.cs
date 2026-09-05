using AcademicSurveySystem.Domain.Surveys.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademicSurveySystem.Infrastructure.Persistence.Configurations.Surveys;

public sealed class SurveyMatrixRowConfiguration : IEntityTypeConfiguration<SurveyMatrixRow>
{
    public void Configure(EntityTypeBuilder<SurveyMatrixRow> builder)
    {
        builder.ToTable("survey_matrix_rows");

        builder.HasKey(matrixRow => matrixRow.Id);

        builder.Property(matrixRow => matrixRow.Id)
            .HasColumnName("id");

        builder.Property(matrixRow => matrixRow.SurveyQuestionId)
            .HasColumnName("survey_question_id")
            .IsRequired();

        builder.Property(matrixRow => matrixRow.Text)
            .HasColumnName("text")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(matrixRow => matrixRow.Order)
            .HasColumnName("order")
            .IsRequired();

        builder.Property(matrixRow => matrixRow.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(matrixRow => matrixRow.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(matrixRow => matrixRow.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(matrixRow => matrixRow.SurveyQuestionId);

        builder.HasIndex(matrixRow => new
            {
                matrixRow.SurveyQuestionId,
                matrixRow.Order
            })
            .IsUnique();

        builder.HasOne(matrixRow => matrixRow.SurveyQuestion)
            .WithMany(question => question.MatrixRows)
            .HasForeignKey(matrixRow => matrixRow.SurveyQuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
