using AcademicSurveySystem.Domain.Academic.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademicSurveySystem.Infrastructure.Persistence.Configurations.Academic;

public sealed class TeacherSubjectAssignmentConfiguration
    : IEntityTypeConfiguration<TeacherSubjectAssignment>
{
    public void Configure(EntityTypeBuilder<TeacherSubjectAssignment> builder)
    {
        builder.ToTable("teacher_subject_assignments");

        builder.HasKey(assignment => assignment.Id);

        builder.Property(assignment => assignment.Id)
            .HasColumnName("id");

        builder.Property(assignment => assignment.TeacherId)
            .HasColumnName("teacher_id")
            .IsRequired();

        builder.Property(assignment => assignment.SubjectId)
            .HasColumnName("subject_id")
            .IsRequired();

        builder.Property(assignment => assignment.AcademicCycleId)
            .HasColumnName("academic_cycle_id")
            .IsRequired();

        builder.Property(assignment => assignment.TeachingRole)
            .HasColumnName("teaching_role")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(assignment => assignment.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(assignment => assignment.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(assignment => assignment.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(assignment => assignment.TeacherId);
        builder.HasIndex(assignment => assignment.SubjectId);
        builder.HasIndex(assignment => assignment.AcademicCycleId);

        builder.HasIndex(assignment => new
            {
                assignment.TeacherId,
                assignment.SubjectId,
                assignment.AcademicCycleId
            })
            .IsUnique();

        builder.HasOne(assignment => assignment.Teacher)
            .WithMany(teacher => teacher.TeacherSubjectAssignments)
            .HasForeignKey(assignment => assignment.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(assignment => assignment.Subject)
            .WithMany(subject => subject.TeacherSubjectAssignments)
            .HasForeignKey(assignment => assignment.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(assignment => assignment.AcademicCycle)
            .WithMany(academicCycle => academicCycle.TeacherSubjectAssignments)
            .HasForeignKey(assignment => assignment.AcademicCycleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
