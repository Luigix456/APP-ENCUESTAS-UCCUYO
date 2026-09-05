using AcademicSurveySystem.Domain.Common;
using AcademicSurveySystem.Domain.Surveys.Entities;

namespace AcademicSurveySystem.UnitTests.Surveys;

public sealed class SurveyMatrixRowTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_CreatesMatrixRow_WhenValuesAreValid()
    {
        var matrixRow = CreateMatrixRow();

        Assert.Equal("Claridad", matrixRow.Text);
        Assert.Equal(1, matrixRow.Order);
        Assert.True(matrixRow.IsActive);
        Assert.Equal(CreatedAtUtc, matrixRow.CreatedAtUtc);
    }

    [Fact]
    public void Constructor_RejectsEmptyText()
    {
        Assert.Throws<DomainException>(() => CreateMatrixRow(text: " "));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsInvalidOrder(int order)
    {
        Assert.Throws<DomainException>(() => CreateMatrixRow(order: order));
    }

    private static SurveyMatrixRow CreateMatrixRow(
        string text = "Claridad",
        int order = 1)
    {
        return new SurveyMatrixRow(
            Guid.NewGuid(),
            Guid.NewGuid(),
            text,
            order,
            CreatedAtUtc);
    }
}
