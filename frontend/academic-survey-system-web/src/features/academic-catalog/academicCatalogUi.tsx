import { Link } from 'react-router-dom';
import { ApiClientError } from '../../api/apiClient';
import type {
  AcademicCycleDto,
  AcademicCyclePeriod,
  CareerType,
  SubjectPeriod
} from '../../types/academicCatalog';
import {
  ACADEMIC_PERIOD_LABELS,
  CAREER_TYPE_LABELS
} from '../../types/academicCatalog';

export const MANAGE_ACADEMIC_CATALOG_PERMISSION = 'academic.catalog.manage';

export function AcademicCatalogPermissionPanel() {
  return (
    <section className="app-content access-denied-panel">
      <p className="eyebrow">Sin acceso</p>
      <h2>No tenés permisos para administrar el catálogo académico.</h2>
      <p>Solicitá el permiso {MANAGE_ACADEMIC_CATALOG_PERMISSION} a la administración del sistema.</p>
    </section>
  );
}

export function AcademicHomeCard({
  description,
  title,
  to
}: {
  description: string;
  title: string;
  to: string;
}) {
  return (
    <Link className="catalog-home-card" to={to}>
      <h3>{title}</h3>
      <p>{description}</p>
    </Link>
  );
}

export function formatCareerType(type: CareerType): string {
  return CAREER_TYPE_LABELS[type];
}

export function formatPeriod(period: SubjectPeriod | AcademicCyclePeriod): string {
  return ACADEMIC_PERIOD_LABELS[period];
}

export function formatAcademicCycle(cycle: AcademicCycleDto): string {
  return `${cycle.year} · ${formatPeriod(cycle.period)}`;
}

export function formatDateOnly(value: string): string {
  return new Intl.DateTimeFormat('es-AR', { dateStyle: 'short' }).format(new Date(`${value}T00:00:00`));
}

export function getFriendlyCatalogError(error: unknown, fallback: string): string {
  if (error instanceof ApiClientError) {
    if (error.status === 401) {
      return 'La sesión de usuario venció. Volvé a iniciar sesión.';
    }

    if (error.status === 403) {
      return 'No tenés permisos para administrar el catálogo académico.';
    }

    if (error.status === 404) {
      return 'El recurso académico ya no existe.';
    }

    if (error.status === 409) {
      return getConflictMessage(error.message, error.code);
    }

    if (error.status === 400) {
      return getValidationMessage(error.code);
    }
  }

  return fallback;
}

export function nullIfBlank(value: string): string | null {
  const trimmed = value.trim();
  return trimmed ? trimmed : null;
}

function getConflictMessage(message: string, code: string | null): string {
  const conflict = `${code ?? ''} ${message}`.toLowerCase();

  // Las asignaciones docentes contienen las palabras "teacher" y "subject";
  // por eso el caso más específico debe evaluarse primero.
  if (conflict.includes('teacher subject assignment') || conflict.includes('teachersubjectassignment')) {
    return 'Ya existe esta asignación docente para la materia y ciclo seleccionados.';
  }

  if (conflict.includes('academic cycle') || conflict.includes('academiccycle')) {
    return 'Ya existe un ciclo lectivo con ese año y período.';
  }

  if (conflict.includes('career')) {
    return 'Ya existe una carrera con ese código.';
  }

  if (conflict.includes('subject')) {
    return 'Ya existe una materia con ese código para esta carrera.';
  }

  if (conflict.includes('teacher')) {
    return 'Ya existe un docente con ese email.';
  }

  return 'Ya existe un recurso académico con esos datos.';
}

function getValidationMessage(code: string | null): string {
  switch (code) {
    case 'Career.CodeRequired':
      return 'Ingresá el código de la carrera.';
    case 'Career.NameRequired':
      return 'Ingresá el nombre de la carrera.';
    case 'Career.TypeRequired':
    case 'Career.TypeInvalid':
      return 'Seleccioná un tipo de carrera válido.';
    case 'Subject.CareerIdRequired':
      return 'Seleccioná una carrera.';
    case 'Subject.CodeRequired':
      return 'Ingresá el código de la materia.';
    case 'Subject.NameRequired':
      return 'Ingresá el nombre de la materia.';
    case 'Subject.YearRequired':
    case 'Subject.YearInvalid':
      return 'Ingresá un año entre 1 y 10.';
    case 'Subject.PeriodRequired':
    case 'Subject.PeriodInvalid':
      return 'Seleccioná un período válido.';
    case 'AcademicCycle.YearRequired':
    case 'AcademicCycle.YearInvalid':
      return 'Ingresá un año entre 2000 y 2100.';
    case 'AcademicCycle.PeriodRequired':
    case 'AcademicCycle.PeriodInvalid':
      return 'Seleccioná un período válido.';
    case 'AcademicCycle.StartDateRequired':
      return 'Ingresá la fecha de inicio.';
    case 'AcademicCycle.EndDateRequired':
      return 'Ingresá la fecha de finalización.';
    case 'AcademicCycle.DateRangeInvalid':
      return 'La fecha de inicio debe ser anterior o igual a la fecha de finalización.';
    case 'Teacher.FirstNameRequired':
      return 'Ingresá el nombre del docente.';
    case 'Teacher.LastNameRequired':
      return 'Ingresá el apellido del docente.';
    case 'Teacher.EmailInvalid':
      return 'Ingresá un email válido o dejá el campo vacío.';
    case 'TeacherSubjectAssignment.TeacherIdRequired':
      return 'Seleccioná un docente.';
    case 'TeacherSubjectAssignment.SubjectIdRequired':
      return 'Seleccioná una materia.';
    case 'TeacherSubjectAssignment.AcademicCycleIdRequired':
      return 'Seleccioná un ciclo lectivo.';
    case 'TeacherSubjectAssignment.TeachingRoleRequired':
      return 'Ingresá el rol docente.';
    default:
      return 'No fue posible procesar la solicitud. Revisá los datos e intentá nuevamente.';
  }
}
