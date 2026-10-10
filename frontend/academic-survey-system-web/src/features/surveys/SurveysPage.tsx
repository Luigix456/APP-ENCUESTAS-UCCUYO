import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { getSurveyAssignments } from '../../api/surveyAssignmentsApi';
import {
  archiveSurvey,
  createSurvey,
  getOrCreateEditableSurveyVersion,
  getSurveys
} from '../../api/surveysApi';
import { useAuth } from '../../auth/AuthProvider';
import { ConfirmDialog, Modal } from '../../components/ui/Modal';
import { useToast } from '../../components/ui/ToastProvider';
import type { SurveyAssignmentDto } from '../../types/surveyAssignments';
import type { SurveyFilters, SurveyStatus, SurveySummaryDto, SurveyTarget } from '../../types/surveys';
import { SURVEY_STATUSES, SURVEY_STATUS_LABELS, SURVEY_TARGETS } from '../../types/surveys';
import {
  formatDateTime,
  formatSurveyTarget,
  getFriendlySurveyError,
  MANAGE_SURVEY_TEMPLATES_PERMISSION,
  READ_SURVEY_TEMPLATES_PERMISSION,
  PermissionDeniedPanel,
  SurveyStatusBadge,
  SurveyVersionBadge,
  trimmedOrNull
} from './surveyUi';

type LoadState = 'loading' | 'ready' | 'error';
type FamilyStatusFilter = SurveyStatus | '';

interface SurveyCreateFormState {
  title: string;
  description: string;
  target: SurveyTarget;
  isAnonymous: boolean;
}

interface SurveyTemplateFamily {
  versionGroupId: string;
  title: string;
  description: string | null;
  target: SurveyTarget;
  primary: SurveySummaryDto;
  draft: SurveySummaryDto | null;
  published: SurveySummaryDto | null;
  archived: SurveySummaryDto | null;
  versions: SurveySummaryDto[];
  sectionCount: number;
  questionCount: number;
  updatedAtUtc: string;
}

interface SurveyTemplateInCareer extends SurveyTemplateFamily {
  assignmentCount: number;
  subjectNames: string[];
}

interface SurveyCareerGroup {
  careerId: string;
  careerName: string;
  academicUnitName: string | null;
  templates: SurveyTemplateInCareer[];
  isUnassigned?: boolean;
}

interface SurveyCareerCatalog {
  careerGroups: SurveyCareerGroup[];
  unassignedTemplates: SurveyTemplateInCareer[];
  allFamilies: SurveyTemplateFamily[];
}

interface FilteredCatalog {
  careerGroups: SurveyCareerGroup[];
  unassignedTemplates: SurveyTemplateInCareer[];
}

