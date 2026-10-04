using AcademicSurveySystem.Domain.Surveys.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademicSurveySystem.Infrastructure.Persistence.Configurations.Surveys;

public sealed class SurveyAssignmentConfiguration : IEntityTypeConfiguration<SurveyAssignment>
{
    public void Configure(EntityTypeBuilder<SurveyAssignment> builder)
    {
        builder.ToTable("survey_assignments", table => table.HasCheckConstraint(
            "ck_survey_assignments_expected_respondent_count_positive",
            "expected_respondent_count IS NULL OR expected_respondent_count > 0"));

        builder.HasKey(assignment => assignment.Id);

        builder.Property(assignment => assignment.Id)
            .HasColumnName("id");

        builder.Property(assignment => assignment.SurveyId)
            .HasColumnName("survey_id")
            .IsRequired();

        builder.Property(assignment => assignment.CareerId)
            .HasColumnName("career_id")
            .IsRequired();

        builder.Property(assignment => assignment.SubjectId)
            .HasColumnName("subject_id")
            .IsRequired();

        builder.Property(assignment => assignment.AcademicCycleId)
            .HasColumnName("academic_cycle_id")
            .IsRequired();

        builder.Property(assignment => assignment.TeacherSubjectAssignmentId)
            .HasColumnName("teacher_subject_assignment_id")
            .IsRequired();

        builder.Property(assignment => assignment.ExpectedRespondentCount)
            .HasColumnName("expected_respondent_count");

        builder.Property(assignment => assignment.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(assignment => assignment.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(assignment => assignment.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(assignment => assignment.SurveyId);
        builder.HasIndex(assignment => assignment.CareerId);
        builder.HasIndex(assignment => assignment.SubjectId);
        builder.HasIndex(assignment => assignment.AcademicCycleId);
        builder.HasIndex(assignment => assignment.TeacherSubjectAssignmentId);

        builder.HasIndex(assignment => new
            {
                assignment.SurveyId,
                assignment.CareerId,
                assignment.SubjectId,
                assignment.AcademicCycleId,
                assignment.TeacherSubjectAssignmentId
            })
            .IsUnique();

        builder.HasOne(assignment => assignment.Survey)
            .WithMany()
            .HasForeignKey(assignment => assignment.SurveyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(assignment => assignment.Career)
            .WithMany()
            .HasForeignKey(assignment => assignment.CareerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(assignment => assignment.Subject)
            .WithMany()
            .HasForeignKey(assignment => assignment.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(assignment => assignment.AcademicCycle)
            .WithMany()
            .HasForeignKey(assignment => assignment.AcademicCycleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(assignment => assignment.TeacherSubjectAssignment)
            .WithMany()
            .HasForeignKey(assignment => assignment.TeacherSubjectAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
