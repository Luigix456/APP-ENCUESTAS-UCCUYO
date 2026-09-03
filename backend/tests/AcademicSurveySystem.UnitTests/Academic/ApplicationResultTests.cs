using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.UnitTests.Academic;

public sealed class ApplicationResultTests
{
    [Fact]
    public void Success_CreatesSuccessfulResult()
    {
        var result = ApplicationResult.Success();

        Assert.True(result.Succeeded);
        Assert.Equal(ApplicationResultStatus.Success, result.Status);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validation_CreatesValidationResult()
    {
        var result = ApplicationResult.Validation([
            new ApplicationError("Validation", "Invalid request.")
        ]);

        Assert.False(result.Succeeded);
        Assert.Equal(ApplicationResultStatus.Validation, result.Status);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void GenericSuccess_CarriesValue()
    {
        var result = ApplicationResult<string>.Success("value");

        Assert.True(result.Succeeded);
        Assert.Equal("value", result.Value);
    }
}
