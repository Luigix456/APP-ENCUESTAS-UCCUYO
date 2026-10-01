using AcademicSurveySystem.Domain.Surveys.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademicSurveySystem.Infrastructure.Persistence.Configurations.Surveys;

public sealed class SurveyResponseConfiguration : IEntityTypeConfiguration<SurveyResponse>
{
    public void Configure(EntityTypeBuilder<SurveyResponse> builder)
    {
        builder.ToTable("survey_responses");

        builder.HasKey(response => response.Id);

        builder.Property(response => response.Id)
            .HasColumnName("id");

        builder.Property(response => response.SurveySessionId)
            .HasColumnName("survey_session_id")
            .IsRequired();

        builder.Property(response => response.SurveyId)
            .HasColumnName("survey_id")
            .IsRequired();

        builder.Property(response => response.SubmittedAtUtc)
            .HasColumnName("submitted_at_utc")
            .IsRequired();

        builder.HasIndex(response => response.SurveySessionId);
        builder.HasIndex(response => response.SurveyId);
        builder.HasIndex(response => response.SubmittedAtUtc);

        builder.HasOne(response => response.SurveySession)
            .WithMany()
            .HasForeignKey(response => response.SurveySessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(response => response.Survey)
            .WithMany()
            .HasForeignKey(response => response.SurveyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(response => response.Answers)
            .WithOne(answer => answer.SurveyResponse)
            .HasForeignKey(answer => answer.SurveyResponseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(response => response.Answers)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
