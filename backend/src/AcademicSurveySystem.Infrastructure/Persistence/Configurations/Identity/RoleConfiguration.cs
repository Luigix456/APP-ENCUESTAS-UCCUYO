using AcademicSurveySystem.Domain.Identity;
using AcademicSurveySystem.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademicSurveySystem.Infrastructure.Persistence.Configurations.Identity;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(role => role.Id);

        builder.Property(role => role.Id)
            .HasColumnName("id");

        builder.Property(role => role.Code)
            .HasColumnName("code")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(role => role.Name)
            .HasColumnName("name")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(role => role.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(role => role.IsSystem)
            .HasColumnName("is_system")
            .IsRequired();

        builder.Property(role => role.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(role => role.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(role => role.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(role => role.Code)
            .IsUnique();

        builder.HasMany(role => role.UserRoles)
            .WithOne(userRole => userRole.Role)
            .HasForeignKey(userRole => userRole.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(role => role.RolePermissions)
            .WithOne(rolePermission => rolePermission.Role)
            .HasForeignKey(rolePermission => rolePermission.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(role => role.UserRoles)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(role => role.RolePermissions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasData(IdentityCatalog.Roles.Select(role => new
        {
            role.Id,
            role.Code,
            role.Name,
            role.Description,
            IsSystem = true,
            IsActive = true,
            CreatedAtUtc = IdentityCatalog.CatalogDateUtc,
            UpdatedAtUtc = IdentityCatalog.CatalogDateUtc
        }).ToArray());
    }
}
