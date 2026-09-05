using AcademicSurveySystem.Domain.Common;
using AcademicSurveySystem.Domain.Surveys.Entities;
using AcademicSurveySystem.Domain.Surveys.Enums;

namespace AcademicSurveySystem.UnitTests.Surveys;

public sealed class SurveySectionTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset UpdatedAtUtc =
        new(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CreatesSection_WhenValuesAreValid()
    {
        var section = CreateSection();

        Assert.Equal("Datos generales", section.Title);
        Assert.Equal("Descripcion", section.Description);
        Assert.Equal(1, section.Order);
        Assert.True(section.IsActive);
        Assert.Equal(CreatedAtUtc, section.CreatedAtUtc);
    }

    [Fact]
    public void Constructor_RejectsEmptySurveyId()
    {
        Assert.Throws<DomainException>(() => CreateSection(surveyId: Guid.Empty));
    }

    [Fact]
    public void Constructor_RejectsEmptyTitle()
    {
        Assert.Throws<DomainException>(() => CreateSection(title: " "));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsInvalidOrder(int order)
    {
        Assert.Throws<DomainException>(() => CreateSection(order: order));
    }

    [Fact]
    public void AddQuestion_AddsQuestionAndUpdatesTimestamp()
    {
        var section = CreateSection();
        var question = CreateQuestion(section.Id, order: 1);

        section.AddQuestion(question, UpdatedAtUtc);

        Assert.Single(section.Questions);
        Assert.Equal(UpdatedAtUtc, section.UpdatedAtUtc);
    }

    [Fact]
    public void AddQuestion_RejectsDuplicatedOrder()
    {
        var section = CreateSection();

        section.AddQuestion(CreateQuestion(section.Id, order: 1), UpdatedAtUtc);

        Assert.Throws<DomainException>(() =>
            section.AddQuestion(CreateQuestion(section.Id, order: 1), UpdatedAtUtc.AddDays(1)));
    }

    private static SurveySection CreateSection(
        Guid? surveyId = null,
        string title = "Datos generales",
        int order = 1)
    {
        return new SurveySection(
            Guid.NewGuid(),
            surveyId ?? Guid.NewGuid(),
            title,
            "Descripcion",
            order,
            CreatedAtUtc);
    }

    private static SurveyQuestion CreateQuestion(Guid sectionId, int order)
    {
        return new SurveyQuestion(
            Guid.NewGuid(),
            sectionId,
            "Pregunta",
            SurveyQuestionType.SingleChoice,
            isRequired: true,
            allowsComment: false,
            allowsOtherOption: false,
            order,
            CreatedAtUtc);
    }
}
