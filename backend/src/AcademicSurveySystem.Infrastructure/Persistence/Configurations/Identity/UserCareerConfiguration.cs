using AcademicSurveySystem.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademicSurveySystem.Infrastructure.Persistence.Configurations.Identity;

public sealed class UserCareerConfiguration : IEntityTypeConfiguration<UserCareer>
{
    public void Configure(EntityTypeBuilder<UserCareer> builder)
    {
        builder.ToTable("user_careers");

        builder.HasKey(userCareer => new
        {
            userCareer.UserId,
            userCareer.CareerId
        });

        builder.Property(userCareer => userCareer.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(userCareer => userCareer.CareerId)
            .HasColumnName("career_id")
            .IsRequired();

        builder.Property(userCareer => userCareer.AssignedAtUtc)
            .HasColumnName("assigned_at_utc")
            .IsRequired();

        builder.HasIndex(userCareer => userCareer.CareerId);

        builder.HasOne(userCareer => userCareer.User)
            .WithMany(user => user.UserCareers)
            .HasForeignKey(userCareer => userCareer.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(userCareer => userCareer.Career)
            .WithMany(career => career.UserCareers)
            .HasForeignKey(userCareer => userCareer.CareerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
