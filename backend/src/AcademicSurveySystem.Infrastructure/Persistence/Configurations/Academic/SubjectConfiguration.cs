using AcademicSurveySystem.Domain.Academic.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademicSurveySystem.Infrastructure.Persistence.Configurations.Academic;

public sealed class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("subjects");

        builder.HasKey(subject => subject.Id);

        builder.Property(subject => subject.Id)
            .HasColumnName("id");

        builder.Property(subject => subject.CareerId)
            .HasColumnName("career_id")
            .IsRequired();

        builder.Property(subject => subject.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(subject => subject.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(subject => subject.Year)
            .HasColumnName("year")
            .IsRequired();

        builder.Property(subject => subject.Period)
            .HasColumnName("period")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(subject => subject.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(subject => subject.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(subject => subject.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(subject => subject.CareerId);

        builder.HasIndex(subject => new
            {
                subject.CareerId,
                subject.Code
            })
            .IsUnique();

        builder.HasOne(subject => subject.Career)
            .WithMany(career => career.Subjects)
            .HasForeignKey(subject => subject.CareerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(subject => subject.TeacherSubjectAssignments)
            .WithOne(assignment => assignment.Subject)
            .HasForeignKey(assignment => assignment.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(subject => subject.Enrollments)
            .WithOne(enrollment => enrollment.Subject)
            .HasForeignKey(enrollment => enrollment.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(subject => subject.TeacherSubjectAssignments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(subject => subject.Enrollments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
