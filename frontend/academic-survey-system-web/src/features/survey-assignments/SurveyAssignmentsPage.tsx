import { useEffect, useMemo, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import {
  getAcademicCycles,
  getCareerTeachers,
  getCareers,
  getTeachers
} from '../../api/academicCatalogApi';
import {
  activateSurveyAssignment,
  deactivateSurveyAssignment,
  getSurveyAssignments
} from '../../api/surveyAssignmentsApi';
import { getSurveys } from '../../api/surveysApi';
import { useAuth } from '../../auth/AuthProvider';
import { PaginationControls, usePagination } from '../../components/Pagination';
import { ConfirmDialog } from '../../components/ui/Modal';
import { useToast } from '../../components/ui/ToastProvider';
import type { AcademicCycleDto, CareerDto, TeacherDto } from '../../types/academicCatalog';
import type { SurveyAssignmentDto, SurveyAssignmentFilters } from '../../types/surveyAssignments';
import type { SurveySummaryDto } from '../../types/surveys';
import { ActivityBadge } from '../surveys/surveyUi';
import { useAcademicContext } from '../academic-context/AcademicContextProvider';
import {
  formatAcademicCycle,
  formatAcademicCycleParts,
  formatSurveyStatus,
  formatSurveyOption,
  formatTeacher,
  getFriendlyAssignmentError,
  MANAGE_SURVEY_ASSIGNMENTS_PERMISSION,
  READ_ACADEMIC_CATALOG_PERMISSION,
  READ_SURVEY_ASSIGNMENTS_PERMISSION,
  SurveyAssignmentPermissionPanel
} from './surveyAssignmentUi';

type LoadState = 'loading' | 'ready' | 'error';

const initialFilters: SurveyAssignmentFilters = {
  includeInactive: true,
  surveyId: '',
  careerId: '',
  subjectId: '',
  academicCycleId: '',
  teacherId: ''
};

export function SurveyAssignmentsPage({ contextual = false }: { contextual?: boolean }) {
  const auth = useAuth();
  const toast = useToast();
  const academicContext = useAcademicContext();
  const location = useLocation();
  const navigate = useNavigate();
  const [assignments, setAssignments] = useState<SurveyAssignmentDto[]>([]);
  const [surveys, setSurveys] = useState<SurveySummaryDto[]>([]);
  const [careers, setCareers] = useState<CareerDto[]>([]);
  const [cycles, setCycles] = useState<AcademicCycleDto[]>([]);
  const [teachers, setTeachers] = useState<TeacherDto[]>([]);
  const [filters, setFilters] = useState<SurveyAssignmentFilters>(initialFilters);
  const [loadState, setLoadState] = useState<LoadState>('loading');
  const [pageError, setPageError] = useState<string | null>(null);
  const [activeActionId, setActiveActionId] = useState<string | null>(null);
  const [toggleTarget, setToggleTarget] = useState<SurveyAssignmentDto | null>(null);

  const accessToken = auth.accessToken;
  const canManageAssignments = auth.hasPermission(MANAGE_SURVEY_ASSIGNMENTS_PERMISSION);
  const canReadAssignments = canManageAssignments || auth.hasPermission(READ_SURVEY_ASSIGNMENTS_PERMISSION);
  const canReadCatalog = auth.hasPermission(READ_ACADEMIC_CATALOG_PERMISSION) || auth.hasPermission('academic.catalog.manage');
  const assignmentPagination = usePagination(assignments, 8);
  const contextualBasePath = academicContext.academicUnitId && academicContext.careerId
    ? `/app/academic/units/${academicContext.academicUnitId}/careers/${academicContext.careerId}`
    : '/app/academic/units';
  const contextualQuery = academicContext.academicCycleId
    ? `?cycle=${encodeURIComponent(academicContext.academicCycleId)}`
    : '';

  useEffect(() => {
    if (isCreatedAssignmentState(location.state)) {
      toast.success('Asignación creada', 'Ya queda disponible para el panel de sesiones.');
      navigate(location.pathname, { replace: true, state: null });
    }
  }, [location.pathname, location.state, navigate, toast]);

  useEffect(() => {
    if (!contextual) {
      return;
    }

    setFilters((current) => ({
      ...current,
      careerId: '',
      subjectId: '',
      academicCycleId: '',
      teacherId: ''
    }));
  }, [academicContext.academicCycleId, academicContext.careerId, contextual]);

  useEffect(() => {
    if (
      !canReadAssignments ||
      !canReadCatalog ||
      !accessToken ||
      (contextual && (!academicContext.careerId || !academicContext.academicCycleId))
    ) {
      setLoadState('ready');
      setAssignments([]);
      return;
    }

    const abortController = new AbortController();
    void loadData(accessToken, filters, abortController.signal);
    return () => abortController.abort();
  }, [
    accessToken,
    academicContext.academicCycleId,
    academicContext.careerId,
    canReadAssignments,
    canReadCatalog,
    contextual,
    filters
  ]);

  const subjectOptions = useMemo(() => {
    const subjectsById = new Map<string, { id: string; name: string; careerId: string }>();

    assignments.forEach((assignment) => {
      subjectsById.set(assignment.subjectId, {
        id: assignment.subjectId,
        name: assignment.subjectName,
        careerId: assignment.careerId
      });
    });

    return [...subjectsById.values()]
      .filter((subject) => !filters.careerId || subject.careerId === filters.careerId)
      .sort((left, right) => left.name.localeCompare(right.name));
  }, [assignments, filters.careerId]);

  if (!canReadAssignments) {
    return <SurveyAssignmentPermissionPanel mode="read" />;
  }

  if (!canReadCatalog) {
    return <SurveyAssignmentPermissionPanel missingCatalog />;
  }

  if (contextual && !academicContext.selectedCareer) {
    return (
      <section className="app-content empty-detail">
        <h3>Seleccioná una carrera.</h3>
        <p>Elegí una carrera en el panel lateral para consultar sus encuestas asignadas.</p>
      </section>
    );
  }

  if (contextual && !academicContext.selectedAcademicCycle) {
    return (
      <section className="app-content empty-detail">
        <h3>Seleccioná un ciclo lectivo.</h3>
        <p>Elegí un ciclo lectivo para evitar mezclar asignaciones de distintos períodos.</p>
      </section>
    );
  }

  async function loadData(token: string, nextFilters = filters, signal?: AbortSignal) {
    setLoadState('loading');
    setPageError(null);

    try {
      const effectiveFilters = buildEffectiveFilters(nextFilters, contextual, academicContext.careerId, academicContext.academicCycleId);
      const [nextAssignments, nextSurveys, nextCareers, nextCycles, nextTeachers] = await Promise.all([
        getSurveyAssignments(token, auth.logout, effectiveFilters, signal),
        getSurveys({ includeInactive: false, status: 'Published', target: '' }, token, auth.logout),
        contextual
          ? Promise.resolve(academicContext.selectedCareer ? [academicContext.selectedCareer] : [])
          : getCareers({ accessToken: token, onUnauthorized: auth.logout, signal }),
        contextual
          ? Promise.resolve(academicContext.selectedAcademicCycle ? [academicContext.selectedAcademicCycle] : [])
          : getAcademicCycles({ accessToken: token, onUnauthorized: auth.logout, signal }),
        contextual
          ? getCareerTeachers(academicContext.careerId, {
              accessToken: token,
              academicCycleId: academicContext.academicCycleId,
              includeInactive: false,
              onUnauthorized: auth.logout,
              signal
            })
          : getTeachers({ accessToken: token, onUnauthorized: auth.logout, signal })
      ]);

      setAssignments(nextAssignments);
      setSurveys(nextSurveys.filter((survey) => survey.status === 'Published' && survey.isActive));
      setCareers(nextCareers);
      setCycles(nextCycles);
      setTeachers(nextTeachers);
      setLoadState('ready');
    } catch (error) {
      if (signal?.aborted) {
        return;
      }

      setPageError(getFriendlyAssignmentError(error, 'No fue posible cargar las asignaciones.'));
      setLoadState('error');
    }
  }

  async function handleToggleAssignment(assignment: SurveyAssignmentDto) {
    if (!accessToken) {
      return;
    }

    setActiveActionId(assignment.id);

    try {
      if (assignment.isActive) {
        await deactivateSurveyAssignment(assignment.id, accessToken, auth.logout);
        toast.success('Asignación desactivada', 'Ya no se utilizará para nuevas sesiones.');
      } else {
        await activateSurveyAssignment(assignment.id, accessToken, auth.logout);
        toast.success('Asignación reactivada');
      }

      await loadData(accessToken);
    } catch (error) {
      const message = getFriendlyAssignmentError(
        error,
        assignment.isActive ? 'No fue posible desactivar la asignación.' : 'No fue posible reactivar la asignación.'
      );
      toast.error('No se pudo actualizar la asignación', message);
    } finally {
      setActiveActionId(null);
    }
  }

  return (
    <section className="app-content assignments-page">
      <header className="surveys-header">
        <div>
          <p className="eyebrow">{contextual ? 'Carrera seleccionada' : 'Contexto académico'}</p>
          <h2>{contextual ? 'Encuestas asignadas' : 'Asignaciones de encuesta'}</h2>
          <p>
            {contextual
              ? canManageAssignments
                ? 'Consultá y gestioná encuestas asignadas para la carrera y ciclo lectivo seleccionados.'
                : 'Consultá las encuestas asignadas para la carrera y ciclo lectivo seleccionados.'
              : canManageAssignments
                ? 'Asociá plantillas publicadas a carreras, materias, ciclos lectivos y docentes.'
                : 'Consultá las encuestas ya asignadas. Las acciones de alta y modificación están reservadas a administración.'}
          </p>
        </div>
        <div className="surveys-actions">
          <button
            className="secondary-button"
            disabled={!accessToken || loadState === 'loading'}
            onClick={() => accessToken && void loadData(accessToken)}
            type="button"
          >
            Actualizar
          </button>
          {canManageAssignments ? (
            <Link
              className="primary-link-button"
              to={contextual ? `${contextualBasePath}/surveys/new${contextualQuery}` : '/app/survey-assignments/new'}
            >
              Nueva asignación
            </Link>
          ) : null}
        </div>
      </header>

      <div className="surveys-filters" aria-label="Filtros de asignaciones">
        <label>
          <span>Encuesta</span>
          <select
            className="text-input"
            onChange={(event) => setFilters((current) => ({ ...current, surveyId: event.target.value }))}
            value={filters.surveyId}
          >
            <option value="">Todas</option>
            {surveys.map((survey) => (
              <option key={survey.id} value={survey.id}>
                {formatSurveyOption(survey)}
              </option>
            ))}
          </select>
        </label>

        {!contextual ? (
        <label>
          <span>Carrera</span>
          <select
            className="text-input"
            onChange={(event) =>
              setFilters((current) => ({
                ...current,
                careerId: event.target.value,
                subjectId: ''
              }))
            }
            value={filters.careerId}
          >
            <option value="">Todas</option>
            {careers.map((career) => (
              <option key={career.id} value={career.id}>
                {career.name}
              </option>
            ))}
          </select>
        </label>
        ) : null}

        <label>
          <span>Materia</span>
          <select
            className="text-input"
            disabled={subjectOptions.length === 0}
            onChange={(event) => setFilters((current) => ({ ...current, subjectId: event.target.value }))}
            value={filters.subjectId}
          >
            <option value="">Todas</option>
            {subjectOptions.map((subject) => (
              <option key={subject.id} value={subject.id}>
                {subject.name}
              </option>
            ))}
          </select>
        </label>

        {!contextual ? (
        <label>
          <span>Ciclo</span>
          <select
            className="text-input"
            onChange={(event) => setFilters((current) => ({ ...current, academicCycleId: event.target.value }))}
            value={filters.academicCycleId}
          >
            <option value="">Todos</option>
            {cycles.map((cycle) => (
              <option key={cycle.id} value={cycle.id}>
                {formatAcademicCycle(cycle)}
              </option>
            ))}
          </select>
        </label>
        ) : null}

        <label>
          <span>Docente</span>
          <select
            className="text-input"
            onChange={(event) => setFilters((current) => ({ ...current, teacherId: event.target.value }))}
            value={filters.teacherId}
          >
            <option value="">Todos</option>
            {teachers.map((teacher) => (
              <option key={teacher.id} value={teacher.id}>
                {formatTeacher(teacher)}
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

      {loadState === 'loading' ? <p aria-live="polite">Cargando asignaciones...</p> : null}

      {loadState === 'error' ? (
        <div className="empty-detail" role="alert">
          <h3>No pudimos cargar las asignaciones</h3>
          <p>{pageError}</p>
        </div>
      ) : null}

      {loadState === 'ready' && assignments.length === 0 ? (
        <div className="empty-detail">
          <h3>No hay asignaciones disponibles</h3>
          <p>
            {contextual
              ? 'No hay encuestas asignadas para esta carrera y ciclo lectivo.'
              : 'Creá una asignación para que el panel de sesiones pueda utilizar una encuesta publicada.'}
          </p>
        </div>
      ) : null}

      {loadState === 'ready' && assignments.length > 0 ? (
        <>
          <PaginationControls
            firstItem={assignmentPagination.firstItem}
            itemLabel="asignaciones"
            lastItem={assignmentPagination.lastItem}
            onPageChange={assignmentPagination.setPage}
            onPageSizeChange={assignmentPagination.setPageSize}
            page={assignmentPagination.page}
            pageSize={assignmentPagination.pageSize}
            totalItems={assignmentPagination.totalItems}
            totalPages={assignmentPagination.totalPages}
          />
          <div className="assignment-card-list" role="list">
            {assignmentPagination.items.map((assignment) => (
              <article className="survey-list-card" key={assignment.id} role="listitem">
                <header>
                  <div>
                    <p className="eyebrow">{assignment.surveyTitle}</p>
                    <h3>{assignment.subjectName}</h3>
                    <p>
                      {assignment.careerName} · {formatAcademicCycleParts(
                        assignment.academicCycleYear,
                        assignment.academicCyclePeriod
                      )}
                    </p>
                  </div>
                  <ActivityBadge isActive={assignment.isActive} />
                </header>

                <dl className="survey-card-meta">
                  <div>
                    <dt>Docente</dt>
                    <dd>{assignment.teacherFullName}</dd>
                  </div>
                  <div>
                    <dt>Rol</dt>
                    <dd>{assignment.teachingRole}</dd>
                  </div>
                  <div>
                    <dt>Encuesta</dt>
                    <dd>{assignment.surveyTitle}</dd>
                  </div>
                  <div>
                    <dt>Estado plantilla</dt>
                    <dd>{formatSurveyStatus(assignment.surveyStatus)}</dd>
                  </div>
                  <div>
                    <dt>Alumnos esperados</dt>
                    <dd>{assignment.expectedRespondentCount ?? 'No aplica'}</dd>
                  </div>
                </dl>

                {canManageAssignments ? (
                  <div className="survey-card-actions">
                    <button
                      className={assignment.isActive ? 'danger-button' : 'secondary-button'}
                      disabled={activeActionId === assignment.id}
                      onClick={() => setToggleTarget(assignment)}
                      type="button"
                    >
                      {activeActionId === assignment.id
                        ? 'Procesando...'
                        : assignment.isActive
                          ? 'Desactivar'
                          : 'Reactivar'}
                    </button>
                  </div>
                ) : null}
              </article>
            ))}
          </div>
        </>
      ) : null}
      {canManageAssignments ? (
        <ConfirmDialog
          busy={Boolean(toggleTarget && activeActionId === toggleTarget.id)}
          confirmLabel={toggleTarget?.isActive ? 'Desactivar' : 'Reactivar'}
          message={toggleTarget?.isActive ? 'La asignación dejará de utilizarse para nuevas sesiones, pero su historial se conservará.' : 'La asignación volverá a estar disponible para nuevas sesiones.'}
          onCancel={() => setToggleTarget(null)}
          onConfirm={() => { if (toggleTarget) void handleToggleAssignment(toggleTarget).finally(() => setToggleTarget(null)); }}
          open={toggleTarget !== null}
          title={toggleTarget?.isActive ? '¿Desactivar esta asignación?' : '¿Reactivar esta asignación?'}
          tone={toggleTarget?.isActive ? 'danger' : 'primary'}
        />
      ) : null}
    </section>
  );
}

function buildEffectiveFilters(
  filters: SurveyAssignmentFilters,
  contextual: boolean,
  careerId: string,
  academicCycleId: string
): SurveyAssignmentFilters {
  if (!contextual) {
    return filters;
  }

  return {
    ...filters,
    careerId,
    academicCycleId
  };
}

function isCreatedAssignmentState(state: unknown): state is { createdAssignmentId: string } {
  return (
    typeof state === 'object' &&
    state !== null &&
    'createdAssignmentId' in state &&
    typeof state.createdAssignmentId === 'string'
  );
}
