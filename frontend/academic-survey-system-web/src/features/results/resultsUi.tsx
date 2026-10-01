import { ApiClientError } from '../../api/apiClient';

export const READ_ALL_RESULTS_PERMISSION = 'results.read_all';
export const READ_CAREER_RESULTS_PERMISSION = 'results.read_career';

export function hasResultsPermission(hasPermission: (permission: string) => boolean): boolean {
  return (
    hasPermission(READ_ALL_RESULTS_PERMISSION) ||
    hasPermission(READ_CAREER_RESULTS_PERMISSION)
  );
}

export function ResultsPermissionPanel() {
  return (
    <section className="app-content access-denied-panel">
      <p className="eyebrow">Sin acceso</p>
      <h2>No tenés permisos para consultar resultados.</h2>
      <p>
        Solicitá el permiso {READ_ALL_RESULTS_PERMISSION} o {READ_CAREER_RESULTS_PERMISSION} a la
        administración del sistema.
      </p>
    </section>
  );
}

export function formatAcademicCycleParts(year: number, period: string): string {
  return `${year} · ${formatPeriod(period)}`;
}

export function formatQuestionType(type: string): string {
  switch (type) {
    case 'SingleChoice':
      return 'Opción única';
    case 'MultipleChoice':
      return 'Opción múltiple';
    case 'ShortText':
      return 'Texto corto';
    case 'LongText':
      return 'Texto largo';
    case 'RatingScale':
      return 'Escala de valoración';
    case 'MatrixSingleChoice':
      return 'Matriz de opción única';
    default:
      return type;
  }
}

export function formatDateTimeOrEmpty(value: string | null): string {
  if (!value) {
    return 'Sin respuestas';
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return 'Fecha no disponible';
  }

  return new Intl.DateTimeFormat('es-AR', {
    dateStyle: 'short',
    timeStyle: 'short'
  }).format(date);
}

export function formatPercent(value: number): string {
  return new Intl.NumberFormat('es-AR', {
    maximumFractionDigits: 2
  }).format(value);
}

export function formatAverage(value: number | null): string {
  if (value === null) {
    return 'Sin promedio';
  }

  return new Intl.NumberFormat('es-AR', {
    minimumFractionDigits: Number.isInteger(value) ? 0 : 2,
    maximumFractionDigits: 2
  }).format(value);
}

export function getFriendlyResultsError(error: unknown, fallback: string): string {
  if (error instanceof ApiClientError) {
    if (error.status === 401) {
      return 'La sesión de usuario venció. Volvé a iniciar sesión.';
    }

    if (error.status === 403) {
      return 'No tenés permisos para consultar estos resultados.';
    }

    if (error.status === 404) {
      return 'Los resultados solicitados no fueron encontrados.';
    }
  }

  return fallback;
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
