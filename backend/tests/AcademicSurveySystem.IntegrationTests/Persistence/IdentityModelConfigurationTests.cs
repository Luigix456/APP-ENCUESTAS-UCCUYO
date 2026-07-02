using AcademicSurveySystem.Domain.Identity;
using AcademicSurveySystem.Domain.Identity.Entities;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AcademicSurveySystem.IntegrationTests.Persistence;

public sealed class IdentityModelConfigurationTests
{
    [Fact]
    public void IdentityEntities_AreIncludedInModel()
    {
        using var context = CreateContext();
        var model = context.Model;

        Assert.NotNull(model.FindEntityType(typeof(User)));
        Assert.NotNull(model.FindEntityType(typeof(Role)));
        Assert.NotNull(model.FindEntityType(typeof(Permission)));
        Assert.NotNull(model.FindEntityType(typeof(UserRole)));
        Assert.NotNull(model.FindEntityType(typeof(RolePermission)));
    }

    [Fact]
    public void IdentityTables_HaveExpectedNames()
    {
        using var context = CreateContext();

        Assert.Equal("users", GetEntity<User>(context).GetTableName());
        Assert.Equal("roles", GetEntity<Role>(context).GetTableName());
        Assert.Equal("permissions", GetEntity<Permission>(context).GetTableName());
        Assert.Equal("user_roles", GetEntity<UserRole>(context).GetTableName());
        Assert.Equal("role_permissions", GetEntity<RolePermission>(context).GetTableName());
    }

    [Fact]
    public void UniqueIndexes_AreConfigured()
    {
        using var context = CreateContext();

        AssertUniqueIndex<User>(context, nameof(User.NormalizedEmail));
        AssertUniqueIndex<Role>(context, nameof(Role.Code));
        AssertUniqueIndex<Permission>(context, nameof(Permission.Code));
    }

    [Fact]
    public void JoinEntities_HaveCompositePrimaryKeys()
    {
        using var context = CreateContext();

        AssertCompositeKey<UserRole>(context, nameof(UserRole.UserId), nameof(UserRole.RoleId));
        AssertCompositeKey<RolePermission>(
            context,
            nameof(RolePermission.RoleId),
            nameof(RolePermission.PermissionId));
    }

    [Fact]
    public void JoinEntities_HaveExpectedRelationshipsAndDeleteBehavior()
    {
        using var context = CreateContext();

        AssertForeignKey<UserRole>(context, nameof(UserRole.UserId), DeleteBehavior.Cascade);
        AssertForeignKey<UserRole>(context, nameof(UserRole.RoleId), DeleteBehavior.Cascade);
        AssertForeignKey<RolePermission>(context, nameof(RolePermission.RoleId), DeleteBehavior.Cascade);
        AssertForeignKey<RolePermission>(context, nameof(RolePermission.PermissionId), DeleteBehavior.Cascade);
    }

    [Fact]
    public void IdentitySeedData_HasExpectedCounts()
    {
        using var context = CreateContext();

        Assert.Equal(4, GetSeedData<Role>(context).Count);
        Assert.Equal(14, GetSeedData<Permission>(context).Count);
        Assert.Equal(23, GetSeedData<RolePermission>(context).Count);
    }

    [Fact]
    public void IdentitySeedData_HasExpectedRolePermissionAssignments()
    {
        using var context = CreateContext();

        AssertRolePermissionCount(context, "administrator", 14);
        AssertRolePermissionCount(context, "surveyor", 3);
        AssertRolePermissionCount(context, "dean", 3);
        AssertRolePermissionCount(context, "career_director", 3);
    }

    [Fact]
    public void IdentitySeedData_DoesNotContainUserOrUserRoleRows()
    {
        using var context = CreateContext();

        Assert.Empty(GetSeedData<User>(context));
        Assert.Empty(GetSeedData<UserRole>(context));
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=academic_survey_db;Username=postgres;Password=postgres")
            .Options;

        return new ApplicationDbContext(options);
    }

    private static IEntityType GetEntity<TEntity>(ApplicationDbContext context)
    {
        return context.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"Entity {typeof(TEntity).Name} was not found.");
    }

    private static void AssertUniqueIndex<TEntity>(
        ApplicationDbContext context,
        params string[] propertyNames)
    {
        var entity = GetEntity<TEntity>(context);
        var index = entity.GetIndexes().SingleOrDefault(item =>
            item.Properties.Select(property => property.Name).SequenceEqual(propertyNames));

        Assert.NotNull(index);
        Assert.True(index.IsUnique);
    }

    private static void AssertCompositeKey<TEntity>(
        ApplicationDbContext context,
        params string[] propertyNames)
    {
        var key = GetEntity<TEntity>(context).FindPrimaryKey();

        Assert.NotNull(key);
        Assert.Equal(propertyNames, key.Properties.Select(property => property.Name));
    }

    private static void AssertForeignKey<TEntity>(
        ApplicationDbContext context,
        string propertyName,
        DeleteBehavior deleteBehavior)
    {
        var foreignKey = GetEntity<TEntity>(context)
            .GetForeignKeys()
            .SingleOrDefault(item =>
                item.Properties.Select(property => property.Name).SequenceEqual([propertyName]));

        Assert.NotNull(foreignKey);
        Assert.Equal(deleteBehavior, foreignKey.DeleteBehavior);
    }

    private static IReadOnlyList<IDictionary<string, object?>> GetSeedData<TEntity>(
        ApplicationDbContext context)
    {
        var designTimeModel = context.GetService<IDesignTimeModel>().Model;
        var entity = designTimeModel.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"Entity {typeof(TEntity).Name} was not found.");

        return entity.GetSeedData().ToArray();
    }

    private static void AssertRolePermissionCount(
        ApplicationDbContext context,
        string roleCode,
        int expectedPermissionCount)
    {
        var role = IdentityCatalog.GetRole(roleCode);
        var rolePermissions = GetSeedData<RolePermission>(context)
            .Where(seed => seed[nameof(RolePermission.RoleId)]?.Equals(role.Id) == true)
            .ToArray();

        Assert.Equal(expectedPermissionCount, rolePermissions.Length);
    }
}
