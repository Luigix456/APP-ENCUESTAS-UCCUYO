using AcademicSurveySystem.Domain.Academic.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademicSurveySystem.Infrastructure.Persistence.Configurations.Academic;

public sealed class CareerConfiguration : IEntityTypeConfiguration<Career>
{
    public void Configure(EntityTypeBuilder<Career> builder)
    {
        builder.ToTable("careers");

        builder.HasKey(career => career.Id);

        builder.Property(career => career.Id)
            .HasColumnName("id");

        builder.Property(career => career.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(career => career.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(career => career.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(career => career.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(career => career.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(career => career.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(career => career.Code)
            .IsUnique();

        builder.HasMany(career => career.Subjects)
            .WithOne(subject => subject.Career)
            .HasForeignKey(subject => subject.CareerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(career => career.Subjects)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
