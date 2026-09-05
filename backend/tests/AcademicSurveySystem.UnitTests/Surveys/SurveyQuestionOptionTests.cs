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
