import { useEffect, useMemo, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { getAcademicCycles, getCareers, getTeachers } from '../../api/academicCatalogApi';
import {
  activateSurveyAssignment,
  deactivateSurveyAssignment,
  getSurveyAssignments
} from '../../api/surveyAssignmentsApi';
import { getSurveys } from '../../api/surveysApi';
import { useAuth } from '../../auth/AuthProvider';
import type { AcademicCycleDto, CareerDto, TeacherDto } from '../../types/academicCatalog';
import type { SurveyAssignmentDto, SurveyAssignmentFilters } from '../../types/surveyAssignments';
import type { SurveySummaryDto } from '../../types/surveys';
import { ActivityBadge } from '../surveys/surveyUi';
import {
  formatAcademicCycle,
  formatAcademicCycleParts,
  formatSurveyOption,
  formatTeacher,
  getFriendlyAssignmentError,
  MANAGE_SURVEY_ASSIGNMENTS_PERMISSION,
  READ_ACADEMIC_CATALOG_PERMISSION,
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

export function SurveyAssignmentsPage() {
  const auth = useAuth();
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
  const [actionError, setActionError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [activeActionId, setActiveActionId] = useState<string | null>(null);

  const accessToken = auth.accessToken;
  const canManageAssignments = auth.hasPermission(MANAGE_SURVEY_ASSIGNMENTS_PERMISSION);
  const canReadCatalog = auth.hasPermission(READ_ACADEMIC_CATALOG_PERMISSION);

  useEffect(() => {
    if (isCreatedAssignmentState(location.state)) {
      setSuccessMessage('Asignación creada. Ya queda disponible para el panel de sesiones.');
      navigate(location.pathname, { replace: true, state: null });
    }
  }, [location.pathname, location.state, navigate]);

  useEffect(() => {
    if (!canManageAssignments || !canReadCatalog || !accessToken) {
      setLoadState('ready');
      return;
    }

    void loadData(accessToken, filters);
  }, [accessToken, canManageAssignments, canReadCatalog, filters]);

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

  if (!canManageAssignments) {
    return <SurveyAssignmentPermissionPanel />;
  }

  if (!canReadCatalog) {
    return <SurveyAssignmentPermissionPanel missingCatalog />;
  }

  async function loadData(token: string, nextFilters = filters) {
    setLoadState('loading');
    setPageError(null);

    try {
      const [nextAssignments, nextSurveys, nextCareers, nextCycles, nextTeachers] = await Promise.all([
        getSurveyAssignments(token, auth.logout, nextFilters),
        getSurveys({ includeInactive: false, status: 'Published', target: '' }, token, auth.logout),
        getCareers({ accessToken: token, onUnauthorized: auth.logout }),
        getAcademicCycles({ accessToken: token, onUnauthorized: auth.logout }),
        getTeachers({ accessToken: token, onUnauthorized: auth.logout })
      ]);

      setAssignments(nextAssignments);
      setSurveys(nextSurveys.filter((survey) => survey.status === 'Published' && survey.isActive));
      setCareers(nextCareers);
      setCycles(nextCycles);
      setTeachers(nextTeachers);
      setLoadState('ready');
    } catch (error) {
      setPageError(getFriendlyAssignmentError(error, 'No fue posible cargar las asignaciones.'));
      setLoadState('error');
    }
  }

  async function handleToggleAssignment(assignment: SurveyAssignmentDto) {
    if (!accessToken) {
      return;
    }

    if (assignment.isActive) {
      const confirmed = window.confirm(
        'Al desactivar esta asignación ya no debería utilizarse para nuevas sesiones. ¿Deseás continuar?'
      );

      if (!confirmed) {
        return;
      }
    }

    setActiveActionId(assignment.id);
    setActionError(null);
    setSuccessMessage(null);

    try {
      if (assignment.isActive) {
        await deactivateSurveyAssignment(assignment.id, accessToken, auth.logout);
        setSuccessMessage('Asignación desactivada.');
      } else {
        await activateSurveyAssignment(assignment.id, accessToken, auth.logout);
        setSuccessMessage('Asignación reactivada.');
      }

      await loadData(accessToken);
    } catch (error) {
      setActionError(
        getFriendlyAssignmentError(
          error,
          assignment.isActive
            ? 'No fue posible desactivar la asignación.'
            : 'No fue posible reactivar la asignación.'
        )
      );
    } finally {
      setActiveActionId(null);
    }
  }

  return (
    <section className="app-content assignments-page">
      <header className="surveys-header">
        <div>
          <p className="eyebrow">Contexto académico</p>
          <h2>Asignaciones de encuesta</h2>
          <p>Asociá plantillas publicadas a carreras, materias, ciclos lectivos y docentes.</p>
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
          <Link className="primary-link-button" to="/app/survey-assignments/new">
            Nueva asignación
          </Link>
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

      {successMessage ? (
        <div className="success-message" role="status">
          {successMessage}
        </div>
      ) : null}

      {actionError ? (
        <p className="submit-error" role="alert">
          {actionError}
        </p>
      ) : null}

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
          <p>Creá una asignación para que el panel de sesiones pueda utilizar una encuesta publicada.</p>
        </div>
      ) : null}

      {loadState === 'ready' && assignments.length > 0 ? (
        <div className="assignment-card-list" role="list">
          {assignments.map((assignment) => (
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
                  <dd>{assignment.surveyStatus}</dd>
                </div>
              </dl>

              <div className="survey-card-actions">
                <button
                  className={assignment.isActive ? 'danger-button' : 'secondary-button'}
                  disabled={activeActionId === assignment.id}
                  onClick={() => void handleToggleAssignment(assignment)}
                  type="button"
                >
                  {activeActionId === assignment.id
                    ? 'Procesando...'
                    : assignment.isActive
                      ? 'Desactivar'
                      : 'Reactivar'}
                </button>
              </div>
            </article>
          ))}
        </div>
      ) : null}
    </section>
  );
}

function isCreatedAssignmentState(state: unknown): state is { createdAssignmentId: string } {
  return (
    typeof state === 'object' &&
    state !== null &&
    'createdAssignmentId' in state &&
    typeof state.createdAssignmentId === 'string'
  );
}
