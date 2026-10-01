using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Surveys.Requests;

/// <summary>
/// Datos para actualizar una opción de pregunta.
/// </summary>
/// <param name="Text">Texto visible de la opción.</param>
/// <param name="Value">Valor interno de la opción.</param>
/// <param name="Order">Orden de visualización de la opción.</param>
public sealed record UpdateSurveyQuestionOptionRequest(
    string? Text,
    string? Value,
    int? Order)
{
    public IReadOnlyCollection<ApplicationError> Validate()
    {
        var errors = new List<ApplicationError>();

        if (string.IsNullOrWhiteSpace(Text))
        {
            errors.Add(new ApplicationError("SurveyQuestionOption.TextRequired", "Text is required."));
        }

        if (string.IsNullOrWhiteSpace(Value))
        {
            errors.Add(new ApplicationError("SurveyQuestionOption.ValueRequired", "Value is required."));
        }

        SurveyRequestValidation.ValidateOrder(Order, "SurveyQuestionOption.OrderRequired", errors);

        return errors;
    }
}
