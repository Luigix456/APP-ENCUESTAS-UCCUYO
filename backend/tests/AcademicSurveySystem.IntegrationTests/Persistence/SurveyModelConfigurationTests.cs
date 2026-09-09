using AcademicSurveySystem.Domain.Identity.Entities;
using AcademicSurveySystem.Domain.Surveys.Entities;
using AcademicSurveySystem.Domain.Surveys.Enums;
using AcademicSurveySystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AcademicSurveySystem.IntegrationTests.Persistence;

public sealed class SurveyModelConfigurationTests
{
    [Fact]
    public void SurveyEntities_AreIncludedInModel()
    {
        using var context = CreateContext();

        Assert.NotNull(context.Model.FindEntityType(typeof(Survey)));
        Assert.NotNull(context.Model.FindEntityType(typeof(SurveySection)));
        Assert.NotNull(context.Model.FindEntityType(typeof(SurveyQuestion)));
        Assert.NotNull(context.Model.FindEntityType(typeof(SurveyQuestionOption)));
        Assert.NotNull(context.Model.FindEntityType(typeof(SurveyMatrixRow)));
        Assert.NotNull(context.Model.FindEntityType(typeof(SurveyAssignment)));
    }

    [Fact]
    public void SurveyTables_HaveExpectedNames()
    {
        using var context = CreateContext();

        Assert.Equal("surveys", GetEntity<Survey>(context).GetTableName());
        Assert.Equal("survey_sections", GetEntity<SurveySection>(context).GetTableName());
        Assert.Equal("survey_questions", GetEntity<SurveyQuestion>(context).GetTableName());
        Assert.Equal("survey_question_options", GetEntity<SurveyQuestionOption>(context).GetTableName());
        Assert.Equal("survey_matrix_rows", GetEntity<SurveyMatrixRow>(context).GetTableName());
        Assert.Equal("survey_assignments", GetEntity<SurveyAssignment>(context).GetTableName());
    }

    [Fact]
    public void Enums_AreStoredAsStrings()
    {
        using var context = CreateContext();

        AssertStoresAsString<Survey>(context, nameof(Survey.Target));
        AssertStoresAsString<Survey>(context, nameof(Survey.Status));
        AssertStoresAsString<SurveyQuestion>(context, nameof(SurveyQuestion.Type));
    }

