using AcademicSurveySystem.Domain.Common;
using AcademicSurveySystem.Domain.Identity.Entities;

namespace AcademicSurveySystem.UnitTests.Identity;

public sealed class PermissionTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedAt = new(2026, 1, 2, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CreatesValidPermission()
    {
        var permission = CreatePermission();

        Assert.Equal("encuestas.crear", permission.Code);
        Assert.Equal("Crear encuestas", permission.Name);
        Assert.Equal("encuestas", permission.Module);
    }

    [Fact]
    public void Constructor_NormalizesCode()
    {
        var permission = new Permission(
            Guid.NewGuid(),
            "  Encuestas.Crear  ",
            "Crear encuestas",
            "Encuestas",
            null,
            CreatedAt);

        Assert.Equal("encuestas.crear", permission.Code);
    }

    [Fact]
    public void Constructor_NormalizesModule()
    {
        var permission = new Permission(
            Guid.NewGuid(),
            "encuestas.crear",
            "Crear encuestas",
            "  Encuestas  ",
            null,
            CreatedAt);

        Assert.Equal("encuestas", permission.Module);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsEmptyCode(string code)
    {
        Assert.Throws<DomainException>(() =>
            new Permission(Guid.NewGuid(), code, "Crear encuestas", "encuestas", null, CreatedAt));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsEmptyName(string name)
    {
        Assert.Throws<DomainException>(() =>
            new Permission(Guid.NewGuid(), "encuestas.crear", name, "encuestas", null, CreatedAt));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsEmptyModule(string module)
    {
        Assert.Throws<DomainException>(() =>
            new Permission(Guid.NewGuid(), "encuestas.crear", "Crear encuestas", module, null, CreatedAt));
    }

    [Fact]
    public void Code_RemainsImmutable_WhenDetailsAreUpdated()
    {
        var permission = CreatePermission();

        permission.UpdateDetails("Editar encuestas", "administracion", "Descripcion", UpdatedAt);

        Assert.Equal("encuestas.crear", permission.Code);
    }

    [Fact]
    public void UpdateDetails_UpdatesAllowedData()
    {
        var permission = CreatePermission();

        permission.UpdateDetails("Editar encuestas", "Administracion", "Descripcion", UpdatedAt);

        Assert.Equal("Editar encuestas", permission.Name);
        Assert.Equal("administracion", permission.Module);
        Assert.Equal("Descripcion", permission.Description);
        Assert.Equal(UpdatedAt, permission.UpdatedAtUtc);
    }

    private static Permission CreatePermission()
    {
        return new Permission(
            Guid.NewGuid(),
            "encuestas.crear",
            "Crear encuestas",
            "encuestas",
            null,
            CreatedAt);
    }
}
