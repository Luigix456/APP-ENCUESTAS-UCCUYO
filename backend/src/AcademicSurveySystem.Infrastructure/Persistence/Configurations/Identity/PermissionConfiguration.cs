using AcademicSurveySystem.Domain.Identity;
using AcademicSurveySystem.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademicSurveySystem.Infrastructure.Persistence.Configurations.Identity;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(permission => permission.Id);

        builder.Property(permission => permission.Id)
            .HasColumnName("id");

        builder.Property(permission => permission.Code)
            .HasColumnName("code")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(permission => permission.Name)
            .HasColumnName("name")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(permission => permission.Module)
            .HasColumnName("module")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(permission => permission.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(permission => permission.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(permission => permission.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(permission => permission.Code)
            .IsUnique();

        builder.HasMany(permission => permission.RolePermissions)
            .WithOne(rolePermission => rolePermission.Permission)
            .HasForeignKey(rolePermission => rolePermission.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(permission => permission.RolePermissions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasData(IdentityCatalog.Permissions.Select(permission => new
        {
            permission.Id,
            permission.Code,
            permission.Name,
            permission.Module,
            permission.Description,
            CreatedAtUtc = IdentityCatalog.CatalogDateUtc,
            UpdatedAtUtc = IdentityCatalog.CatalogDateUtc
        }).ToArray());
    }
}