const unassignedCareerId = 'unassigned-templates';

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
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<FamilyStatusFilter>('');
  const [targetFilter, setTargetFilter] = useState<SurveyTarget | ''>('');
  const [surveys, setSurveys] = useState<SurveySummaryDto[]>([]);
  const [assignments, setAssignments] = useState<SurveyAssignmentDto[]>([]);
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [expandedCareerId, setExpandedCareerId] = useState<string | null>(null);
  const [expandedFamilyIds, setExpandedFamilyIds] = useState<Set<string>>(new Set());
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
  const canReadTemplates = canManageTemplates || auth.hasPermission(READ_SURVEY_TEMPLATES_PERMISSION);

  useEffect(() => {
    if (!canReadTemplates || !accessToken) {
      setLoadState('ready');
      return;
    }
    void loadSurveys(accessToken, targetFilter);
  }, [accessToken, canReadTemplates, targetFilter]);

  const catalog = useMemo(
    () => buildSurveyCatalogByCareer(surveys, assignments),
    [assignments, surveys]
  );
  const catalogForStatusCounts = useMemo(
    () => filterCatalog(catalog, search, ''),
    [catalog, search]
  );
  const filteredCatalog = useMemo(
    () => filterCatalog(catalog, search, statusFilter),
    [catalog, search, statusFilter]
  );
  const uniqueTemplateCount = useMemo(
    () => countUniqueTemplates(filteredCatalog),
    [filteredCatalog]
  );
  const statusCounts = useMemo(
    () => countCatalogByStatus(catalogForStatusCounts),
    [catalogForStatusCounts]
  );
  const visibleCareerCount = filteredCatalog.careerGroups.length;
  const hasVisibleUnassigned = filteredCatalog.unassignedTemplates.length > 0;
  const hasVisibleTemplates = visibleCareerCount > 0 || hasVisibleUnassigned;
  const hasFilters = Boolean(search || statusFilter || targetFilter);
  const advancedFilterCount = targetFilter ? 1 : 0;

  if (!canReadTemplates) return <PermissionDeniedPanel mode="read" />;

  async function loadSurveys(token: string, nextTargetFilter = targetFilter) {
    setLoadState('loading');
    setPageError(null);
    try {
      const surveyFilters: SurveyFilters = {
        includeInactive: true,
        status: '',
        target: nextTargetFilter
      };
      const [nextSurveys, nextAssignments] = await Promise.all([
        getSurveys(surveyFilters, token, auth.logout),
        getSurveyAssignments(token, auth.logout, { includeInactive: true })
      ]);
      setSurveys(nextSurveys);
      setAssignments(nextAssignments);
      setLoadState('ready');
    } catch (error) {
      setPageError(getFriendlySurveyError(error, 'No fue posible cargar las plantillas.'));
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
    setStatusFilter('');
    setTargetFilter('');
  }

  function toggleCareer(careerId: string) {
    setExpandedCareerId((current) => current === careerId ? null : careerId);
  }

  function toggleVersions(versionGroupId: string) {
    setExpandedFamilyIds((current) => {
      const next = new Set(current);
      if (next.has(versionGroupId)) {
        next.delete(versionGroupId);
      } else {
        next.add(versionGroupId);
      }
      return next;
    });
  }

  return (
    <section className="app-content surveys-page">
      <header className="surveys-header surveys-header--templates">
        <div>
          <p className="eyebrow">Plantillas de encuestas</p>
          <h2>Plantillas</h2>
          <p>Administrá las plantillas organizadas por carrera.</p>
        </div>
        <div className="surveys-actions">
          <button className="secondary-button" disabled={!accessToken || loadState === 'loading'} onClick={() => accessToken && void loadSurveys(accessToken)} type="button">
            Actualizar
          </button>
          {canManageTemplates ? (
            <button className="primary-button" onClick={() => setShowCreateModal(true)} type="button">
              + Nueva plantilla
            </button>
          ) : null}
        </div>
      </header>

      <section className="survey-template-toolbar" aria-label="Buscar y filtrar plantillas">
        <div className="survey-template-toolbar__main survey-template-toolbar__main--career">
          <label className="survey-search-input">
            <span className="sr-only">Buscar carrera o plantilla</span>
            <svg aria-hidden="true" viewBox="0 0 24 24"><path d="m21 21-4.35-4.35m2.35-5.65a8 8 0 1 1-16 0 8 8 0 0 1 16 0Z" /></svg>
            <input
              className="text-input"
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Buscar carrera o plantilla"
              type="search"
              value={search}
            />
          </label>
          <label className="survey-template-status-filter">
            <span className="sr-only">Estado</span>
            <select className="text-input" onChange={(event) => setStatusFilter(event.target.value as FamilyStatusFilter)} value={statusFilter}>
              <option value="">Todos los estados</option>
              {SURVEY_STATUSES.map((status) => (
                <option key={status} value={status}>
                  {SURVEY_STATUS_LABELS[status]} ({statusCounts[status]})
                </option>
              ))}
            </select>
          </label>
          <button
            aria-controls="survey-template-advanced-filters"
            aria-expanded={filtersOpen}
            aria-label={filtersOpen ? 'Ocultar filtros' : 'Mostrar filtros'}
            className={`filter-toggle-button ${filtersOpen ? 'filter-toggle-button--active' : ''}`}
            onClick={() => setFiltersOpen((current) => !current)}
            title={filtersOpen ? 'Ocultar filtros' : 'Mostrar filtros'}
            type="button"
          >
            <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M4 5h16M7 12h10M10 19h4" /></svg>
            <span>Filtros</span>
            {advancedFilterCount > 0 ? <strong>{advancedFilterCount}</strong> : null}
          </button>
          {hasFilters ? <button className="link-button" onClick={clearFilters} type="button">Limpiar filtros</button> : null}
        </div>

        {filtersOpen ? (
          <div className="survey-template-advanced-filters" id="survey-template-advanced-filters">
            <label>
              <span>Audiencia</span>
              <select className="text-input" onChange={(event) => setTargetFilter(event.target.value as SurveyTarget | '')} value={targetFilter}>
                <option value="">Todas</option>
                {SURVEY_TARGETS.map((target) => <option key={target} value={target}>{formatSurveyTarget(target)}</option>)}
              </select>
            </label>
          </div>
        ) : null}

        <div className="survey-template-count" aria-live="polite">
          <strong>{visibleCareerCount}</strong> {visibleCareerCount === 1 ? 'carrera con plantillas' : 'carreras con plantillas'}
          <span>·</span>
          <strong>{uniqueTemplateCount}</strong> {uniqueTemplateCount === 1 ? 'plantilla única' : 'plantillas únicas'}
          {hasVisibleUnassigned ? <span>· Incluye plantillas sin asignar</span> : null}
        </div>
      </section>

      {loadState === 'loading' ? <p className="survey-template-loading" aria-live="polite">Cargando plantillas...</p> : null}
      {loadState === 'error' ? (
        <div className="survey-template-empty survey-template-empty--compact" role="alert">
          <h3>No pudimos cargar las plantillas.</h3>
          <p>{pageError}</p>
          <button className="secondary-button" disabled={!accessToken} onClick={() => accessToken && void loadSurveys(accessToken)} type="button">
            Reintentar
          </button>
        </div>
      ) : null}
      {loadState === 'ready' && catalog.allFamilies.length === 0 ? (
        <div className="survey-template-empty">
          <h3>No hay plantillas de encuestas todavía.</h3>
          <p>Creá una plantilla para empezar a diseñar encuestas académicas.</p>
          {canManageTemplates ? (
            <button className="primary-button" onClick={() => setShowCreateModal(true)} type="button">
              Crear primera plantilla
            </button>
          ) : null}
        </div>
      ) : null}
      {loadState === 'ready' && catalog.allFamilies.length > 0 && !hasVisibleTemplates ? (
        <div className="survey-template-empty">
          <h3>No hay plantillas que coincidan con los filtros.</h3>
          <p>Probá ajustar la búsqueda, el estado o la audiencia.</p>
          <button className="secondary-button" onClick={clearFilters} type="button">Limpiar filtros</button>
        </div>
      ) : null}

      {loadState === 'ready' && hasVisibleTemplates ? (
        <div className="survey-career-list">
          {filteredCatalog.careerGroups.map((group) => (
            <SurveyCareerGroupPanel
              activeActionId={activeActionId}
              canManageTemplates={canManageTemplates}
              expanded={expandedCareerId === group.careerId}
              expandedFamilyIds={expandedFamilyIds}
              group={group}
              key={group.careerId}
              onArchive={setArchiveTarget}
              onEdit={handleEdit}
              onToggleCareer={toggleCareer}
              onToggleVersions={toggleVersions}
            />
          ))}
          {hasVisibleUnassigned ? (
            <SurveyCareerGroupPanel
              activeActionId={activeActionId}
              canManageTemplates={canManageTemplates}
              expanded={expandedCareerId === unassignedCareerId}
              expandedFamilyIds={expandedFamilyIds}
              group={{
                careerId: unassignedCareerId,
                careerName: 'Plantillas sin asignar',
                academicUnitName: null,
                templates: filteredCatalog.unassignedTemplates,
                isUnassigned: true
              }}
              key={unassignedCareerId}
              onArchive={setArchiveTarget}
              onEdit={handleEdit}
              onToggleCareer={toggleCareer}
              onToggleVersions={toggleVersions}
            />
          ) : null}
        </div>
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

function SurveyCareerGroupPanel({
  activeActionId,
  canManageTemplates,
  expanded,
  expandedFamilyIds,
  group,
  onArchive,
  onEdit,
  onToggleCareer,
  onToggleVersions
}: {
  activeActionId: string | null;
  canManageTemplates: boolean;
  expanded: boolean;
  expandedFamilyIds: Set<string>;
  group: SurveyCareerGroup;
  onArchive: (survey: SurveySummaryDto) => void;
  onEdit: (survey: SurveySummaryDto) => Promise<void>;
  onToggleCareer: (careerId: string) => void;
  onToggleVersions: (versionGroupId: string) => void;
}) {
  const contentId = `survey-career-group-${group.careerId}`;

  return (
    <section className={`survey-career-group ${group.isUnassigned ? 'survey-career-group--unassigned' : ''}`}>
      <button
        aria-controls={contentId}
        aria-expanded={expanded}
        className="survey-career-group__trigger"
        onClick={() => onToggleCareer(group.careerId)}
        type="button"
      >
        <span className="survey-career-group__identity">
          <strong className="survey-career-group__name">{group.careerName}</strong>
          {group.academicUnitName ? <span className="survey-career-group__unit">{group.academicUnitName}</span> : null}
        </span>
        <span className="survey-career-group__count">{formatTemplateCount(group.templates.length)}</span>
        <svg aria-hidden="true" viewBox="0 0 24 24"><path d="m6 9 6 6 6-6" /></svg>
      </button>

      {expanded ? (
        <div className="survey-career-group__content" id={contentId}>
          {group.templates.length > 0 ? (
            <div className="survey-template-grid">
              {group.templates.map((template) => (
                <SurveyTemplateFamilyCard
                  activeActionId={activeActionId}
                  canManageTemplates={canManageTemplates}
                  expanded={expandedFamilyIds.has(template.versionGroupId)}
                  key={template.versionGroupId}
                  onArchive={onArchive}
                  onEdit={onEdit}
                  onToggleVersions={onToggleVersions}
                  template={template}
                />
              ))}
            </div>
          ) : (
            <div className="survey-template-empty survey-template-empty--compact">
              <h3>No hay plantillas que coincidan con los filtros.</h3>
              <p>Probá ajustar la búsqueda o el estado seleccionado.</p>
            </div>
          )}
        </div>
      ) : null}
    </section>
  );
}

function SurveyTemplateFamilyCard({
  activeActionId,
  canManageTemplates,
  expanded,
  onArchive,
  onEdit,
  onToggleVersions,
  template
}: {
  activeActionId: string | null;
  canManageTemplates: boolean;
  expanded: boolean;
  onArchive: (survey: SurveySummaryDto) => void;
  onEdit: (survey: SurveySummaryDto) => Promise<void>;
  onToggleVersions: (versionGroupId: string) => void;
  template: SurveyTemplateInCareer;
}) {
  const editableVersion = template.draft;
  const viewVersion = template.published ?? template.primary;
  const versionListId = `survey-template-versions-${template.versionGroupId}`;

  return (
    <article className="survey-template-family-card">
      <header className="survey-template-family-card__header">
        <div className="survey-template-family-card__title">
          <h3>{template.title}</h3>
          <p>{template.description || 'Sin descripción'}</p>
        </div>
        <span className="survey-template-family-card__target">{formatSurveyTarget(template.target)}</span>
      </header>

      <div className="survey-template-family-card__statuses" aria-label="Estado de versiones principales">
        {template.published ? (
          <div>
            <SurveyStatusBadge status="Published" />
            <SurveyVersionBadge survey={template.published} />
          </div>
        ) : null}
        {template.draft ? (
          <div className="survey-template-family-card__draft">
            <SurveyStatusBadge status="Draft" />
            <SurveyVersionBadge survey={template.draft} />
            <span>Borrador en edición</span>
          </div>
        ) : null}
        {!template.draft && !template.published && template.archived ? (
          <div>
            <SurveyStatusBadge status="Archived" />
            <SurveyVersionBadge survey={template.archived} />
          </div>
        ) : null}
      </div>

      <dl className="survey-template-family-card__meta">
        <div><dt>Contenido</dt><dd>{template.sectionCount} {template.sectionCount === 1 ? 'sección' : 'secciones'} · {template.questionCount} {template.questionCount === 1 ? 'pregunta' : 'preguntas'}</dd></div>
        <div><dt>Asignaciones</dt><dd>{formatCareerAssignmentCount(template.assignmentCount)}</dd></div>
        {template.subjectNames.length > 0 ? (
          <div><dt>Asignada en</dt><dd>{formatSubjectNames(template.subjectNames)}</dd></div>
        ) : null}
        <div><dt>Actualizada</dt><dd>{formatDateTime(template.updatedAtUtc)}</dd></div>
      </dl>

      <div className="survey-template-family-card__actions">
        {editableVersion && canManageTemplates ? (
          <button className="primary-button" disabled={activeActionId === editableVersion.id} onClick={() => void onEdit(editableVersion)} type="button">
            {activeActionId === editableVersion.id ? 'Abriendo...' : 'Editar borrador'}
          </button>
        ) : (
          <Link className="primary-link-button" to={`/app/surveys/${viewVersion.id}/preview`}>Ver</Link>
        )}
        {!editableVersion && template.published && canManageTemplates ? (
          <button className="secondary-button" disabled={activeActionId === template.published.id} onClick={() => void onEdit(template.published!)} type="button">
            {activeActionId === template.published.id ? 'Preparando...' : 'Crear versión editable'}
          </button>
        ) : null}
        {template.published && canManageTemplates ? (
          <Link className="secondary-link-button" to="/app/survey-assignments/new">Asignar</Link>
        ) : null}
        {editableVersion ? (
          <Link className="secondary-link-button" to={`/app/surveys/${viewVersion.id}/preview`}>Vista previa</Link>
        ) : null}
        {template.published && canManageTemplates ? (
          <button className="text-danger-button" disabled={activeActionId === template.published.id} onClick={() => onArchive(template.published!)} type="button">Archivar</button>
        ) : null}
      </div>

      <button
        aria-controls={versionListId}
        aria-expanded={expanded}
        className="survey-template-family-card__versions-toggle"
        onClick={() => onToggleVersions(template.versionGroupId)}
        type="button"
      >
        <span>Ver versiones</span>
        <strong>{template.versions.length}</strong>
        <svg aria-hidden="true" viewBox="0 0 24 24"><path d="m6 9 6 6 6-6" /></svg>
      </button>

      {expanded ? (
        <div className="survey-template-version-list" id={versionListId}>
          {template.versions.map((version) => (
            <div className="survey-template-version-row" key={version.id}>
              <div className="survey-template-version-row__status">
                <SurveyVersionBadge survey={version} />
                <SurveyStatusBadge status={version.status} />
              </div>
              <div className="survey-template-version-row__meta">
                <span>{version.sectionCount} {version.sectionCount === 1 ? 'sección' : 'secciones'}</span>
                <span>{version.questionCount} {version.questionCount === 1 ? 'pregunta' : 'preguntas'}</span>
                <span>{formatDateTime(version.updatedAtUtc)}</span>
              </div>
              <div className="survey-template-version-row__actions">
                <Link className="secondary-link-button" to={`/app/surveys/${version.id}/preview`}>Ver</Link>
                {version.status === 'Draft' && canManageTemplates ? (
                  <Link className="primary-link-button" to={`/app/surveys/${version.id}/edit`}>Editar</Link>
                ) : null}
              </div>
            </div>
          ))}
        </div>
      ) : null}
    </article>
  );
}

function buildSurveyCatalogByCareer(
  surveys: SurveySummaryDto[],
  assignments: SurveyAssignmentDto[]
): SurveyCareerCatalog {
  const families = groupSurveyFamilies(surveys);
  const familyByVersionGroupId = new Map(families.map((family) => [family.versionGroupId, family]));
  const surveyById = new Map(surveys.map((survey) => [survey.id, survey]));
  const assignedVersionGroupIds = new Set<string>();
  const careerMap = new Map<string, {
    careerId: string;
    careerName: string;
    academicUnitName: string | null;
    templates: Map<string, SurveyTemplateInCareer & { assignmentIds: Set<string>; subjectSet: Set<string> }>;
  }>();

  assignments.forEach((assignment) => {
    const survey = surveyById.get(assignment.surveyId);
    if (!survey) return;
    const family = familyByVersionGroupId.get(survey.versionGroupId);
    if (!family) return;

    assignedVersionGroupIds.add(family.versionGroupId);
    const careerGroup = careerMap.get(assignment.careerId) ?? {
      careerId: assignment.careerId,
      careerName: assignment.careerName,
      academicUnitName: null,
      templates: new Map<string, SurveyTemplateInCareer & { assignmentIds: Set<string>; subjectSet: Set<string> }>()
    };
    const template = careerGroup.templates.get(family.versionGroupId) ?? {
      ...family,
      assignmentCount: 0,
      subjectNames: [],
      assignmentIds: new Set<string>(),
      subjectSet: new Set<string>()
    };

    template.assignmentIds.add(assignment.id);
    template.subjectSet.add(assignment.subjectName);
    template.assignmentCount = template.assignmentIds.size;
    template.subjectNames = [...template.subjectSet].sort((left, right) => left.localeCompare(right, 'es'));
    careerGroup.templates.set(family.versionGroupId, template);
    careerMap.set(assignment.careerId, careerGroup);
  });

  const careerGroups = [...careerMap.values()]
    .map((group) => ({
      careerId: group.careerId,
      careerName: group.careerName,
      academicUnitName: group.academicUnitName,
      templates: [...group.templates.values()]
        .sort(compareTemplates)
    }))
    .sort((left, right) => left.careerName.localeCompare(right.careerName, 'es'));

  const unassignedTemplates = families
    .filter((family) => !assignedVersionGroupIds.has(family.versionGroupId))
    .map((family) => ({
      ...family,
      assignmentCount: 0,
      subjectNames: []
    }))
    .sort(compareTemplates);

  return {
    careerGroups,
    unassignedTemplates,
    allFamilies: families
  };
}

function groupSurveyFamilies(surveys: SurveySummaryDto[]): SurveyTemplateFamily[] {
  const groups = surveys.reduce<Map<string, SurveySummaryDto[]>>((map, survey) => {
    const versions = map.get(survey.versionGroupId) ?? [];
    versions.push(survey);
    map.set(survey.versionGroupId, versions);
    return map;
  }, new Map<string, SurveySummaryDto[]>());

  return [...groups.entries()]
    .map(([versionGroupId, versions]) => buildSurveyFamily(versionGroupId, versions))
    .sort(compareTemplates);
}

function buildSurveyFamily(versionGroupId: string, versions: SurveySummaryDto[]): SurveyTemplateFamily {
  const orderedVersions = [...versions].sort(compareSurveyVersionsDescending);
  const draft = orderedVersions.find((survey) => survey.status === 'Draft') ?? null;
  const published = orderedVersions.find((survey) => survey.status === 'Published') ?? null;
  const archived = orderedVersions.find((survey) => survey.status === 'Archived') ?? null;
  const primary = draft ?? published ?? archived ?? orderedVersions[0];

  return {
    versionGroupId,
    title: primary.title,
    description: primary.description,
    target: primary.target,
    primary,
    draft,
    published,
    archived,
    versions: orderedVersions,
    sectionCount: primary.sectionCount,
    questionCount: primary.questionCount,
    updatedAtUtc: primary.updatedAtUtc
  };
}

function filterCatalog(catalog: SurveyCareerCatalog, search: string, status: FamilyStatusFilter): FilteredCatalog {
  const normalizedSearch = search.trim().toLocaleLowerCase('es');
  const careerGroups = catalog.careerGroups
    .map((group) => {
      const careerMatches = normalizedSearch
        ? group.careerName.toLocaleLowerCase('es').includes(normalizedSearch)
        : false;
      const templates = group.templates.filter((template) => (
        familyMatchesStatus(template, status) &&
        (careerMatches || templateMatchesSearch(template, normalizedSearch))
      ));

      return { ...group, templates };
    })
    .filter((group) => group.templates.length > 0);

  const unassignedSearchMatches = normalizedSearch
    ? 'plantillas sin asignar'.includes(normalizedSearch) || 'sin asignar a una carrera'.includes(normalizedSearch)
    : false;
  const unassignedTemplates = catalog.unassignedTemplates.filter((template) => (
    familyMatchesStatus(template, status) &&
    (unassignedSearchMatches || templateMatchesSearch(template, normalizedSearch))
  ));

  return { careerGroups, unassignedTemplates };
}

function templateMatchesSearch(template: SurveyTemplateFamily, normalizedSearch: string): boolean {
  if (!normalizedSearch) return true;
  return (
    template.title.toLocaleLowerCase('es').includes(normalizedSearch) ||
    template.versions.some((version) => version.title.toLocaleLowerCase('es').includes(normalizedSearch))
  );
}

function familyMatchesStatus(template: SurveyTemplateFamily, status: FamilyStatusFilter): boolean {
  if (!status) return true;
  if (status === 'Draft') return Boolean(template.draft);
  if (status === 'Published') return Boolean(template.published);
  return template.primary.status === 'Archived';
}

function countUniqueTemplates(catalog: FilteredCatalog): number {
  const ids = new Set<string>();
  catalog.careerGroups.forEach((group) => {
    group.templates.forEach((template) => ids.add(template.versionGroupId));
  });
  catalog.unassignedTemplates.forEach((template) => ids.add(template.versionGroupId));
  return ids.size;
}

function countCatalogByStatus(catalog: FilteredCatalog): Record<SurveyStatus, number> {
  const familiesById = new Map<string, SurveyTemplateInCareer>();
  catalog.careerGroups.forEach((group) => {
    group.templates.forEach((template) => familiesById.set(template.versionGroupId, template));
  });
  catalog.unassignedTemplates.forEach((template) => familiesById.set(template.versionGroupId, template));
  const families = [...familiesById.values()];

  return {
    Draft: families.filter((family) => Boolean(family.draft)).length,
    Published: families.filter((family) => Boolean(family.published)).length,
    Archived: families.filter((family) => family.primary.status === 'Archived').length
  };
}

function compareSurveyVersionsDescending(left: SurveySummaryDto, right: SurveySummaryDto): number {
  const versionComparison = right.versionNumber - left.versionNumber;
  if (versionComparison !== 0) return versionComparison;
  return Date.parse(right.updatedAtUtc) - Date.parse(left.updatedAtUtc);
}

function compareTemplates(left: SurveyTemplateFamily, right: SurveyTemplateFamily): number {
  const priorityComparison = getTemplatePriority(left) - getTemplatePriority(right);
  if (priorityComparison !== 0) return priorityComparison;
  return left.title.localeCompare(right.title, 'es');
}

function getTemplatePriority(template: SurveyTemplateFamily): number {
  if (template.draft) return 0;
  if (template.published) return 1;
  return 2;
}

function formatTemplateCount(count: number): string {
  return count === 1 ? '1 plantilla' : `${count} plantillas`;
}

function formatCareerAssignmentCount(count: number): string {
  if (count === 0) return 'Sin asignaciones';
  return count === 1 ? '1 asignación en esta carrera' : `${count} asignaciones en esta carrera`;
}

function formatSubjectNames(subjectNames: string[]): string {
  if (subjectNames.length <= 3) {
    return subjectNames.join(' · ');
  }

  return `${subjectNames.length} materias`;
}
