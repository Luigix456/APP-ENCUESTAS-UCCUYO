using AcademicSurveySystem.Domain.Common;
using AcademicSurveySystem.Domain.Identity.Entities;

namespace AcademicSurveySystem.UnitTests.Identity;

public sealed class RoleTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedAt = new(2026, 1, 2, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CreatesValidRole()
    {
        var role = CreateRole();

        Assert.Equal("coordinador", role.Code);
        Assert.Equal("Coordinador", role.Name);
        Assert.True(role.IsActive);
        Assert.False(role.IsSystem);
    }

    [Fact]
    public void Constructor_NormalizesCode()
    {
        var role = new Role(Guid.NewGuid(), "  Admin.Sistema_1  ", "Admin", null, true, CreatedAt);

        Assert.Equal("admin.sistema_1", role.Code);
    }

    [Theory]
    [InlineData("admin role")]
    [InlineData("admin@role")]
    public void Constructor_RejectsInvalidCode(string code)
    {
        Assert.Throws<DomainException>(() =>
            new Role(Guid.NewGuid(), code, "Admin", null, false, CreatedAt));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsEmptyName(string name)
    {
        Assert.Throws<DomainException>(() =>
            new Role(Guid.NewGuid(), "admin", name, null, false, CreatedAt));
    }

    [Fact]
    public void UpdateDetails_UpdatesData()
    {
        var role = CreateRole();

        role.UpdateDetails("Nuevo nombre", "Nueva descripcion", UpdatedAt);

        Assert.Equal("Nuevo nombre", role.Name);
        Assert.Equal("Nueva descripcion", role.Description);
        Assert.Equal(UpdatedAt, role.UpdatedAtUtc);
    }

    [Fact]
    public void ActivateAndDeactivate_ChangeActiveState()
    {
        var role = CreateRole();

        role.Deactivate(UpdatedAt);
        Assert.False(role.IsActive);

        role.Activate(UpdatedAt.AddDays(1));
        Assert.True(role.IsActive);
    }

    [Fact]
    public void GrantPermission_AddsPermission()
    {
        var role = CreateRole();
        var permissionId = Guid.NewGuid();

        role.GrantPermission(permissionId, UpdatedAt);

        Assert.Contains(role.RolePermissions, item => item.PermissionId == permissionId);
    }

    [Fact]
    public void GrantPermission_DoesNotDuplicatePermission()
    {
        var role = CreateRole();
        var permissionId = Guid.NewGuid();

        role.GrantPermission(permissionId, UpdatedAt);
        role.GrantPermission(permissionId, UpdatedAt.AddDays(1));

        Assert.Single(role.RolePermissions);
    }

    [Fact]
    public void RevokePermission_RemovesPermission()
    {
        var role = CreateRole();
        var permissionId = Guid.NewGuid();
        role.GrantPermission(permissionId, UpdatedAt);

        role.RevokePermission(permissionId);

        Assert.Empty(role.RolePermissions);
    }

    private static Role CreateRole()
    {
        return new Role(Guid.NewGuid(), "Coordinador", "Coordinador", null, false, CreatedAt);
    }
}
