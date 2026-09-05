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
}
