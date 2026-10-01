using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Application.Surveys;
using AcademicSurveySystem.Application.Surveys.Requests;
using AcademicSurveySystem.Domain.Surveys.Entities;
using AcademicSurveySystem.Domain.Surveys.Enums;
using AcademicSurveySystem.Infrastructure.Persistence;
using AcademicSurveySystem.Infrastructure.Surveys;
using Microsoft.EntityFrameworkCore;

namespace AcademicSurveySystem.UnitTests.Surveys;

public sealed class SurveyTemplateServiceUpdateOptionTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset UpdatedAtUtc =
        new(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task UpdateOptionAsync_UpdatesAndReturnsDetail()
    {
        using var context = CreateContext();
        var fixture = SeedSurvey(context);
        var service = new SurveyTemplateService(context);

        var result = await service.UpdateOptionAsync(
            fixture.Survey.Id,
            fixture.Section.Id,
            fixture.ChoiceQuestion.Id,
            fixture.OptionA.Id,
            new UpdateSurveyQuestionOptionRequest(" Excelente ", " EXCELLENT ", 3),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        var question = result.Value!.Sections.Single().Questions.Single(item => item.Id == fixture.ChoiceQuestion.Id);
        var option = question.Options.Single(item => item.Id == fixture.OptionA.Id);
        Assert.Equal("Excelente", option.Text);
        Assert.Equal("excellent", option.Value);
        Assert.Equal(3, option.Order);

        var persisted = await context.SurveyQuestionOptions.SingleAsync(item => item.Id == fixture.OptionA.Id);
        Assert.Equal("Excelente", persisted.Text);
        Assert.Equal("excellent", persisted.Value);
        Assert.Equal(3, persisted.Order);
    }

    [Fact]
    public async Task UpdateOptionAsync_ReturnsNotFound_WhenOptionBelongsToAnotherQuestion()
    {
        using var context = CreateContext();
        var fixture = SeedSurvey(context);
        var service = new SurveyTemplateService(context);

        var result = await service.UpdateOptionAsync(
            fixture.Survey.Id,
            fixture.Section.Id,
            fixture.ChoiceQuestion.Id,
            fixture.OtherQuestionOption.Id,
            new UpdateSurveyQuestionOptionRequest("Excelente", "excellent", 1),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task UpdateOptionAsync_ReturnsNotFound_WhenQuestionBelongsToAnotherSection()
    {
        using var context = CreateContext();
        var fixture = SeedSurvey(context, includeSecondSection: true);
        var service = new SurveyTemplateService(context);

        var result = await service.UpdateOptionAsync(
            fixture.Survey.Id,
            fixture.SecondSection!.Id,
            fixture.ChoiceQuestion.Id,
            fixture.OptionA.Id,
            new UpdateSurveyQuestionOptionRequest("Excelente", "excellent", 1),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task UpdateOptionAsync_ReturnsNotFound_WhenSectionBelongsToAnotherSurvey()
    {
        using var context = CreateContext();
        var fixture = SeedSurvey(context);
        var otherFixture = SeedSurvey(context, title: "Otra encuesta");
        var service = new SurveyTemplateService(context);

        var result = await service.UpdateOptionAsync(
            fixture.Survey.Id,
            otherFixture.Section.Id,
            fixture.ChoiceQuestion.Id,
            fixture.OptionA.Id,
            new UpdateSurveyQuestionOptionRequest("Excelente", "excellent", 1),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task UpdateOptionAsync_ReturnsConflict_WhenOrderAlreadyExists()
    {
        using var context = CreateContext();
        var fixture = SeedSurvey(context);
        var service = new SurveyTemplateService(context);

        var result = await service.UpdateOptionAsync(
            fixture.Survey.Id,
            fixture.Section.Id,
            fixture.ChoiceQuestion.Id,
            fixture.OptionA.Id,
            new UpdateSurveyQuestionOptionRequest("Excelente", "excellent", fixture.OptionB.Order),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task UpdateOptionAsync_ReturnsValidation_WhenQuestionTypeDoesNotAllowOptions()
    {
        using var context = CreateContext();
        var fixture = SeedSurvey(context, makeChoiceQuestionIncompatible: true);
        var service = new SurveyTemplateService(context);

        var result = await service.UpdateOptionAsync(
            fixture.Survey.Id,
            fixture.Section.Id,
            fixture.ChoiceQuestion.Id,
            fixture.OptionA.Id,
            new UpdateSurveyQuestionOptionRequest("Excelente", "excellent", 1),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Validation, result.Status);
    }

    [Fact]
    public async Task UpdateOptionAsync_ReturnsSurveyNotEditable_WhenSurveyIsPublished_AndPersistsOriginalOption()
    {
        using var context = CreateContext();
        var fixture = SeedSurvey(context);
        fixture.Survey.Publish(UpdatedAtUtc.AddDays(1));
        await context.SaveChangesAsync();
        var service = new SurveyTemplateService(context);
        var originalText = fixture.OptionA.Text;
        var originalValue = fixture.OptionA.Value;
        var originalOrder = fixture.OptionA.Order;

        var result = await service.UpdateOptionAsync(
            fixture.Survey.Id,
            fixture.Section.Id,
            fixture.ChoiceQuestion.Id,
            fixture.OptionA.Id,
            new UpdateSurveyQuestionOptionRequest("Excelente", "excellent", 3),
            CancellationToken.None);

        AssertSurveyNotEditable(result);

        var persisted = await context.SurveyQuestionOptions.SingleAsync(item => item.Id == fixture.OptionA.Id);
        Assert.Equal(originalText, persisted.Text);
        Assert.Equal(originalValue, persisted.Value);
        Assert.Equal(originalOrder, persisted.Order);
    }

    [Fact]
    public async Task UpdateMatrixRowAsync_UpdatesAndReturnsDetail()
    {
        using var context = CreateContext();
        var fixture = SeedSurvey(context);
        var service = new SurveyTemplateService(context);

        var result = await service.UpdateMatrixRowAsync(
            fixture.Survey.Id,
            fixture.Section.Id,
            fixture.MatrixQuestion.Id,
            fixture.MatrixRowA.Id,
            new UpdateSurveyMatrixRowRequest(" Organizacion ", 3),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        var question = result.Value!.Sections.Single().Questions.Single(item => item.Id == fixture.MatrixQuestion.Id);
        var matrixRow = question.MatrixRows.Single(item => item.Id == fixture.MatrixRowA.Id);
        Assert.Equal("Organizacion", matrixRow.Text);
        Assert.Equal(3, matrixRow.Order);

        var persisted = await context.SurveyMatrixRows.SingleAsync(item => item.Id == fixture.MatrixRowA.Id);
        Assert.Equal("Organizacion", persisted.Text);
        Assert.Equal(3, persisted.Order);
    }

    [Fact]
    public async Task UpdateMatrixRowAsync_ReturnsNotFound_WhenRowBelongsToAnotherQuestion()
    {
        using var context = CreateContext();
        var fixture = SeedSurvey(context, includeSecondMatrixQuestion: true);
        var service = new SurveyTemplateService(context);

        var result = await service.UpdateMatrixRowAsync(
            fixture.Survey.Id,
            fixture.Section.Id,
            fixture.MatrixQuestion.Id,
            fixture.OtherMatrixRow!.Id,
            new UpdateSurveyMatrixRowRequest("Organizacion", 1),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task UpdateMatrixRowAsync_ReturnsConflict_WhenOrderAlreadyExists()
    {
        using var context = CreateContext();
        var fixture = SeedSurvey(context);
        var service = new SurveyTemplateService(context);

        var result = await service.UpdateMatrixRowAsync(
            fixture.Survey.Id,
            fixture.Section.Id,
            fixture.MatrixQuestion.Id,
            fixture.MatrixRowA.Id,
            new UpdateSurveyMatrixRowRequest("Organizacion", fixture.MatrixRowB.Order),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task UpdateMatrixRowAsync_ReturnsValidation_WhenQuestionTypeIsNotMatrix()
    {
        using var context = CreateContext();
        var fixture = SeedSurvey(context, makeMatrixQuestionIncompatible: true);
        var service = new SurveyTemplateService(context);

        var result = await service.UpdateMatrixRowAsync(
            fixture.Survey.Id,
            fixture.Section.Id,
            fixture.MatrixQuestion.Id,
            fixture.MatrixRowA.Id,
            new UpdateSurveyMatrixRowRequest("Organizacion", 1),
            CancellationToken.None);

        Assert.Equal(ApplicationResultStatus.Validation, result.Status);
    }

    [Fact]
    public async Task UpdateMatrixRowAsync_ReturnsSurveyNotEditable_WhenSurveyIsPublished_AndPersistsOriginalRow()
    {
        using var context = CreateContext();
        var fixture = SeedSurvey(context);
        fixture.Survey.Publish(UpdatedAtUtc.AddDays(1));
        await context.SaveChangesAsync();
        var service = new SurveyTemplateService(context);
        var originalText = fixture.MatrixRowA.Text;
        var originalOrder = fixture.MatrixRowA.Order;

        var result = await service.UpdateMatrixRowAsync(
            fixture.Survey.Id,
            fixture.Section.Id,
            fixture.MatrixQuestion.Id,
            fixture.MatrixRowA.Id,
            new UpdateSurveyMatrixRowRequest("Organizacion", 3),
            CancellationToken.None);

        AssertSurveyNotEditable(result);

        var persisted = await context.SurveyMatrixRows.SingleAsync(item => item.Id == fixture.MatrixRowA.Id);
        Assert.Equal(originalText, persisted.Text);
        Assert.Equal(originalOrder, persisted.Order);
    }

    [Theory]
    [InlineData(nameof(ISurveyTemplateService.UpdateSurveyAsync))]
    [InlineData(nameof(ISurveyTemplateService.AddSectionAsync))]
    [InlineData(nameof(ISurveyTemplateService.UpdateSectionAsync))]
    [InlineData(nameof(ISurveyTemplateService.ActivateSectionAsync))]
    [InlineData(nameof(ISurveyTemplateService.DeactivateSectionAsync))]
    [InlineData(nameof(ISurveyTemplateService.AddQuestionAsync))]
    [InlineData(nameof(ISurveyTemplateService.UpdateQuestionAsync))]
    [InlineData(nameof(ISurveyTemplateService.ActivateQuestionAsync))]
    [InlineData(nameof(ISurveyTemplateService.DeactivateQuestionAsync))]
    [InlineData(nameof(ISurveyTemplateService.AddOptionAsync))]
    [InlineData(nameof(ISurveyTemplateService.UpdateOptionAsync))]
    [InlineData(nameof(ISurveyTemplateService.ActivateOptionAsync))]
    [InlineData(nameof(ISurveyTemplateService.DeactivateOptionAsync))]
    [InlineData(nameof(ISurveyTemplateService.AddMatrixRowAsync))]
    [InlineData(nameof(ISurveyTemplateService.UpdateMatrixRowAsync))]
    [InlineData(nameof(ISurveyTemplateService.ActivateMatrixRowAsync))]
    [InlineData(nameof(ISurveyTemplateService.DeactivateMatrixRowAsync))]
    public async Task StructuralOperations_ReturnSurveyNotEditable_WhenSurveyIsPublished(string operation)
    {
        using var context = CreateContext();
        var fixture = SeedSurvey(context);
        fixture.Survey.Publish(UpdatedAtUtc.AddDays(1));
        await context.SaveChangesAsync();
        var service = new SurveyTemplateService(context);

        var outcome = await ExecuteStructuralOperationAsync(service, fixture, operation);

        AssertSurveyNotEditable(outcome);
    }

    [Theory]
    [InlineData(nameof(ISurveyTemplateService.UpdateSurveyAsync))]
    [InlineData(nameof(ISurveyTemplateService.AddQuestionAsync))]
    [InlineData(nameof(ISurveyTemplateService.UpdateOptionAsync))]
    [InlineData(nameof(ISurveyTemplateService.UpdateMatrixRowAsync))]
    [InlineData(nameof(ISurveyTemplateService.DeactivateQuestionAsync))]
    public async Task StructuralOperations_ReturnSurveyNotEditable_WhenSurveyIsArchived(string operation)
    {
        using var context = CreateContext();
        var fixture = SeedSurvey(context);
        fixture.Survey.Publish(UpdatedAtUtc.AddDays(1));
        fixture.Survey.Archive(UpdatedAtUtc.AddDays(2));
        await context.SaveChangesAsync();
        var service = new SurveyTemplateService(context);

        var outcome = await ExecuteStructuralOperationAsync(service, fixture, operation);

        AssertSurveyNotEditable(outcome);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static Fixture SeedSurvey(
        ApplicationDbContext context,
        string title = "Encuesta docente",
        bool includeSecondSection = false,
        bool includeSecondMatrixQuestion = false,
        bool makeChoiceQuestionIncompatible = false,
        bool makeMatrixQuestionIncompatible = false)
    {
        var survey = new Survey(
            Guid.NewGuid(),
            Guid.NewGuid(),
            title,
            "Descripcion",
            SurveyTarget.Student,
            CreatedAtUtc);

        var section = new SurveySection(
            Guid.NewGuid(),
            survey.Id,
            "Seccion",
            null,
            order: 1,
            CreatedAtUtc);

        var choiceQuestion = new SurveyQuestion(
            Guid.NewGuid(),
            section.Id,
            "Pregunta de opcion",
            SurveyQuestionType.SingleChoice,
            isRequired: true,
            allowsComment: false,
            allowsOtherOption: false,
            order: 1,
            CreatedAtUtc);

        var optionA = new SurveyQuestionOption(
            Guid.NewGuid(),
            choiceQuestion.Id,
            "Bueno",
            "good",
            order: 1,
            CreatedAtUtc);

        var optionB = new SurveyQuestionOption(
            Guid.NewGuid(),
            choiceQuestion.Id,
            "Muy bueno",
            "very-good",
            order: 2,
            CreatedAtUtc);

        choiceQuestion.AddOption(optionA, UpdatedAtUtc);
        choiceQuestion.AddOption(optionB, UpdatedAtUtc);

        if (makeChoiceQuestionIncompatible)
        {
            optionA.Deactivate(UpdatedAtUtc.AddMinutes(1));
            optionB.Deactivate(UpdatedAtUtc.AddMinutes(1));
            choiceQuestion.Update(
                choiceQuestion.Text,
                SurveyQuestionType.ShortText,
                choiceQuestion.IsRequired,
                choiceQuestion.AllowsComment,
                allowsOtherOption: false,
                choiceQuestion.Order,
                UpdatedAtUtc.AddMinutes(2));
        }

        var otherQuestion = new SurveyQuestion(
            Guid.NewGuid(),
            section.Id,
            "Otra pregunta de opcion",
            SurveyQuestionType.SingleChoice,
            isRequired: true,
            allowsComment: false,
            allowsOtherOption: false,
            order: 2,
            CreatedAtUtc);

        var otherQuestionOption = new SurveyQuestionOption(
            Guid.NewGuid(),
            otherQuestion.Id,
            "Regular",
            "regular",
            order: 1,
            CreatedAtUtc);

        var otherQuestionOptionB = new SurveyQuestionOption(
            Guid.NewGuid(),
            otherQuestion.Id,
            "Bueno",
            "good",
            order: 2,
            CreatedAtUtc);

        otherQuestion.AddOption(otherQuestionOption, UpdatedAtUtc);
        otherQuestion.AddOption(otherQuestionOptionB, UpdatedAtUtc);

        var matrixQuestion = new SurveyQuestion(
            Guid.NewGuid(),
            section.Id,
            "Pregunta matriz",
            SurveyQuestionType.MatrixSingleChoice,
            isRequired: true,
            allowsComment: false,
            allowsOtherOption: false,
            order: 3,
            CreatedAtUtc);

        var matrixOptionA = new SurveyQuestionOption(
            Guid.NewGuid(),
            matrixQuestion.Id,
            "Bueno",
            "good",
            order: 1,
            CreatedAtUtc);

        var matrixOptionB = new SurveyQuestionOption(
            Guid.NewGuid(),
            matrixQuestion.Id,
            "Excelente",
            "excellent",
            order: 2,
            CreatedAtUtc);

        var matrixRowA = new SurveyMatrixRow(
            Guid.NewGuid(),
            matrixQuestion.Id,
            "Claridad",
            order: 1,
            CreatedAtUtc);

        var matrixRowB = new SurveyMatrixRow(
            Guid.NewGuid(),
            matrixQuestion.Id,
            "Materiales",
            order: 2,
            CreatedAtUtc);

        matrixQuestion.AddOption(matrixOptionA, UpdatedAtUtc);
        matrixQuestion.AddOption(matrixOptionB, UpdatedAtUtc);
        matrixQuestion.AddMatrixRow(matrixRowA, UpdatedAtUtc);
        matrixQuestion.AddMatrixRow(matrixRowB, UpdatedAtUtc);

        if (makeMatrixQuestionIncompatible)
        {
            matrixRowA.Deactivate(UpdatedAtUtc.AddMinutes(1));
            matrixRowB.Deactivate(UpdatedAtUtc.AddMinutes(1));
            matrixOptionA.Deactivate(UpdatedAtUtc.AddMinutes(1));
            matrixOptionB.Deactivate(UpdatedAtUtc.AddMinutes(1));
            matrixQuestion.Update(
                matrixQuestion.Text,
                SurveyQuestionType.ShortText,
                matrixQuestion.IsRequired,
                matrixQuestion.AllowsComment,
                allowsOtherOption: false,
                matrixQuestion.Order,
                UpdatedAtUtc.AddMinutes(2));
        }

        section.AddQuestion(choiceQuestion, UpdatedAtUtc);
        section.AddQuestion(otherQuestion, UpdatedAtUtc);
        section.AddQuestion(matrixQuestion, UpdatedAtUtc);

        SurveyMatrixRow? otherMatrixRow = null;

        if (includeSecondMatrixQuestion)
        {
            var otherMatrixQuestion = new SurveyQuestion(
                Guid.NewGuid(),
                section.Id,
                "Otra matriz",
                SurveyQuestionType.MatrixSingleChoice,
                isRequired: true,
                allowsComment: false,
                allowsOtherOption: false,
                order: 4,
                CreatedAtUtc);

            otherMatrixQuestion.AddOption(
                new SurveyQuestionOption(Guid.NewGuid(), otherMatrixQuestion.Id, "A", "a", 1, CreatedAtUtc),
                UpdatedAtUtc);
            otherMatrixQuestion.AddOption(
                new SurveyQuestionOption(Guid.NewGuid(), otherMatrixQuestion.Id, "B", "b", 2, CreatedAtUtc),
                UpdatedAtUtc);

            otherMatrixRow = new SurveyMatrixRow(
                Guid.NewGuid(),
                otherMatrixQuestion.Id,
                "Otra fila",
                order: 1,
                CreatedAtUtc);

            otherMatrixQuestion.AddMatrixRow(otherMatrixRow, UpdatedAtUtc);
            section.AddQuestion(otherMatrixQuestion, UpdatedAtUtc);
        }

        SurveySection? secondSection = null;

        if (includeSecondSection)
        {
            secondSection = new SurveySection(
                Guid.NewGuid(),
                survey.Id,
                "Otra seccion",
                null,
                order: 2,
                CreatedAtUtc);

            survey.AddSection(secondSection, UpdatedAtUtc);
        }

        survey.AddSection(section, UpdatedAtUtc);
        context.Surveys.Add(survey);
        context.SaveChanges();

        return new Fixture(
            survey,
            section,
            secondSection,
            choiceQuestion,
            otherQuestionOption,
            optionA,
            optionB,
            matrixQuestion,
            matrixRowA,
            matrixRowB,
            otherMatrixRow);
    }

    private sealed record Fixture(
        Survey Survey,
        SurveySection Section,
        SurveySection? SecondSection,
        SurveyQuestion ChoiceQuestion,
        SurveyQuestionOption OtherQuestionOption,
        SurveyQuestionOption OptionA,
        SurveyQuestionOption OptionB,
        SurveyQuestion MatrixQuestion,
        SurveyMatrixRow MatrixRowA,
        SurveyMatrixRow MatrixRowB,
        SurveyMatrixRow? OtherMatrixRow);

    private static async Task<OperationOutcome> ExecuteStructuralOperationAsync(
        ISurveyTemplateService service,
        Fixture fixture,
        string operation)
    {
        return operation switch
        {
            nameof(ISurveyTemplateService.UpdateSurveyAsync) => ToOutcome(await service.UpdateSurveyAsync(
                fixture.Survey.Id,
                new UpdateSurveyRequest("Encuesta actualizada", null, "Student", IsAnonymous: true),
                CancellationToken.None)),

            nameof(ISurveyTemplateService.AddSectionAsync) => ToOutcome(await service.AddSectionAsync(
                fixture.Survey.Id,
                new CreateSurveySectionRequest("Nueva seccion", null, Order: 5),
                CancellationToken.None)),

            nameof(ISurveyTemplateService.UpdateSectionAsync) => ToOutcome(await service.UpdateSectionAsync(
                fixture.Survey.Id,
                fixture.Section.Id,
                new UpdateSurveySectionRequest("Seccion actualizada", null, Order: 1),
                CancellationToken.None)),

            nameof(ISurveyTemplateService.ActivateSectionAsync) => ToOutcome(await service.ActivateSectionAsync(
                fixture.Survey.Id,
                fixture.Section.Id,
                CancellationToken.None)),

            nameof(ISurveyTemplateService.DeactivateSectionAsync) => ToOutcome(await service.DeactivateSectionAsync(
                fixture.Survey.Id,
                fixture.Section.Id,
                CancellationToken.None)),

            nameof(ISurveyTemplateService.AddQuestionAsync) => ToOutcome(await service.AddQuestionAsync(
                fixture.Survey.Id,
                fixture.Section.Id,
                new CreateSurveyQuestionRequest(
                    "Nueva pregunta",
                    "ShortText",
                    IsRequired: true,
                    AllowsComment: false,
                    AllowsOtherOption: false,
                    Order: 5),
                CancellationToken.None)),

            nameof(ISurveyTemplateService.UpdateQuestionAsync) => ToOutcome(await service.UpdateQuestionAsync(
                fixture.Survey.Id,
                fixture.Section.Id,
                fixture.ChoiceQuestion.Id,
                new UpdateSurveyQuestionRequest(
                    "Pregunta actualizada",
                    "SingleChoice",
                    IsRequired: true,
                    AllowsComment: false,
                    AllowsOtherOption: false,
                    Order: 1),
                CancellationToken.None)),

            nameof(ISurveyTemplateService.ActivateQuestionAsync) => ToOutcome(await service.ActivateQuestionAsync(
                fixture.Survey.Id,
                fixture.Section.Id,
                fixture.ChoiceQuestion.Id,
                CancellationToken.None)),

            nameof(ISurveyTemplateService.DeactivateQuestionAsync) => ToOutcome(await service.DeactivateQuestionAsync(
                fixture.Survey.Id,
                fixture.Section.Id,
                fixture.ChoiceQuestion.Id,
                CancellationToken.None)),

            nameof(ISurveyTemplateService.AddOptionAsync) => ToOutcome(await service.AddOptionAsync(
                fixture.Survey.Id,
                fixture.Section.Id,
                fixture.ChoiceQuestion.Id,
                new CreateSurveyQuestionOptionRequest("Nueva opcion", "new", Order: 5),
                CancellationToken.None)),

            nameof(ISurveyTemplateService.UpdateOptionAsync) => ToOutcome(await service.UpdateOptionAsync(
                fixture.Survey.Id,
                fixture.Section.Id,
                fixture.ChoiceQuestion.Id,
                fixture.OptionA.Id,
                new UpdateSurveyQuestionOptionRequest("Opcion actualizada", "updated", Order: 1),
                CancellationToken.None)),

            nameof(ISurveyTemplateService.ActivateOptionAsync) => ToOutcome(await service.ActivateOptionAsync(
                fixture.Survey.Id,
                fixture.Section.Id,
                fixture.ChoiceQuestion.Id,
                fixture.OptionA.Id,
                CancellationToken.None)),

            nameof(ISurveyTemplateService.DeactivateOptionAsync) => ToOutcome(await service.DeactivateOptionAsync(
                fixture.Survey.Id,
                fixture.Section.Id,
                fixture.ChoiceQuestion.Id,
                fixture.OptionA.Id,
                CancellationToken.None)),

            nameof(ISurveyTemplateService.AddMatrixRowAsync) => ToOutcome(await service.AddMatrixRowAsync(
                fixture.Survey.Id,
                fixture.Section.Id,
                fixture.MatrixQuestion.Id,
                new CreateSurveyMatrixRowRequest("Nueva fila", Order: 5),
                CancellationToken.None)),

            nameof(ISurveyTemplateService.UpdateMatrixRowAsync) => ToOutcome(await service.UpdateMatrixRowAsync(
                fixture.Survey.Id,
                fixture.Section.Id,
                fixture.MatrixQuestion.Id,
                fixture.MatrixRowA.Id,
                new UpdateSurveyMatrixRowRequest("Fila actualizada", Order: 1),
                CancellationToken.None)),

            nameof(ISurveyTemplateService.ActivateMatrixRowAsync) => ToOutcome(await service.ActivateMatrixRowAsync(
                fixture.Survey.Id,
                fixture.Section.Id,
                fixture.MatrixQuestion.Id,
                fixture.MatrixRowA.Id,
                CancellationToken.None)),

            nameof(ISurveyTemplateService.DeactivateMatrixRowAsync) => ToOutcome(await service.DeactivateMatrixRowAsync(
                fixture.Survey.Id,
                fixture.Section.Id,
                fixture.MatrixQuestion.Id,
                fixture.MatrixRowA.Id,
                CancellationToken.None)),

            _ => throw new InvalidOperationException($"Unknown operation {operation}.")
        };
    }

    private static OperationOutcome ToOutcome(ApplicationResult result)
    {
        return new OperationOutcome(result.Status, result.Errors);
    }

    private static OperationOutcome ToOutcome<T>(ApplicationResult<T> result)
    {
        return new OperationOutcome(result.Status, result.Errors);
    }

    private static void AssertSurveyNotEditable(ApplicationResult result)
    {
        AssertSurveyNotEditable(new OperationOutcome(result.Status, result.Errors));
    }

    private static void AssertSurveyNotEditable<T>(ApplicationResult<T> result)
    {
        AssertSurveyNotEditable(new OperationOutcome(result.Status, result.Errors));
    }

    private static void AssertSurveyNotEditable(OperationOutcome outcome)
    {
        Assert.Equal(ApplicationResultStatus.Validation, outcome.Status);
        Assert.Contains(outcome.Errors, error => error.Code == "Survey.NotEditable");
    }

    private sealed record OperationOutcome(
        ApplicationResultStatus Status,
        IReadOnlyCollection<ApplicationError> Errors);
}
