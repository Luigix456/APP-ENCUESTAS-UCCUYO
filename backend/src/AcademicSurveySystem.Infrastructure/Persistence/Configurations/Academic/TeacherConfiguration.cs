using AcademicSurveySystem.Domain.Academic.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademicSurveySystem.Infrastructure.Persistence.Configurations.Academic;

public sealed class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
{
    public void Configure(EntityTypeBuilder<Teacher> builder)
    {
        builder.ToTable("teachers");

        builder.HasKey(teacher => teacher.Id);

        builder.Property(teacher => teacher.Id)
            .HasColumnName("id");

        builder.Property(teacher => teacher.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(teacher => teacher.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(teacher => teacher.Email)
            .HasColumnName("email")
            .HasMaxLength(320);

        builder.Property(teacher => teacher.NormalizedEmail)
            .HasColumnName("normalized_email")
            .HasMaxLength(320);

        builder.Property(teacher => teacher.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(teacher => teacher.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(teacher => teacher.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(teacher => teacher.NormalizedEmail)
            .IsUnique()
            .HasFilter("normalized_email IS NOT NULL");

        builder.HasMany(teacher => teacher.TeacherSubjectAssignments)
            .WithOne(assignment => assignment.Teacher)
            .HasForeignKey(assignment => assignment.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(teacher => teacher.TeacherSubjectAssignments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