    [Fact]
    public void SurveyToUser_UsesRestrictDeleteBehavior()
    {
        using var context = CreateContext();

        var foreignKey = GetEntity<Survey>(context)
            .GetForeignKeys()
            .SingleOrDefault(item =>
                item.PrincipalEntityType.ClrType == typeof(User)
                && item.Properties.Select(property => property.Name)
                    .SequenceEqual([nameof(Survey.CreatedByUserId)]));

        Assert.NotNull(foreignKey);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void InternalRelationships_UseCascadeDeleteBehavior()
    {
        using var context = CreateContext();

        AssertForeignKey<SurveySection>(context, nameof(SurveySection.SurveyId), DeleteBehavior.Cascade);
        AssertForeignKey<SurveyQuestion>(context, nameof(SurveyQuestion.SurveySectionId), DeleteBehavior.Cascade);
        AssertForeignKey<SurveyQuestionOption>(
            context,
            nameof(SurveyQuestionOption.SurveyQuestionId),
            DeleteBehavior.Cascade);
        AssertForeignKey<SurveyMatrixRow>(
            context,
            nameof(SurveyMatrixRow.SurveyQuestionId),
            DeleteBehavior.Cascade);
    }

    [Fact]
    public void SurveyAssignmentRelationships_UseRestrictDeleteBehavior()
    {
        using var context = CreateContext();

        AssertForeignKey<SurveyAssignment>(context, nameof(SurveyAssignment.SurveyId), DeleteBehavior.Restrict);
        AssertForeignKey<SurveyAssignment>(context, nameof(SurveyAssignment.CareerId), DeleteBehavior.Restrict);
        AssertForeignKey<SurveyAssignment>(context, nameof(SurveyAssignment.SubjectId), DeleteBehavior.Restrict);
        AssertForeignKey<SurveyAssignment>(
            context,
            nameof(SurveyAssignment.AcademicCycleId),
            DeleteBehavior.Restrict);
        AssertForeignKey<SurveyAssignment>(
            context,
            nameof(SurveyAssignment.TeacherSubjectAssignmentId),
            DeleteBehavior.Restrict);
    }

    [Fact]
    public void UniqueOrderIndexes_AreConfigured()
    {
        using var context = CreateContext();

        AssertUniqueIndex<SurveySection>(context, nameof(SurveySection.SurveyId), nameof(SurveySection.Order));
        AssertUniqueIndex<SurveyQuestion>(
            context,
            nameof(SurveyQuestion.SurveySectionId),
            nameof(SurveyQuestion.Order));
        AssertUniqueIndex<SurveyQuestionOption>(
            context,
            nameof(SurveyQuestionOption.SurveyQuestionId),
            nameof(SurveyQuestionOption.Order));
        AssertUniqueIndex<SurveyMatrixRow>(
            context,
            nameof(SurveyMatrixRow.SurveyQuestionId),
            nameof(SurveyMatrixRow.Order));
    }

    [Fact]
    public void SurveyAssignmentIndexes_AreConfigured()
    {
        using var context = CreateContext();

        AssertIndex<SurveyAssignment>(context, nameof(SurveyAssignment.SurveyId));
        AssertIndex<SurveyAssignment>(context, nameof(SurveyAssignment.CareerId));
        AssertIndex<SurveyAssignment>(context, nameof(SurveyAssignment.SubjectId));
        AssertIndex<SurveyAssignment>(context, nameof(SurveyAssignment.AcademicCycleId));
        AssertIndex<SurveyAssignment>(context, nameof(SurveyAssignment.TeacherSubjectAssignmentId));
        AssertUniqueIndex<SurveyAssignment>(
            context,
            nameof(SurveyAssignment.SurveyId),
            nameof(SurveyAssignment.CareerId),
            nameof(SurveyAssignment.SubjectId),
            nameof(SurveyAssignment.AcademicCycleId),
            nameof(SurveyAssignment.TeacherSubjectAssignmentId));
    }

    [Fact]
    public void SurveyModel_DoesNotContainSeedData()
    {
        using var context = CreateContext();

        Assert.Empty(GetSeedData<Survey>(context));
        Assert.Empty(GetSeedData<SurveySection>(context));
        Assert.Empty(GetSeedData<SurveyQuestion>(context));
        Assert.Empty(GetSeedData<SurveyQuestionOption>(context));
        Assert.Empty(GetSeedData<SurveyMatrixRow>(context));
        Assert.Empty(GetSeedData<SurveyAssignment>(context));
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

    private static void AssertStoresAsString<TEntity>(
        ApplicationDbContext context,
        string propertyName)
    {
        var property = GetEntity<TEntity>(context).FindProperty(propertyName)
            ?? throw new InvalidOperationException($"Property {propertyName} was not found.");

        Assert.Equal(typeof(string), property.GetProviderClrType());
    }

    private static void AssertForeignKey<TEntity>(
        ApplicationDbContext context,
        string propertyName,
        DeleteBehavior expectedDeleteBehavior)
    {
        var foreignKey = GetEntity<TEntity>(context)
            .GetForeignKeys()
            .SingleOrDefault(item =>
                item.Properties.Select(property => property.Name).SequenceEqual([propertyName]));

        Assert.NotNull(foreignKey);
        Assert.Equal(expectedDeleteBehavior, foreignKey.DeleteBehavior);
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

    private static void AssertIndex<TEntity>(
        ApplicationDbContext context,
        params string[] propertyNames)
    {
        var entity = GetEntity<TEntity>(context);
        var index = entity.GetIndexes().SingleOrDefault(item =>
            item.Properties.Select(property => property.Name).SequenceEqual(propertyNames));

        Assert.NotNull(index);
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
