using AcademicSurveySystem.Domain.Academic.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademicSurveySystem.Infrastructure.Persistence.Configurations.Academic;

public sealed class AcademicCycleConfiguration : IEntityTypeConfiguration<AcademicCycle>
{
    public void Configure(EntityTypeBuilder<AcademicCycle> builder)
    {
        builder.ToTable("academic_cycles");

        builder.HasKey(academicCycle => academicCycle.Id);

        builder.Property(academicCycle => academicCycle.Id)
            .HasColumnName("id");

        builder.Property(academicCycle => academicCycle.Year)
            .HasColumnName("year")
            .IsRequired();

        builder.Property(academicCycle => academicCycle.Period)
            .HasColumnName("period")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(academicCycle => academicCycle.StartDate)
            .HasColumnName("start_date")
            .IsRequired();

        builder.Property(academicCycle => academicCycle.EndDate)
            .HasColumnName("end_date")
            .IsRequired();

        builder.Property(academicCycle => academicCycle.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(academicCycle => academicCycle.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(academicCycle => academicCycle.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(academicCycle => new
            {
                academicCycle.Year,
                academicCycle.Period
            })
            .IsUnique();

        builder.HasMany(academicCycle => academicCycle.TeacherSubjectAssignments)
            .WithOne(assignment => assignment.AcademicCycle)
            .HasForeignKey(assignment => assignment.AcademicCycleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(academicCycle => academicCycle.TeacherSubjectAssignments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
