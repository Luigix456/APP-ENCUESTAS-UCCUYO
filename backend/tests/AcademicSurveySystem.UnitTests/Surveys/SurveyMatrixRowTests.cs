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
    public void Update_ChangesTextAndOrder()
    {
        var matrixRow = CreateMatrixRow();
        var updatedAtUtc = CreatedAtUtc.AddDays(1);

        matrixRow.Update(" Organizacion ", 2, updatedAtUtc);

        Assert.Equal("Organizacion", matrixRow.Text);
        Assert.Equal(2, matrixRow.Order);
        Assert.Equal(updatedAtUtc, matrixRow.UpdatedAtUtc);
    }

    [Fact]
    public void Update_PreservesIdentityQuestionAndActiveState()
    {
        var matrixRow = CreateMatrixRow();
        var id = matrixRow.Id;
        var questionId = matrixRow.SurveyQuestionId;

        matrixRow.Update("Organizacion", 3, CreatedAtUtc.AddDays(1));

        Assert.Equal(id, matrixRow.Id);
        Assert.Equal(questionId, matrixRow.SurveyQuestionId);
        Assert.True(matrixRow.IsActive);
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

    [Fact]
    public void Update_RejectsEmptyText()
    {
        var matrixRow = CreateMatrixRow();

        Assert.Throws<DomainException>(() =>
            matrixRow.Update(" ", 1, CreatedAtUtc.AddDays(1)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Update_RejectsInvalidOrder(int order)
    {
        var matrixRow = CreateMatrixRow();

        Assert.Throws<DomainException>(() =>
            matrixRow.Update("Claridad", order, CreatedAtUtc.AddDays(1)));
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
