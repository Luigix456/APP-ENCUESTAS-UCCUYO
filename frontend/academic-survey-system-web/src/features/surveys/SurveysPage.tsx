import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  archiveSurvey,
  createSurvey,
  getOrCreateEditableSurveyVersion,
  getSurveys
} from '../../api/surveysApi';
import { useAuth } from '../../auth/AuthProvider';
import { PaginationControls, usePagination } from '../../components/Pagination';
import { ConfirmDialog, Modal } from '../../components/ui/Modal';
import { useToast } from '../../components/ui/ToastProvider';
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
  SurveyStatusBadge,
  trimmedOrNull
} from './surveyUi';

type LoadState = 'loading' | 'ready' | 'error';
type ActivityFilter = '' | 'active' | 'inactive';
type SortOption = 'updated-desc' | 'updated-asc' | 'title-asc' | 'title-desc';

interface SurveyCreateFormState {
  title: string;
  description: string;
  target: SurveyTarget;
  isAnonymous: boolean;
}

const initialFilters: SurveyFilters = {
  includeInactive: true,
  status: '',
  target: ''
};

const initialCreateForm: SurveyCreateFormState = {
  title: '',
  description: '',
  target: 'Student',
  isAnonymous: true
};

export function SurveysPage() {
  const auth = useAuth();
  const navigate = useNavigate();
  const toast = useToast();
  const [filters, setFilters] = useState<SurveyFilters>(initialFilters);
  const [search, setSearch] = useState('');
  const [activityFilter, setActivityFilter] = useState<ActivityFilter>('');
  const [sortBy, setSortBy] = useState<SortOption>('updated-desc');
  const [surveys, setSurveys] = useState<SurveySummaryDto[]>([]);
  const [loadState, setLoadState] = useState<LoadState>('loading');
  const [pageError, setPageError] = useState<string | null>(null);
  const [activeActionId, setActiveActionId] = useState<string | null>(null);
  const [archiveTarget, setArchiveTarget] = useState<SurveySummaryDto | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [createForm, setCreateForm] = useState<SurveyCreateFormState>(initialCreateForm);
  const [createError, setCreateError] = useState<string | null>(null);
  const [isCreating, setIsCreating] = useState(false);

  const accessToken = auth.accessToken;
  const canManageTemplates = auth.hasPermission(MANAGE_SURVEY_TEMPLATES_PERMISSION);

  useEffect(() => {
    if (!canManageTemplates || !accessToken) {
      setLoadState('ready');
      return;
    }
    void loadSurveys(accessToken, filters);
  }, [accessToken, canManageTemplates, filters]);

  const filteredSurveys = useMemo(() => {
    const normalizedSearch = search.trim().toLocaleLowerCase('es');
    return [...surveys]
      .filter((survey) => {
        if (!normalizedSearch) return true;
        return `${survey.title} ${survey.description ?? ''} ${formatSurveyTarget(survey.target)}`
          .toLocaleLowerCase('es')
          .includes(normalizedSearch);
      })
      .filter((survey) => {
        if (activityFilter === 'active') return survey.isActive;
        if (activityFilter === 'inactive') return !survey.isActive;
        return true;
      })
      .sort((left, right) => {
        if (sortBy === 'updated-asc') return Date.parse(left.updatedAtUtc) - Date.parse(right.updatedAtUtc);
        if (sortBy === 'title-asc') return left.title.localeCompare(right.title, 'es');
        if (sortBy === 'title-desc') return right.title.localeCompare(left.title, 'es');
        return Date.parse(right.updatedAtUtc) - Date.parse(left.updatedAtUtc);
      });
  }, [activityFilter, search, sortBy, surveys]);

  const summary = useMemo(
    () => ({
      total: surveys.length,
      drafts: surveys.filter((survey) => survey.status === 'Draft').length,
      published: surveys.filter((survey) => survey.status === 'Published').length,
      archived: surveys.filter((survey) => survey.status === 'Archived').length
    }),
    [surveys]
  );

  const surveyPagination = usePagination(filteredSurveys, 8);

  if (!canManageTemplates) return <PermissionDeniedPanel />;

  async function loadSurveys(token: string, nextFilters = filters) {
    setLoadState('loading');
    setPageError(null);
    try {
      setSurveys(await getSurveys(nextFilters, token, auth.logout));
      setLoadState('ready');
    } catch (error) {
      setPageError(getFriendlySurveyError(error, 'No fue posible cargar las plantillas de encuesta.'));
      setLoadState('error');
    }
  }

  async function confirmArchive() {
    if (!accessToken || !archiveTarget) return;
    const survey = archiveTarget;
    setActiveActionId(survey.id);
    try {
      await archiveSurvey(survey.id, accessToken, auth.logout);
      setArchiveTarget(null);
      await loadSurveys(accessToken);
      toast.success('Plantilla archivada', 'Se conservan sus asignaciones y resultados históricos.');
    } catch (error) {
      toast.error('No se pudo archivar', getFriendlySurveyError(error, 'No fue posible archivar la encuesta.'));
    } finally {
      setActiveActionId(null);
    }
  }

  async function handleEdit(survey: SurveySummaryDto) {
    if (!accessToken) return;
    setActiveActionId(survey.id);
    try {
      const editableVersion = await getOrCreateEditableSurveyVersion(survey.id, accessToken, auth.logout);
      const message = editableVersion.createdNewVersion
        ? `Se creó la versión ${editableVersion.versionNumber} como borrador para preservar el historial.`
        : `Se abrió la versión ${editableVersion.versionNumber} que ya estaba en edición.`;
      toast.info('Versión editable preparada', message);
      navigate(`/app/surveys/${editableVersion.survey.id}/edit`, {
        state: { editableVersionMessage: message }
      });
    } catch (error) {
      toast.error(
        'No se pudo abrir la plantilla',
        getFriendlySurveyError(error, 'No fue posible preparar una versión editable de la encuesta.')
      );
    } finally {
      setActiveActionId(null);
    }
  }

  async function handleCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!accessToken) return;
    if (!createForm.title.trim()) {
      setCreateError('Ingresá un título para identificar la plantilla.');
      return;
    }

    setIsCreating(true);
    setCreateError(null);
    try {
      const created = await createSurvey(
        {
          title: createForm.title.trim(),
          description: trimmedOrNull(createForm.description),
          target: createForm.target,
          isAnonymous: createForm.isAnonymous
        },
        accessToken,
        auth.logout
      );
      setShowCreateModal(false);
      setCreateForm(initialCreateForm);
      toast.success('Plantilla creada', 'Ahora podés agregar secciones y preguntas.');
      navigate(`/app/surveys/${created.id}/edit`);
    } catch (error) {
      const message = getFriendlySurveyError(error, 'No fue posible crear la encuesta.');
      setCreateError(message);
      toast.error('No se pudo crear la plantilla', message);
    } finally {
      setIsCreating(false);
    }
  }

  function clearFilters() {
    setSearch('');
    setActivityFilter('');
    setSortBy('updated-desc');
    setFilters(initialFilters);
  }

  const hasFilters = Boolean(search || activityFilter || filters.status || filters.target || !filters.includeInactive || sortBy !== 'updated-desc');

  return (
    <section className="app-content surveys-page">
      <header className="surveys-header surveys-header--templates">
        <div>
          <p className="eyebrow">Administración</p>
          <h2>Plantillas de encuestas</h2>
          <p>Buscá, filtrá y administrá las plantillas que luego se asignan a carreras, materias y docentes.</p>
        </div>
        <div className="surveys-actions">
          <button className="secondary-button" disabled={!accessToken || loadState === 'loading'} onClick={() => accessToken && void loadSurveys(accessToken)} type="button">
            Actualizar
          </button>
          <button className="primary-button" onClick={() => setShowCreateModal(true)} type="button">
            Nueva plantilla
          </button>
        </div>
      </header>

      <div className="template-summary-grid" aria-label="Resumen de plantillas">
        <div><span>Total</span><strong>{summary.total}</strong></div>
        <div><span>Borradores</span><strong>{summary.drafts}</strong></div>
        <div><span>Publicadas</span><strong>{summary.published}</strong></div>
        <div><span>Archivadas</span><strong>{summary.archived}</strong></div>
      </div>

      <section className="filter-panel" aria-label="Buscar y filtrar plantillas">
        <header className="filter-panel__header">
          <div>
            <h3>Buscar plantillas</h3>
            <p>Usá uno o más filtros para encontrar rápidamente la encuesta que necesitás.</p>
          </div>
          {hasFilters ? <button className="link-button" onClick={clearFilters} type="button">Limpiar filtros</button> : null}
        </header>
        <div className="template-filter-grid">
          <label className="filter-search-field">
            <span>Buscar por nombre o descripción</span>
            <input
              className="text-input"
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Ej.: evaluación docente"
              type="search"
              value={search}
            />
          </label>
          <label>
            <span>Estado</span>
            <select className="text-input" onChange={(event) => setFilters((current) => ({ ...current, status: event.target.value as SurveyStatus | '' }))} value={filters.status}>
              <option value="">Todos</option>
              {SURVEY_STATUSES.map((status) => <option key={status} value={status}>{SURVEY_STATUS_LABELS[status]}</option>)}
            </select>
          </label>
          <label>
            <span>Audiencia</span>
            <select className="text-input" onChange={(event) => setFilters((current) => ({ ...current, target: event.target.value as SurveyTarget | '' }))} value={filters.target}>
              <option value="">Todas</option>
              {SURVEY_TARGETS.map((target) => <option key={target} value={target}>{formatSurveyTarget(target)}</option>)}
            </select>
          </label>
          <label>
            <span>Disponibilidad</span>
            <select className="text-input" onChange={(event) => setActivityFilter(event.target.value as ActivityFilter)} value={activityFilter}>
              <option value="">Todas</option>
              <option value="active">Activas</option>
              <option value="inactive">Inactivas</option>
            </select>
          </label>
          <label>
            <span>Ordenar</span>
            <select className="text-input" onChange={(event) => setSortBy(event.target.value as SortOption)} value={sortBy}>
              <option value="updated-desc">Actualizadas recientemente</option>
              <option value="updated-asc">Actualizadas hace más tiempo</option>
              <option value="title-asc">Nombre A-Z</option>
              <option value="title-desc">Nombre Z-A</option>
            </select>
          </label>
          <label className="checkbox-field filter-checkbox">
            <input checked={filters.includeInactive} onChange={(event) => setFilters((current) => ({ ...current, includeInactive: event.target.checked }))} type="checkbox" />
            <span>Incluir plantillas inactivas</span>
          </label>
        </div>
        <div className="filter-results-count" aria-live="polite">
          {filteredSurveys.length} {filteredSurveys.length === 1 ? 'plantilla encontrada' : 'plantillas encontradas'}
        </div>
      </section>

      {loadState === 'loading' ? <p aria-live="polite">Cargando plantillas...</p> : null}
      {loadState === 'error' ? (
        <div className="empty-detail" role="alert"><h3>No pudimos cargar las plantillas</h3><p>{pageError}</p></div>
      ) : null}
      {loadState === 'ready' && filteredSurveys.length === 0 ? (
        <div className="empty-detail">
          <h3>{surveys.length === 0 ? 'Todavía no hay plantillas' : 'No encontramos coincidencias'}</h3>
          <p>{surveys.length === 0 ? 'Creá la primera plantilla para comenzar.' : 'Probá cambiar o limpiar los filtros de búsqueda.'}</p>
        </div>
      ) : null}

      {loadState === 'ready' && filteredSurveys.length > 0 ? (
        <>
          <PaginationControls
            firstItem={surveyPagination.firstItem}
            itemLabel="plantillas"
            lastItem={surveyPagination.lastItem}
            onPageChange={surveyPagination.setPage}
            onPageSizeChange={surveyPagination.setPageSize}
            page={surveyPagination.page}
            pageSize={surveyPagination.pageSize}
            totalItems={surveyPagination.totalItems}
            totalPages={surveyPagination.totalPages}
          />
          <div className="surveys-table" role="list">
            {surveyPagination.items.map((survey) => (
              <article className="survey-list-card template-card" key={survey.id} role="listitem">
                <header>
                  <div>
                    <div className="template-card__title-row">
                      <h3>{survey.title}</h3>
                      <SurveyVersionBadge survey={survey} />
                    </div>
                    <p>{survey.description || 'Sin descripción'}</p>
                  </div>
                  <div className="badge-group">
                    <SurveyStatusBadge status={survey.status} />
                    <ActivityBadge isActive={survey.isActive} />
                  </div>
                </header>
                <dl className="survey-card-meta">
                  <div><dt>Audiencia</dt><dd>{formatSurveyTarget(survey.target)}</dd></div>
                  <div><dt>Secciones</dt><dd>{survey.sectionCount}</dd></div>
                  <div><dt>Preguntas</dt><dd>{survey.questionCount}</dd></div>
                  <div><dt>Actualizada</dt><dd>{formatDateTime(survey.updatedAtUtc)}</dd></div>
                </dl>
                <div className="survey-card-actions">
                  <Link className="secondary-link-button" to={`/app/surveys/${survey.id}/preview`}>Vista previa</Link>
                  <button className="secondary-button" disabled={activeActionId === survey.id} onClick={() => void handleEdit(survey)} type="button">
                    {activeActionId === survey.id ? 'Preparando...' : 'Editar'}
                  </button>
                  {survey.status === 'Published' ? (
                    <button className="danger-button" disabled={activeActionId === survey.id} onClick={() => setArchiveTarget(survey)} type="button">Archivar</button>
                  ) : null}
                </div>
              </article>
            ))}
          </div>
        </>
      ) : null}

      <Modal
        closeDisabled={isCreating}
        description="Completá los datos básicos. La plantilla se creará como borrador y luego podrás diseñar sus preguntas."
        onClose={() => { setShowCreateModal(false); setCreateError(null); }}
        open={showCreateModal}
        title="Nueva plantilla de encuesta"
      >
        <form className="modal-form" noValidate onSubmit={handleCreate}>
          <label><span>Título</span><input autoFocus className="text-input" maxLength={200} onChange={(event) => { setCreateForm((current) => ({ ...current, title: event.target.value })); setCreateError(null); }} required type="text" value={createForm.title} /></label>
          <label><span>Descripción <small>(opcional)</small></span><textarea className="text-area" maxLength={1000} onChange={(event) => setCreateForm((current) => ({ ...current, description: event.target.value }))} rows={4} value={createForm.description} /></label>
          <label><span>¿Quién responderá?</span><select className="text-input" onChange={(event) => setCreateForm((current) => ({ ...current, target: event.target.value as SurveyTarget }))} value={createForm.target}>{SURVEY_TARGETS.map((target) => <option key={target} value={target}>{formatSurveyTarget(target)}</option>)}</select></label>
          <label className="checkbox-field"><input checked={createForm.isAnonymous} onChange={(event) => setCreateForm((current) => ({ ...current, isAnonymous: event.target.checked }))} type="checkbox" /><span>Respuestas anónimas</span></label>
          {createError ? <p className="submit-error" role="alert">{createError}</p> : null}
          <div className="modal-footer-actions">
            <button className="secondary-button" disabled={isCreating} onClick={() => setShowCreateModal(false)} type="button">Cancelar</button>
            <button className="primary-button" disabled={isCreating} type="submit">{isCreating ? 'Creando...' : 'Crear y continuar'}</button>
          </div>
        </form>
      </Modal>

      <ConfirmDialog
        busy={Boolean(archiveTarget && activeActionId === archiveTarget.id)}
        confirmLabel="Archivar plantilla"
        message="La plantilla dejará de utilizarse para nuevas operaciones, pero sus asignaciones y resultados históricos se conservarán."
        onCancel={() => setArchiveTarget(null)}
        onConfirm={() => void confirmArchive()}
        open={archiveTarget !== null}
        title="¿Archivar esta plantilla?"
        tone="danger"
      />
    </section>
  );
}
