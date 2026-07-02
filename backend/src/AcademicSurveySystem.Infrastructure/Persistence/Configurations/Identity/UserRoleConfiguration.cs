using AcademicSurveySystem.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademicSurveySystem.Infrastructure.Persistence.Configurations.Identity;

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");

        builder.HasKey(userRole => new
        {
            userRole.UserId,
            userRole.RoleId
        });

        builder.Property(userRole => userRole.UserId)
            .HasColumnName("user_id");

        builder.Property(userRole => userRole.RoleId)
            .HasColumnName("role_id");

        builder.Property(userRole => userRole.AssignedAtUtc)
            .HasColumnName("assigned_at_utc")
            .IsRequired();

        builder.HasIndex(userRole => userRole.RoleId);

        builder.HasOne(userRole => userRole.User)
            .WithMany(user => user.UserRoles)
            .HasForeignKey(userRole => userRole.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(userRole => userRole.Role)
            .WithMany(role => role.UserRoles)
            .HasForeignKey(userRole => userRole.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
