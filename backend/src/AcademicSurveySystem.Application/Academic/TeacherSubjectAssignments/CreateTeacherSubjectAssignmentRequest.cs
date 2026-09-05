using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Academic.TeacherSubjectAssignments;

/// <summary>
/// Datos para crear una asignación docente-materia-ciclo.
/// </summary>
/// <param name="TeacherId">Identificador del docente.</param>
/// <param name="SubjectId">Identificador de la materia.</param>
/// <param name="AcademicCycleId">Identificador del ciclo lectivo.</param>
/// <param name="TeachingRole">Rol docente dentro de la asignación.</param>
public sealed record CreateTeacherSubjectAssignmentRequest(
    Guid? TeacherId,
    Guid? SubjectId,
    Guid? AcademicCycleId,
    string? TeachingRole)
{
    public IReadOnlyCollection<ApplicationError> Validate()
    {
        var errors = new List<ApplicationError>();

        if (TeacherId is null || TeacherId == Guid.Empty)
        {
            errors.Add(new ApplicationError("TeacherSubjectAssignment.TeacherIdRequired", "TeacherId is required."));
        }

        if (SubjectId is null || SubjectId == Guid.Empty)
        {
            errors.Add(new ApplicationError("TeacherSubjectAssignment.SubjectIdRequired", "SubjectId is required."));
        }

        if (AcademicCycleId is null || AcademicCycleId == Guid.Empty)
        {
            errors.Add(new ApplicationError(
                "TeacherSubjectAssignment.AcademicCycleIdRequired",
                "AcademicCycleId is required."));
        }

        if (string.IsNullOrWhiteSpace(TeachingRole))
        {
            errors.Add(new ApplicationError(
                "TeacherSubjectAssignment.TeachingRoleRequired",
                "TeachingRole is required."));
        }

        return errors;
    }
}
