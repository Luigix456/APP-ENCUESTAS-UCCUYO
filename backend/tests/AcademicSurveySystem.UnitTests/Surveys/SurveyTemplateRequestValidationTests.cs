using AcademicSurveySystem.Application.Surveys.Requests;

namespace AcademicSurveySystem.UnitTests.Surveys;

public sealed class SurveyTemplateRequestValidationTests
{
    [Fact]
    public void CreateSurvey_RejectsEmptyTitle()
    {
        var request = new CreateSurveyRequest(" ", null, "Student", IsAnonymous: true);

        var errors = request.Validate();

        Assert.Contains(errors, error => error.Code == "Survey.TitleRequired");
    }

    [Fact]
    public void CreateSection_RejectsOrderZero()
    {
        var request = new CreateSurveySectionRequest("Seccion", null, Order: 0);

        var errors = request.Validate();

        Assert.Contains(errors, error => error.Code == "Order.Invalid");
    }

    [Fact]
    public void CreateQuestion_RejectsOrderZero()
    {
        var request = new CreateSurveyQuestionRequest(
            "Pregunta",
            "ShortText",
            IsRequired: true,
            AllowsComment: false,
            AllowsOtherOption: false,
            Order: 0);

        var errors = request.Validate();

        Assert.Contains(errors, error => error.Code == "Order.Invalid");
    }

    [Fact]
    public void UpdateOption_RejectsMissingValue()
    {
        var request = new UpdateSurveyQuestionOptionRequest("Opcion", " ", Order: 1);

        var errors = request.Validate();

        Assert.Contains(errors, error => error.Code == "SurveyQuestionOption.ValueRequired");
    }

    [Fact]
    public void UpdateOption_RejectsInvalidOrder()
    {
        var request = new UpdateSurveyQuestionOptionRequest("Opcion", "opcion", Order: 0);

        var errors = request.Validate();

        Assert.Contains(errors, error => error.Code == "Order.Invalid");
    }

    [Fact]
    public void UpdateMatrixRow_RejectsMissingText()
    {
        var request = new UpdateSurveyMatrixRowRequest(" ", Order: 1);

        var errors = request.Validate();

        Assert.Contains(errors, error => error.Code == "SurveyMatrixRow.TextRequired");
    }

    [Fact]
    public void UpdateMatrixRow_RejectsInvalidOrder()
    {
        var request = new UpdateSurveyMatrixRowRequest("Fila", Order: 0);

        var errors = request.Validate();

        Assert.Contains(errors, error => error.Code == "Order.Invalid");
    }

    [Fact]
    public void CreateQuestion_RejectsAllowsOtherOptionForShortText()
    {
        var request = new CreateSurveyQuestionRequest(
            "Pregunta",
            "ShortText",
            IsRequired: true,
            AllowsComment: false,
            AllowsOtherOption: true,
            Order: 1);

        var errors = request.Validate();

        Assert.Contains(errors, error => error.Code == "SurveyQuestion.AllowsOtherOptionInvalid");
    }

    [Fact]
    public void CreateQuestion_AllowsRatingScaleWithValidBounds()
    {
        var request = new CreateSurveyQuestionRequest(
            "Pregunta",
            "RatingScale",
            IsRequired: true,
            AllowsComment: false,
            AllowsOtherOption: false,
            Order: 1,
            RatingMin: 1,
            RatingMax: 5);

        var errors = request.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void CreateQuestion_RejectsRatingScaleWithoutMin()
    {
        var request = new CreateSurveyQuestionRequest(
            "Pregunta",
            "RatingScale",
            IsRequired: true,
            AllowsComment: false,
            AllowsOtherOption: false,
            Order: 1,
            RatingMax: 5);

        var errors = request.Validate();

        Assert.Contains(errors, error => error.Code == "SurveyQuestion.RatingMinRequired");
    }

    [Fact]
    public void CreateQuestion_RejectsRatingScaleWithoutMax()
    {
        var request = new CreateSurveyQuestionRequest(
            "Pregunta",
            "RatingScale",
            IsRequired: true,
            AllowsComment: false,
            AllowsOtherOption: false,
            Order: 1,
            RatingMin: 1);

        var errors = request.Validate();

        Assert.Contains(errors, error => error.Code == "SurveyQuestion.RatingMaxRequired");
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(6, 5)]
    public void CreateQuestion_RejectsInvalidRatingScaleRange(int ratingMin, int ratingMax)
    {
        var request = new CreateSurveyQuestionRequest(
            "Pregunta",
            "RatingScale",
            IsRequired: true,
            AllowsComment: false,
            AllowsOtherOption: false,
            Order: 1,
            RatingMin: ratingMin,
            RatingMax: ratingMax);

        var errors = request.Validate();

        Assert.Contains(errors, error => error.Code == "SurveyQuestion.RatingRangeInvalid");
    }

    [Fact]
    public void CreateQuestion_RejectsRatingBoundsForOtherQuestionTypes()
    {
        var request = new CreateSurveyQuestionRequest(
            "Pregunta",
            "ShortText",
            IsRequired: true,
            AllowsComment: false,
            AllowsOtherOption: false,
            Order: 1,
            RatingMin: 1,
            RatingMax: 5);

        var errors = request.Validate();

        Assert.Contains(errors, error => error.Code == "SurveyQuestion.RatingBoundsInvalid");
    }
}
