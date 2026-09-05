using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Surveys.Requests;

/// <summary>
/// Datos para actualizar una pregunta de encuesta.
/// </summary>
/// <param name="Text">Texto visible de la pregunta.</param>
/// <param name="Type">Tipo de pregunta.</param>
/// <param name="IsRequired">Indica si la pregunta es obligatoria.</param>
/// <param name="AllowsComment">Indica si admite comentario adicional.</param>
/// <param name="AllowsOtherOption">Indica si admite opción "Otra".</param>
/// <param name="Order">Orden de visualización de la pregunta.</param>
public sealed record UpdateSurveyQuestionRequest(
    string? Text,
    string? Type,
    bool IsRequired,
    bool AllowsComment,
    bool AllowsOtherOption,
    int? Order)
{
    public IReadOnlyCollection<ApplicationError> Validate()
    {
        var errors = new List<ApplicationError>();

        SurveyRequestValidation.ValidateQuestion(Text, Type, AllowsOtherOption, Order, errors);

        return errors;
    }
}
