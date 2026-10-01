using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Academic.AcademicUnits;

/// <summary>
/// Datos para actualizar una unidad académica, facultad o departamento.
/// </summary>
public sealed record UpdateAcademicUnitRequest(string? Name)
{
    public IReadOnlyCollection<ApplicationError> Validate()
    {
        var errors = new List<ApplicationError>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add(new ApplicationError("AcademicUnit.NameRequired", "Name is required."));
        }

        return errors;
    }
}
