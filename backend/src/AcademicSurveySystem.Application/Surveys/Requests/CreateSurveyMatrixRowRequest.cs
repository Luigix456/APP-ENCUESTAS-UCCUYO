using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Surveys.Requests;

/// <summary>
/// Datos para crear una fila de matriz.
/// </summary>
/// <param name="Text">Texto visible de la fila.</param>
/// <param name="Order">Orden de visualización de la fila.</param>
public sealed record CreateSurveyMatrixRowRequest(
    string? Text,
    int? Order)
{
    public IReadOnlyCollection<ApplicationError> Validate()
    {
        var errors = new List<ApplicationError>();

        if (string.IsNullOrWhiteSpace(Text))
        {
            errors.Add(new ApplicationError("SurveyMatrixRow.TextRequired", "Text is required."));
        }

        SurveyRequestValidation.ValidateOrder(Order, "SurveyMatrixRow.OrderRequired", errors);

        return errors;
    }
}
