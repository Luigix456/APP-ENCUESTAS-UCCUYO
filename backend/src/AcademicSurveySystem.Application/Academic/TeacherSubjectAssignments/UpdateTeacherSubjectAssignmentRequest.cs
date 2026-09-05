using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Academic.TeacherSubjectAssignments;

/// <summary>
/// Datos para actualizar una asignación docente-materia-ciclo.
/// </summary>
/// <param name="TeachingRole">Rol docente dentro de la asignación.</param>
public sealed record UpdateTeacherSubjectAssignmentRequest(string? TeachingRole)
{
    public IReadOnlyCollection<ApplicationError> Validate()
    {
        var errors = new List<ApplicationError>();

        if (string.IsNullOrWhiteSpace(TeachingRole))
        {
            errors.Add(new ApplicationError(
                "TeacherSubjectAssignment.TeachingRoleRequired",
                "TeachingRole is required."));
        }

        return errors;
    }
}
