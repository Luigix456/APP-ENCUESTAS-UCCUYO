using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Surveys.Requests;

/// <summary>
/// Datos para actualizar una plantilla de encuesta.
/// </summary>
/// <param name="Title">Título visible de la encuesta.</param>
/// <param name="Description">Descripción opcional de la encuesta.</param>
/// <param name="Target">Audiencia objetivo de la encuesta.</param>
/// <param name="IsAnonymous">Indica si las respuestas serán anónimas.</param>
public sealed record UpdateSurveyRequest(
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
