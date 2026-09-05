using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Domain.Surveys.Enums;

namespace AcademicSurveySystem.Application.Surveys.Requests;

/// <summary>
/// Datos para crear una plantilla de encuesta.
/// </summary>
/// <param name="Title">Título visible de la encuesta.</param>
/// <param name="Description">Descripción opcional de la encuesta.</param>
/// <param name="Target">Audiencia objetivo de la encuesta.</param>
/// <param name="IsAnonymous">Indica si las respuestas serán anónimas.</param>
public sealed record CreateSurveyRequest(
    string? Title,
    string? Description,
    string? Target,
    bool IsAnonymous)
{
    public IReadOnlyCollection<ApplicationError> Validate()
    {
        var errors = new List<ApplicationError>();

        SurveyRequestValidation.ValidateSurveyText(Title, Description, Target, errors);

        return errors;
    }
}
