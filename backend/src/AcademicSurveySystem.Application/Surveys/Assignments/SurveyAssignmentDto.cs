namespace AcademicSurveySystem.Application.Surveys.Assignments;

/// <summary>
/// Representa una asignación de una plantilla de encuesta publicada a un contexto académico.
/// </summary>
/// <param name="Id">Identificador de la asignación.</param>
/// <param name="SurveyId">Identificador de la plantilla de encuesta.</param>
/// <param name="SurveyTitle">Título de la plantilla de encuesta.</param>
/// <param name="SurveyStatus">Estado actual de la plantilla de encuesta.</param>
/// <param name="CareerId">Identificador de la carrera.</param>
/// <param name="CareerName">Nombre de la carrera.</param>
/// <param name="SubjectId">Identificador de la materia.</param>
/// <param name="SubjectName">Nombre de la materia.</param>
/// <param name="AcademicCycleId">Identificador del ciclo académico.</param>
/// <param name="AcademicCycleYear">Año del ciclo académico.</param>
/// <param name="AcademicCyclePeriod">Período del ciclo académico.</param>
/// <param name="TeacherSubjectAssignmentId">Identificador de la asignación docente-materia.</param>
/// <param name="TeacherId">Identificador del docente.</param>
/// <param name="TeacherFullName">Nombre completo del docente.</param>
/// <param name="TeachingRole">Rol docente en la asignación.</param>
/// <param name="IsActive">Indica si la asignación está activa.</param>
/// <param name="CreatedAtUtc">Fecha de creación en UTC.</param>
/// <param name="UpdatedAtUtc">Fecha de última actualización en UTC.</param>
/// <param name="ExpectedRespondentCount">Cantidad esperada de respuestas tomada como snapshot al crear la asignación.</param>
public sealed record SurveyAssignmentDto(
    Guid Id,
    Guid SurveyId,
    string SurveyTitle,
    string SurveyStatus,
    Guid CareerId,
    string CareerName,
    Guid SubjectId,
    string SubjectName,
    Guid AcademicCycleId,
    int AcademicCycleYear,
    string AcademicCyclePeriod,
    Guid TeacherSubjectAssignmentId,
    Guid TeacherId,
    string TeacherFullName,
    string TeachingRole,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    int? ExpectedRespondentCount = null);
