import { ApiClientError } from '../../api/apiClient';
import type {
  SurveyDetailDto,
  SurveyQuestionDto,
  SurveyQuestionType,
  SurveySummaryDto,
  SurveyStatus,
  SurveyTarget
} from '../../types/surveys';
import {
  SURVEY_QUESTION_TYPE_LABELS,
  SURVEY_STATUS_LABELS,
  SURVEY_TARGET_LABELS
} from '../../types/surveys';

export const MANAGE_SURVEY_TEMPLATES_PERMISSION = 'surveys.templates.manage';
export const READ_SURVEY_TEMPLATES_PERMISSION = 'surveys.templates.read';

export function PermissionDeniedPanel() {
  return (
    <section className="app-content access-denied-panel">
      <p className="eyebrow">Sin acceso</p>
      <h2>No tenés permisos para administrar plantillas de encuesta.</h2>
      <p>Solicitá el permiso {MANAGE_SURVEY_TEMPLATES_PERMISSION} a la administración del sistema.</p>
    </section>
  );
}

export function SurveyStatusBadge({ status }: { status: SurveyStatus }) {
  return (
    <span className={`status-badge status-badge--survey-${status.toLowerCase()}`}>
      {SURVEY_STATUS_LABELS[status]}
    </span>
  );
}

export function ActivityBadge({ isActive }: { isActive: boolean }) {
  return (
    <span className={`status-badge ${isActive ? 'status-badge--open' : 'status-badge--closed'}`}>
      {isActive ? 'Activa' : 'Inactiva'}
    </span>
  );
}

export function SurveyVersionBadge({ survey }: { survey: Pick<SurveyDetailDto | SurveySummaryDto, 'versionNumber'> }) {
  return <span className="status-badge status-badge--version">v{survey.versionNumber}</span>;
}

export function formatSurveyVersion(survey: Pick<SurveyDetailDto | SurveySummaryDto, 'versionNumber'>): string {
  return `v${survey.versionNumber}`;
}

export function formatSurveyTarget(target: SurveyTarget): string {
  return SURVEY_TARGET_LABELS[target];
}

export function formatQuestionType(type: SurveyQuestionType): string {
  return SURVEY_QUESTION_TYPE_LABELS[type];
}

export function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat('es-AR', {
    dateStyle: 'short',
    timeStyle: 'short'
  }).format(new Date(value));
}

export function getFriendlySurveyError(error: unknown, fallback: string): string {
  if (error instanceof ApiClientError) {
    if (error.status === 401) {
      return 'La sesión de usuario venció. Volvé a iniciar sesión.';
    }

    if (error.status === 403) {
      return 'No tenés permisos para administrar plantillas de encuesta.';
    }

    if (error.status === 404) {
      return 'La plantilla de encuesta no fue encontrada.';
    }

    if (error.status === 409 || error.code === 'Conflict') {
      return 'Ya existe otro elemento con ese orden. Elegí un orden diferente.';
    }

    if (error.code === 'Survey.NotEditable') {
      return 'Esta encuesta ya no puede modificarse porque no está en estado borrador.';
    }

    if (error.code === 'Survey.PublishInvalid') {
      return 'No fue posible publicar la encuesta. Revisá que tenga secciones y preguntas activas completas.';
    }

    if (error.status === 400) {
      return getValidationMessage(error.code);
    }
  }

  return fallback;
}

export function canQuestionHaveOptions(question: SurveyQuestionDto): boolean {
  return (
    question.type === 'SingleChoice' ||
    question.type === 'MultipleChoice' ||
    question.type === 'MatrixSingleChoice'
  );
}

export function canQuestionHaveMatrixRows(question: SurveyQuestionDto): boolean {
  return question.type === 'MatrixSingleChoice';
}

export function getNextSectionOrder(survey: SurveyDetailDto): number {
  return getNextOrder(survey.sections);
}

export function getNextQuestionOrder(sectionQuestions: SurveyQuestionDto[]): number {
  return getNextOrder(sectionQuestions);
}

export function getNextOrder(items: Array<{ order: number }>): number {
  return items.reduce((highest, item) => Math.max(highest, item.order), 0) + 1;
}

export function trimmedOrNull(value: string): string | null {
  const trimmed = value.trim();
  return trimmed ? trimmed : null;
}

function getValidationMessage(code: string | null): string {
  switch (code) {
    case 'Survey.TitleRequired':
      return 'Ingresá el título de la encuesta.';
    case 'Survey.TargetRequired':
    case 'Survey.TargetInvalid':
      return 'Seleccioná una audiencia válida.';
    case 'SurveySection.TitleRequired':
      return 'Ingresá el título de la sección.';
    case 'SurveyQuestion.TextRequired':
      return 'Ingresá el texto de la pregunta.';
    case 'SurveyQuestion.TypeRequired':
    case 'SurveyQuestion.TypeInvalid':
      return 'Seleccioná un tipo de pregunta válido.';
    case 'SurveyQuestion.AllowsOtherOptionInvalid':
      return 'La opción Otra sólo puede usarse en preguntas de opción única o múltiple.';
    case 'SurveyQuestion.RatingMinRequired':
    case 'SurveyQuestion.RatingMaxRequired':
    case 'SurveyQuestion.RatingRangeInvalid':
      return 'Configurá una escala de valoración válida.';
    case 'SurveyQuestionOption.TextRequired':
      return 'Ingresá el texto de la opción.';
    case 'SurveyQuestionOption.ValueRequired':
      return 'Ingresá el valor de la opción.';
    case 'SurveyMatrixRow.TextRequired':
      return 'Ingresá el texto de la fila.';
    case 'Order.Invalid':
    case 'SurveySection.OrderRequired':
    case 'SurveyQuestion.OrderRequired':
    case 'SurveyQuestionOption.OrderRequired':
    case 'SurveyMatrixRow.OrderRequired':
      return 'Indicá un orden mayor a cero.';
    default:
      return 'No fue posible procesar la solicitud. Revisá los datos e intentá nuevamente.';
  }
}
