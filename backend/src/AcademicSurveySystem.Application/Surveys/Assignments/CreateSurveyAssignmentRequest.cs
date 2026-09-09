using AcademicSurveySystem.Application.Common.Results;

namespace AcademicSurveySystem.Application.Surveys.Assignments;

/// <summary>
/// Datos para crear una asignación de encuesta a un contexto académico.
/// </summary>
/// <param name="SurveyId">Identificador de la plantilla de encuesta publicada.</param>
/// <param name="CareerId">Identificador de la carrera.</param>
/// <param name="SubjectId">Identificador de la materia.</param>
/// <param name="AcademicCycleId">Identificador del ciclo académico.</param>
/// <param name="TeacherSubjectAssignmentId">Identificador de la asignación docente-materia.</param>
public sealed record CreateSurveyAssignmentRequest(
    Guid SurveyId,
    Guid CareerId,
    Guid SubjectId,
    Guid AcademicCycleId,
    Guid TeacherSubjectAssignmentId)
{
    public IReadOnlyCollection<ApplicationError> Validate()
    {
        var errors = new List<ApplicationError>();

        AddRequiredGuidError(SurveyId, "SurveyAssignment.SurveyIdRequired", "SurveyId is required.", errors);
        AddRequiredGuidError(CareerId, "SurveyAssignment.CareerIdRequired", "CareerId is required.", errors);
        AddRequiredGuidError(SubjectId, "SurveyAssignment.SubjectIdRequired", "SubjectId is required.", errors);
        AddRequiredGuidError(
            AcademicCycleId,
            "SurveyAssignment.AcademicCycleIdRequired",
            "AcademicCycleId is required.",
            errors);
        AddRequiredGuidError(
            TeacherSubjectAssignmentId,
            "SurveyAssignment.TeacherSubjectAssignmentIdRequired",
            "TeacherSubjectAssignmentId is required.",
            errors);

        return errors;
    }

    private static void AddRequiredGuidError(
        Guid value,
        string code,
        string message,
        ICollection<ApplicationError> errors)
    {
        if (value == Guid.Empty)
        {
            errors.Add(new ApplicationError(code, message));
        }
    }
}
