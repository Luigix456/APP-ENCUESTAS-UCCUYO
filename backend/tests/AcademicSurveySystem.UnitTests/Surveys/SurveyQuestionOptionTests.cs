using AcademicSurveySystem.Domain.Common;
using AcademicSurveySystem.Domain.Surveys.Entities;

namespace AcademicSurveySystem.UnitTests.Surveys;

public sealed class SurveyQuestionOptionTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CreatesOption_WhenValuesAreValid()
    {
        var option = CreateOption();

        Assert.Equal("Opcion", option.Text);
        Assert.Equal("muy-bueno", option.Value);
        Assert.Equal(1, option.Order);
        Assert.True(option.IsActive);
        Assert.Equal(CreatedAtUtc, option.CreatedAtUtc);
    }

    [Fact]
    public void Constructor_NormalizesValue()
    {
        var option = CreateOption(value: " MUY-BUENO ");

        Assert.Equal("muy-bueno", option.Value);
    }

    [Fact]
    public void Update_ChangesTextValueAndOrder()
    {
        var option = CreateOption();
        var updatedAtUtc = CreatedAtUtc.AddDays(1);

        option.Update(" Muy bueno ", " MUY-BUENO ", 2, updatedAtUtc);

        Assert.Equal("Muy bueno", option.Text);
        Assert.Equal("muy-bueno", option.Value);
        Assert.Equal(2, option.Order);
        Assert.Equal(updatedAtUtc, option.UpdatedAtUtc);
    }

    [Fact]
    public void Update_PreservesIdentityQuestionAndActiveState()
    {
        var option = CreateOption();
        var id = option.Id;
        var questionId = option.SurveyQuestionId;

        option.Update("Nueva opcion", "nueva", 3, CreatedAtUtc.AddDays(1));

        Assert.Equal(id, option.Id);
        Assert.Equal(questionId, option.SurveyQuestionId);
        Assert.True(option.IsActive);
    }

    [Fact]
    public void Constructor_RejectsEmptyText()
    {
        Assert.Throws<DomainException>(() => CreateOption(text: " "));
    }

    [Fact]
    public void Constructor_RejectsEmptyValue()
    {
        Assert.Throws<DomainException>(() => CreateOption(value: " "));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsInvalidOrder(int order)
    {
        Assert.Throws<DomainException>(() => CreateOption(order: order));
    }

    [Fact]
    public void Update_RejectsEmptyText()
    {
        var option = CreateOption();

        Assert.Throws<DomainException>(() =>
            option.Update(" ", "value", 1, CreatedAtUtc.AddDays(1)));
    }

    [Fact]
    public void Update_RejectsEmptyValue()
    {
        var option = CreateOption();

        Assert.Throws<DomainException>(() =>
            option.Update("Texto", " ", 1, CreatedAtUtc.AddDays(1)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Update_RejectsInvalidOrder(int order)
    {
        var option = CreateOption();

        Assert.Throws<DomainException>(() =>
            option.Update("Texto", "value", order, CreatedAtUtc.AddDays(1)));
    }

    private static SurveyQuestionOption CreateOption(
        string text = "Opcion",
        string value = "muy-bueno",
        int order = 1)
    {
        return new SurveyQuestionOption(
            Guid.NewGuid(),
            Guid.NewGuid(),
            text,
            value,
            order,
            CreatedAtUtc);
    }
}
