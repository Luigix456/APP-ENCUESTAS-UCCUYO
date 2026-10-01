import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { archiveSurvey, getOrCreateEditableSurveyVersion, getSurveys } from '../../api/surveysApi';
import { useAuth } from '../../auth/AuthProvider';
import type { SurveyFilters, SurveyStatus, SurveySummaryDto, SurveyTarget } from '../../types/surveys';
import { SURVEY_STATUSES, SURVEY_STATUS_LABELS, SURVEY_TARGETS } from '../../types/surveys';
import {
  ActivityBadge,
  formatDateTime,
  formatSurveyTarget,
  getFriendlySurveyError,
  MANAGE_SURVEY_TEMPLATES_PERMISSION,
  PermissionDeniedPanel,
  SurveyVersionBadge,
  SurveyStatusBadge
} from './surveyUi';

type LoadState = 'loading' | 'ready' | 'error';

const initialFilters: SurveyFilters = {
  includeInactive: true,
  status: '',
  target: ''
};

export function SurveysPage() {
  const auth = useAuth();
  const navigate = useNavigate();
  const [filters, setFilters] = useState<SurveyFilters>(initialFilters);
  const [surveys, setSurveys] = useState<SurveySummaryDto[]>([]);
  const [loadState, setLoadState] = useState<LoadState>('loading');
  const [pageError, setPageError] = useState<string | null>(null);
  const [actionMessage, setActionMessage] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [activeActionId, setActiveActionId] = useState<string | null>(null);

  const accessToken = auth.accessToken;
  const canManageTemplates = auth.hasPermission(MANAGE_SURVEY_TEMPLATES_PERMISSION);

  useEffect(() => {
    if (!canManageTemplates || !accessToken) {
      setLoadState('ready');
      return;
    }

    void loadSurveys(accessToken, filters);
  }, [accessToken, canManageTemplates, filters]);

  const sortedSurveys = useMemo(() => {
    return [...surveys].sort((left, right) => Date.parse(right.updatedAtUtc) - Date.parse(left.updatedAtUtc));
  }, [surveys]);

  if (!canManageTemplates) {
    return <PermissionDeniedPanel />;
  }

  async function loadSurveys(token: string, nextFilters = filters) {
    setLoadState('loading');
    setPageError(null);

    try {
      const nextSurveys = await getSurveys(nextFilters, token, auth.logout);
      setSurveys(nextSurveys);
      setLoadState('ready');
    } catch (error) {
      setPageError(getFriendlySurveyError(error, 'No fue posible cargar las plantillas de encuesta.'));
      setLoadState('error');
    }
  }

  async function handleArchive(survey: SurveySummaryDto) {
    if (!accessToken) {
      return;
    }

    const confirmed = window.confirm(
      'Al archivar la encuesta dejará de utilizarse para nuevas operaciones, pero su información histórica se conservará. ¿Deseás continuar?'
    );

    if (!confirmed) {
      return;
    }

    setActiveActionId(survey.id);
    setActionError(null);
    setActionMessage(null);

    try {
      await archiveSurvey(survey.id, accessToken, auth.logout);
      await loadSurveys(accessToken);
      setActionMessage('Encuesta archivada.');
    } catch (error) {
      setActionError(getFriendlySurveyError(error, 'No fue posible archivar la encuesta.'));
    } finally {
      setActiveActionId(null);
    }
  }

  async function handleEdit(survey: SurveySummaryDto) {
    if (!accessToken) {
      return;
    }

    setActiveActionId(survey.id);
    setActionError(null);
    setActionMessage(null);

    try {
      const editableVersion = await getOrCreateEditableSurveyVersion(survey.id, accessToken, auth.logout);
      const message = editableVersion.createdNewVersion
        ? `Se creó una versión editable v${editableVersion.versionNumber}.`
        : `Se abrió la versión editable v${editableVersion.versionNumber}.`;

      navigate(`/app/surveys/${editableVersion.survey.id}/edit`, {
        state: { editableVersionMessage: message }
      });
    } catch (error) {
      setActionError(getFriendlySurveyError(error, 'No fue posible preparar una versión editable de la encuesta.'));
    } finally {
      setActiveActionId(null);
    }
  }

  return (
    <section className="app-content surveys-page">
      <header className="surveys-header">
        <div>
          <p className="eyebrow">Plantillas</p>
          <h2>Encuestas</h2>
          <p>Administrá plantillas de encuesta en borrador, publicadas o archivadas.</p>
        </div>
        <div className="surveys-actions">
          <button
            className="secondary-button"
            disabled={!accessToken || loadState === 'loading'}
            onClick={() => accessToken && void loadSurveys(accessToken)}
            type="button"
          >
            Actualizar
          </button>
          <Link className="primary-link-button" to="/app/surveys/new">
            Nueva encuesta
          </Link>
        </div>
      </header>

      <div className="surveys-filters" aria-label="Filtros de encuestas">
        <label>
          <span>Estado</span>
          <select
            className="text-input"
            onChange={(event) =>
              setFilters((current) => ({
                ...current,
                status: event.target.value as SurveyStatus | ''
              }))
            }
            value={filters.status}
          >
            <option value="">Todos</option>
            {SURVEY_STATUSES.map((status) => (
              <option key={status} value={status}>
                {SURVEY_STATUS_LABELS[status]}
              </option>
            ))}
          </select>
        </label>

        <label>
          <span>Audiencia</span>
          <select
            className="text-input"
            onChange={(event) =>
              setFilters((current) => ({
                ...current,
                target: event.target.value as SurveyTarget | ''
              }))
            }
            value={filters.target}
          >
            <option value="">Todas</option>
            {SURVEY_TARGETS.map((target) => (
              <option key={target} value={target}>
                {formatSurveyTarget(target)}
              </option>
            ))}
          </select>
        </label>

        <label className="checkbox-field">
          <input
            checked={filters.includeInactive}
            onChange={(event) =>
              setFilters((current) => ({
                ...current,
                includeInactive: event.target.checked
              }))
            }
            type="checkbox"
          />
          <span>Incluir inactivas</span>
        </label>
      </div>

      {actionMessage ? (
        <div className="success-message" role="status">
          {actionMessage}
        </div>
      ) : null}

      {actionError ? (
        <p className="submit-error" role="alert">
          {actionError}
        </p>
      ) : null}

      {loadState === 'loading' ? <p aria-live="polite">Cargando encuestas...</p> : null}

      {loadState === 'error' ? (
        <div className="empty-detail" role="alert">
          <h3>No pudimos cargar las encuestas</h3>
          <p>{pageError}</p>
        </div>
      ) : null}

      {loadState === 'ready' && sortedSurveys.length === 0 ? (
        <div className="empty-detail">
          <h3>No hay plantillas disponibles</h3>
          <p>Creá una encuesta en borrador para empezar a diseñar secciones y preguntas.</p>
        </div>
      ) : null}

      {loadState === 'ready' && sortedSurveys.length > 0 ? (
        <div className="surveys-table" role="list">
          {sortedSurveys.map((survey) => (
            <article className="survey-list-card" key={survey.id} role="listitem">
              <header>
                <div>
                  <h3>{survey.title}</h3>
                  <p>{survey.description || 'Sin descripción'}</p>
                </div>
                <div className="badge-group">
                  <SurveyVersionBadge survey={survey} />
                  <SurveyStatusBadge status={survey.status} />
                  <ActivityBadge isActive={survey.isActive} />
                </div>
              </header>

              <dl className="survey-card-meta">
                <div>
                  <dt>Audiencia</dt>
                  <dd>{formatSurveyTarget(survey.target)}</dd>
                </div>
                <div>
                  <dt>Secciones</dt>
                  <dd>{survey.sectionCount}</dd>
                </div>
                <div>
                  <dt>Preguntas</dt>
                  <dd>{survey.questionCount}</dd>
                </div>
                <div>
                  <dt>Actualizada</dt>
                  <dd>{formatDateTime(survey.updatedAtUtc)}</dd>
                </div>
              </dl>

              <div className="survey-card-actions">
                <Link className="secondary-link-button" to={`/app/surveys/${survey.id}/preview`}>
                  Vista previa
                </Link>
                <button
                  className="secondary-button"
                  disabled={activeActionId === survey.id}
                  onClick={() => void handleEdit(survey)}
                  type="button"
                >
                  {activeActionId === survey.id ? 'Preparando...' : 'Editar'}
                </button>
                {survey.status === 'Published' ? (
                  <button
                    className="danger-button"
                    disabled={activeActionId === survey.id}
                    onClick={() => void handleArchive(survey)}
                    type="button"
                  >
                    {activeActionId === survey.id ? 'Archivando...' : 'Archivar'}
                  </button>
                ) : null}
              </div>
            </article>
          ))}
        </div>
      ) : null}
    </section>
  );
}
