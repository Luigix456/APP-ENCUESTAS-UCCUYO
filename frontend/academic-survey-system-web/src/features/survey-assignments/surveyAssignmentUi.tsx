import { ApiClientError } from '../../api/apiClient';
import type { AcademicCycleDto, SubjectDto, TeacherDto, TeacherSubjectAssignmentDto } from '../../types/academicCatalog';
import type { SurveySummaryDto } from '../../types/surveys';
import { SURVEY_TARGET_LABELS } from '../../types/surveys';

export const MANAGE_SURVEY_ASSIGNMENTS_PERMISSION = 'surveys.templates.manage';
export const READ_ACADEMIC_CATALOG_PERMISSION = 'academic.catalog.read';

export function SurveyAssignmentPermissionPanel({ missingCatalog }: { missingCatalog?: boolean }) {
  if (missingCatalog) {
    return (
      <section className="app-content access-denied-panel">
        <p className="eyebrow">Sin acceso</p>
        <h2>No tenés permisos para consultar el catálogo académico necesario para crear una asignación.</h2>
        <p>Solicitá el permiso {READ_ACADEMIC_CATALOG_PERMISSION} a la administración del sistema.</p>
      </section>
    );
  }

  return (
    <section className="app-content access-denied-panel">
      <p className="eyebrow">Sin acceso</p>
      <h2>No tenés permisos para administrar asignaciones de encuesta.</h2>
      <p>Solicitá el permiso {MANAGE_SURVEY_ASSIGNMENTS_PERMISSION} a la administración del sistema.</p>
    </section>
  );
}

export function formatAcademicCycle(cycle: AcademicCycleDto): string {
  return `${cycle.year} · ${formatPeriod(cycle.period)}`;
}

export function formatAcademicCycleParts(year: number, period: string): string {
  return `${year} · ${formatPeriod(period)}`;
}

export function formatSubject(subject: SubjectDto): string {
  return `${subject.name} · ${subject.year}° · ${formatPeriod(subject.period)}`;
}

export function formatTeacher(teacher: TeacherDto): string {
  return `${teacher.firstName} ${teacher.lastName}`;
}

export function formatTeacherSubjectAssignment(assignment: TeacherSubjectAssignmentDto): string {
  return `${assignment.teacherFullName} · ${assignment.teachingRole}`;
}

export function formatSurveyOption(survey: SurveySummaryDto): string {
  return `${survey.title} · v${survey.versionNumber} · ${SURVEY_TARGET_LABELS[survey.target]}`;
}

export function formatSurveyStatus(status: string): string {
  switch (status) {
    case 'Draft':
      return 'Borrador';
    case 'Published':
      return 'Publicada';
    case 'Archived':
      return 'Archivada';
    default:
      return status;
  }
}

export function getFriendlyAssignmentError(error: unknown, fallback: string): string {
  if (error instanceof ApiClientError) {
    if (error.status === 401) {
      return 'La sesión de usuario venció. Volvé a iniciar sesión.';
    }

    if (error.status === 403) {
      return 'No tenés permisos para administrar asignaciones de encuesta.';
    }

    if (error.status === 404) {
      return 'El recurso académico seleccionado ya no existe.';
    }

    if (error.status === 409) {
      return 'Ya existe una asignación para esta encuesta y contexto académico. Si está inactiva, reactivala desde el listado.';
    }

    if (error.status === 400) {
      return getValidationMessage(error.code);
    }
  }

  return fallback;
}

export function getAssignmentSelectHint({
  error,
  isLoading,
  readyMessage,
  unavailableMessage
}: {
  error: string | null;
  isLoading: boolean;
  readyMessage: string;
  unavailableMessage: string;
}): string {
  if (error) {
    return error;
  }

  if (isLoading) {
    return readyMessage;
  }

  return unavailableMessage;
}

function getValidationMessage(code: string | null): string {
  switch (code) {
    case 'SurveyAssignment.SurveyIdRequired':
      return 'Seleccioná una encuesta publicada.';
    case 'SurveyAssignment.CareerIdRequired':
      return 'Seleccioná una carrera.';
    case 'SurveyAssignment.SubjectIdRequired':
      return 'Seleccioná una materia.';
    case 'SurveyAssignment.AcademicCycleIdRequired':
      return 'Seleccioná un ciclo lectivo.';
    case 'SurveyAssignment.TeacherSubjectAssignmentIdRequired':
      return 'Seleccioná una asignación docente-materia.';
    case 'SurveyAssignment.SurveyInactive':
      return 'La encuesta seleccionada no está activa.';
    case 'SurveyAssignment.SurveyNotPublished':
      return 'La encuesta seleccionada debe estar publicada.';
    case 'SurveyAssignment.CareerInactive':
      return 'La carrera seleccionada no está activa.';
    case 'SurveyAssignment.SubjectInactive':
      return 'La materia seleccionada no está activa.';
    case 'SurveyAssignment.SubjectCareerMismatch':
      return 'La materia no pertenece a la carrera seleccionada.';
    case 'SurveyAssignment.AcademicCycleInactive':
      return 'El ciclo lectivo seleccionado no está activo.';
    case 'SurveyAssignment.TeacherSubjectAssignmentInactive':
      return 'La asignación docente-materia seleccionada no está activa.';
    case 'SurveyAssignment.TeacherSubjectAssignmentSubjectMismatch':
      return 'La asignación docente-materia no corresponde a la materia seleccionada.';
    case 'SurveyAssignment.TeacherSubjectAssignmentCycleMismatch':
      return 'La asignación docente-materia no corresponde al ciclo lectivo seleccionado.';
    case 'Subject.EnrollmentRequired':
      return 'Falta cargar la cantidad de alumnos inscriptos.';
    default:
      return 'No fue posible procesar la solicitud. Revisá los datos e intentá nuevamente.';
  }
}

function formatPeriod(period: string): string {
  switch (period) {
    case 'Annual':
      return 'Anual';
    case 'FirstSemester':
      return 'Primer semestre';
    case 'SecondSemester':
      return 'Segundo semestre';
    case 'FirstQuarter':
      return 'Primer cuatrimestre';
    case 'SecondQuarter':
      return 'Segundo cuatrimestre';
    default:
      return period;
  }
}
