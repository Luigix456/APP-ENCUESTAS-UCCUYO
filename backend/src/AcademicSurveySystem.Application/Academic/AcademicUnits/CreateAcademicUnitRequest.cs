using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Academic.AcademicUnits;

/// <summary>
/// Datos para crear una unidad académica, facultad o departamento.
/// </summary>
public sealed record CreateAcademicUnitRequest(
    string? Code,
    string? Name)
{
    public IReadOnlyCollection<ApplicationError> Validate()
    {
        var errors = new List<ApplicationError>();

        if (string.IsNullOrWhiteSpace(Code))
        {
            errors.Add(new ApplicationError("AcademicUnit.CodeRequired", "Code is required."));
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add(new ApplicationError("AcademicUnit.NameRequired", "Name is required."));
        }

        return errors;
    }
}
