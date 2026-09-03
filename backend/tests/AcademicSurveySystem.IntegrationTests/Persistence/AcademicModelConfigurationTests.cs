using AcademicSurveySystem.Domain.Academic.Entities;
using AcademicSurveySystem.Domain.Academic.Enums;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AcademicSurveySystem.IntegrationTests.Persistence;

public sealed class AcademicModelConfigurationTests
{
    [Fact]
    public void AcademicEntities_AreIncludedInModel()
    {
        using var context = CreateContext();

        Assert.NotNull(context.Model.FindEntityType(typeof(Career)));
        Assert.NotNull(context.Model.FindEntityType(typeof(Subject)));
        Assert.NotNull(context.Model.FindEntityType(typeof(Teacher)));
        Assert.NotNull(context.Model.FindEntityType(typeof(AcademicCycle)));
        Assert.NotNull(context.Model.FindEntityType(typeof(TeacherSubjectAssignment)));
    }

    [Fact]
    public void AcademicTables_HaveExpectedNames()
    {
        using var context = CreateContext();

        Assert.Equal("careers", GetEntity<Career>(context).GetTableName());
        Assert.Equal("subjects", GetEntity<Subject>(context).GetTableName());
        Assert.Equal("teachers", GetEntity<Teacher>(context).GetTableName());
        Assert.Equal("academic_cycles", GetEntity<AcademicCycle>(context).GetTableName());
        Assert.Equal(
            "teacher_subject_assignments",
            GetEntity<TeacherSubjectAssignment>(context).GetTableName());
    }

    [Fact]
    public void UniqueIndexes_AreConfigured()
    {
        using var context = CreateContext();

        AssertUniqueIndex<Career>(context, nameof(Career.Code));
        AssertUniqueIndex<Subject>(context, nameof(Subject.CareerId), nameof(Subject.Code));
        AssertUniqueIndex<Teacher>(context, nameof(Teacher.NormalizedEmail));
        AssertUniqueIndex<AcademicCycle>(
            context,
            nameof(AcademicCycle.Year),
            nameof(AcademicCycle.Period));
        AssertUniqueIndex<TeacherSubjectAssignment>(
            context,
            nameof(TeacherSubjectAssignment.TeacherId),
            nameof(TeacherSubjectAssignment.SubjectId),
            nameof(TeacherSubjectAssignment.AcademicCycleId));
    }

    [Fact]
    public void Relationships_UseRestrictDeleteBehavior()
    {
        using var context = CreateContext();

        AssertForeignKey<Subject>(context, nameof(Subject.CareerId));
        AssertForeignKey<TeacherSubjectAssignment>(
            context,
            nameof(TeacherSubjectAssignment.TeacherId));
        AssertForeignKey<TeacherSubjectAssignment>(
            context,
            nameof(TeacherSubjectAssignment.SubjectId));
        AssertForeignKey<TeacherSubjectAssignment>(
            context,
            nameof(TeacherSubjectAssignment.AcademicCycleId));
    }

    [Fact]
    public void Enums_AreStoredAsStrings()
    {
        using var context = CreateContext();

        AssertStoresAsString<Career>(context, nameof(Career.Type));
        AssertStoresAsString<Subject>(context, nameof(Subject.Period));
        AssertStoresAsString<AcademicCycle>(context, nameof(AcademicCycle.Period));
    }

    [Fact]
    public void AcademicModel_DoesNotContainSeedData()
    {
        using var context = CreateContext();

        Assert.Empty(GetSeedData<Career>(context));
        Assert.Empty(GetSeedData<Subject>(context));
        Assert.Empty(GetSeedData<Teacher>(context));
        Assert.Empty(GetSeedData<AcademicCycle>(context));
        Assert.Empty(GetSeedData<TeacherSubjectAssignment>(context));
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

    private static void AssertForeignKey<TEntity>(
        ApplicationDbContext context,
        string propertyName)
    {
        var foreignKey = GetEntity<TEntity>(context)
            .GetForeignKeys()
            .SingleOrDefault(item =>
                item.Properties.Select(property => property.Name).SequenceEqual([propertyName]));

        Assert.NotNull(foreignKey);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }

    private static void AssertStoresAsString<TEntity>(
        ApplicationDbContext context,
        string propertyName)
    {
        var property = GetEntity<TEntity>(context).FindProperty(propertyName)
            ?? throw new InvalidOperationException($"Property {propertyName} was not found.");

        Assert.Equal(typeof(string), property.GetProviderClrType());
    }

    private static IReadOnlyList<IDictionary<string, object?>> GetSeedData<TEntity>(
        ApplicationDbContext context)
    {
        var designTimeModel = context.GetService<IDesignTimeModel>().Model;
        var entity = designTimeModel.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"Entity {typeof(TEntity).Name} was not found.");

        return entity.GetSeedData().ToArray();
    }
}
