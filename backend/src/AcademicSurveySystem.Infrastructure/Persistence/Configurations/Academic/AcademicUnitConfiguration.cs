using AcademicSurveySystem.Domain.Academic.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademicSurveySystem.Infrastructure.Persistence.Configurations.Academic;

public sealed class AcademicUnitConfiguration : IEntityTypeConfiguration<AcademicUnit>
{
    public void Configure(EntityTypeBuilder<AcademicUnit> builder)
    {
        builder.ToTable("academic_units");

        builder.HasKey(unit => unit.Id);

        builder.Property(unit => unit.Id)
            .HasColumnName("id");

        builder.Property(unit => unit.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(unit => unit.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(unit => unit.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(unit => unit.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(unit => unit.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(unit => unit.Code)
            .IsUnique();

        builder.HasMany(unit => unit.Careers)
            .WithOne(career => career.AcademicUnit)
            .HasForeignKey(career => career.AcademicUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(unit => unit.Careers)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
