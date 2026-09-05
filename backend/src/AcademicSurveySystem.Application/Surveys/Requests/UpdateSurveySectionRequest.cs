using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Surveys.Requests;

/// <summary>
/// Datos para actualizar una sección de encuesta.
/// </summary>
/// <param name="Title">Título visible de la sección.</param>
/// <param name="Description">Descripción opcional de la sección.</param>
/// <param name="Order">Orden de visualización de la sección.</param>
public sealed record UpdateSurveySectionRequest(
    string? Title,
    string? Description,
    int? Order)
{
    public IReadOnlyCollection<ApplicationError> Validate()
    {
        var errors = new List<ApplicationError>();

        SurveyRequestValidation.ValidateSection(Title, Description, Order, errors);

        return errors;
    }
}
