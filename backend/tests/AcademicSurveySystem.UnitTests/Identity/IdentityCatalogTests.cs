using AcademicSurveySystem.Domain.Identity;

namespace AcademicSurveySystem.UnitTests.Identity;

public sealed class IdentityCatalogTests
{
    [Fact]
    public void Roles_ContainsExactlyFourDefinitions()
    {
        Assert.Equal(4, IdentityCatalog.Roles.Count);
    }

    [Fact]
    public void Roles_HaveUniqueCodes()
    {
        Assert.Equal(
            IdentityCatalog.Roles.Count,
            IdentityCatalog.Roles.Select(role => role.Code).Distinct().Count());
    }

    [Fact]
    public void Roles_HaveUniqueNonEmptyIds()
    {
        Assert.DoesNotContain(IdentityCatalog.Roles, role => role.Id == Guid.Empty);
        Assert.Equal(
            IdentityCatalog.Roles.Count,
            IdentityCatalog.Roles.Select(role => role.Id).Distinct().Count());
    }

    [Fact]
    public void Permissions_ContainsExactlyFourteenDefinitions()
    {
        Assert.Equal(14, IdentityCatalog.Permissions.Count);
    }

    [Fact]
    public void Permissions_HaveUniqueCodes()
    {
        Assert.Equal(
            IdentityCatalog.Permissions.Count,
            IdentityCatalog.Permissions.Select(permission => permission.Code).Distinct().Count());
    }

    [Fact]
    public void Permissions_HaveUniqueNonEmptyIds()
    {
        Assert.DoesNotContain(IdentityCatalog.Permissions, permission => permission.Id == Guid.Empty);
        Assert.Equal(
            IdentityCatalog.Permissions.Count,
            IdentityCatalog.Permissions.Select(permission => permission.Id).Distinct().Count());
    }

    [Fact]
    public void Permissions_HaveExpectedModules()
    {
        var modules = IdentityCatalog.Permissions
            .Select(permission => permission.Module)
            .Distinct()
            .Order()
            .ToArray();

        Assert.Equal(
            ["academic", "audit", "identity", "reports", "results", "surveys"],
            modules);
    }

    [Fact]
    public void Codes_AreLowercase()
    {
        Assert.All(IdentityCatalog.Roles, role => Assert.Equal(role.Code.ToLowerInvariant(), role.Code));
        Assert.All(IdentityCatalog.Permissions, permission =>
        {
            Assert.Equal(permission.Code.ToLowerInvariant(), permission.Code);
            Assert.Equal(permission.Module.ToLowerInvariant(), permission.Module);
        });
    }

    [Fact]
    public void CatalogDate_IsUtc()
    {
        Assert.Equal(TimeSpan.Zero, IdentityCatalog.CatalogDateUtc.Offset);
    }

    [Fact]
    public void Roles_DoesNotContainStudent()
    {
        Assert.DoesNotContain(IdentityCatalog.Roles, role => role.Code == "student");
    }

    [Fact]
    public void Catalog_DoesNotContainUsersOrPasswords()
    {
        Assert.DoesNotContain(IdentityCatalog.Roles, role => ContainsPasswordText(role.Code, role.Name, role.Description));
        Assert.DoesNotContain(IdentityCatalog.Permissions, permission =>
            ContainsPasswordText(permission.Code, permission.Name, permission.Module, permission.Description));
    }

    private static bool ContainsPasswordText(params string[] values)
    {
        return values.Any(value => value.Contains("password", StringComparison.OrdinalIgnoreCase));
    }
}
