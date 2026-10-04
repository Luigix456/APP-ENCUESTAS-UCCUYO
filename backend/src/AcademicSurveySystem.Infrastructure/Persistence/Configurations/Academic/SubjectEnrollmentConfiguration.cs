using AcademicSurveySystem.Domain.Academic.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademicSurveySystem.Infrastructure.Persistence.Configurations.Academic;

public sealed class SubjectEnrollmentConfiguration : IEntityTypeConfiguration<SubjectEnrollment>
{
    public void Configure(EntityTypeBuilder<SubjectEnrollment> builder)
    {
        builder.ToTable("subject_enrollments", table => table.HasCheckConstraint(
            "ck_subject_enrollments_enrolled_student_count_positive",
            "enrolled_student_count > 0"));

        builder.HasKey(enrollment => enrollment.Id);

        builder.Property(enrollment => enrollment.Id)
            .HasColumnName("id");

        builder.Property(enrollment => enrollment.SubjectId)
            .HasColumnName("subject_id")
            .IsRequired();

        builder.Property(enrollment => enrollment.AcademicCycleId)
            .HasColumnName("academic_cycle_id")
            .IsRequired();

        builder.Property(enrollment => enrollment.EnrolledStudentCount)
            .HasColumnName("enrolled_student_count")
            .IsRequired();

        builder.Property(enrollment => enrollment.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(enrollment => enrollment.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(enrollment => enrollment.SubjectId);
        builder.HasIndex(enrollment => enrollment.AcademicCycleId);
        builder.HasIndex(enrollment => new
            {
                enrollment.SubjectId,
                enrollment.AcademicCycleId
            })
            .IsUnique();

        builder.HasOne(enrollment => enrollment.Subject)
            .WithMany(subject => subject.Enrollments)
            .HasForeignKey(enrollment => enrollment.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(enrollment => enrollment.AcademicCycle)
            .WithMany()
            .HasForeignKey(enrollment => enrollment.AcademicCycleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
