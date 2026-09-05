using AcademicSurveySystem.Application.Common.Results;
using AcademicSurveySystem.Domain.Academic.Enums;

namespace AcademicSurveySystem.Application.Academic.Careers;

/// <summary>
/// Datos para crear una carrera, curso o trayecto académico.
/// </summary>
/// <param name="Code">Código único de la carrera.</param>
/// <param name="Name">Nombre visible de la carrera.</param>
/// <param name="Type">Tipo de carrera.</param>
public sealed record CreateCareerRequest(
    string? Code,
    string? Name,
    string? Type)
{
    public IReadOnlyCollection<ApplicationError> Validate()
    {
        var errors = new List<ApplicationError>();

        if (string.IsNullOrWhiteSpace(Code))
        {
            errors.Add(new ApplicationError("Career.CodeRequired", "Code is required."));
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add(new ApplicationError("Career.NameRequired", "Name is required."));
        }

        if (string.IsNullOrWhiteSpace(Type))
        {
            errors.Add(new ApplicationError("Career.TypeRequired", "Type is required."));
        }
        else if (!Enum.TryParse<CareerType>(Type.Trim(), ignoreCase: true, out _))
        {
            errors.Add(new ApplicationError("Career.TypeInvalid", "Type is invalid."));
        }

        return errors;
    }
}
