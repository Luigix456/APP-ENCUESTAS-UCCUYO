import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import {
  createSubject,
  getCareerTeachers,
  getSubjects,
  getTeacherSubjectAssignments
} from '../../api/academicCatalogApi';
import { getResultAssignments } from '../../api/resultsApi';
import { getSurveyAssignments } from '../../api/surveyAssignmentsApi';
import { getSurveySessions } from '../../api/surveySessionsApi';
import { useAuth } from '../../auth/AuthProvider';
import type {
  CreateSubjectRequest,
  SubjectDto,
  SubjectPeriod,
  TeacherDto,
  TeacherSubjectAssignmentDto
} from '../../types/academicCatalog';
import { SUBJECT_PERIODS } from '../../types/academicCatalog';
import { formatAcademicCycle, formatPeriod, getFriendlyCatalogError } from '../academic-catalog/academicCatalogUi';
import { useAcademicContext } from './AcademicContextProvider';

type LoadState = 'idle' | 'loading' | 'ready' | 'error';

const READ_CATALOG = 'academic.catalog.read';
const MANAGE_CATALOG = 'academic.catalog.manage';

export function CareerOverviewPage() {
  const auth = useAuth();
  const context = useAcademicContext();
  const [metrics, setMetrics] = useState<Array<{ label: string; value: number; to: string }>>([]);
  const [state, setState] = useState<LoadState>('idle');
  const [error, setError] = useState<string | null>(null);
  const accessToken = auth.accessToken;
  const canReadCatalog = auth.hasPermission(READ_CATALOG) || auth.hasPermission(MANAGE_CATALOG);
  const canManageSessions = auth.hasPermission('surveys.sessions.manage');
  const canManageAssignments = auth.hasPermission('surveys.templates.manage');
  const canReadResults = auth.hasPermission('results.read_all') || auth.hasPermission('results.read_career');

  useEffect(() => {
    if (!accessToken || !context.careerId) {
      setMetrics([]);
      setState('ready');
      return;
    }

    const abortController = new AbortController();

    async function loadOverview() {
      if (!accessToken) {
        return;
      }

      setState('loading');
      setError(null);

      try {
        const nextMetrics: Array<{ label: string; value: number; to: string }> = [];
        const baseFilters = {
          careerId: context.careerId,
          academicCycleId: context.academicCycleId
        };

        if (canReadCatalog) {
          const [subjects, teachers] = await Promise.all([
            getSubjects(context.careerId, {
              accessToken,
              includeInactive: false,
              onUnauthorized: auth.logout,
              signal: abortController.signal
            }),
            getCareerTeachers(context.careerId, {
              accessToken,
              academicCycleId: context.academicCycleId,
              includeInactive: false,
              onUnauthorized: auth.logout,
              signal: abortController.signal
            })
          ]);
          nextMetrics.push({ label: 'Materias', value: subjects.length, to: '/app/context/subjects' });
          nextMetrics.push({ label: 'Docentes', value: teachers.length, to: '/app/context/teachers' });
        }

        if (canManageAssignments && context.academicCycleId) {
          const assignments = await getSurveyAssignments(
            accessToken,
            auth.logout,
            { includeInactive: true, ...baseFilters },
            abortController.signal
          );
          nextMetrics.push({ label: 'Encuestas asignadas', value: assignments.length, to: '/app/context/surveys' });
        }

        if (canManageSessions && context.academicCycleId) {
          const sessions = await getSurveySessions(
            accessToken,
            auth.logout,
            { ...baseFilters },
            abortController.signal
          );
          nextMetrics.push({ label: 'Sesiones', value: sessions.length, to: '/app/context/sessions' });
        }

        if (canReadResults && context.academicCycleId) {
          const results = await getResultAssignments(
            accessToken,
            auth.logout,
            baseFilters,
            abortController.signal
          );
          nextMetrics.push({
            label: 'Respuestas',
            value: results.reduce((sum, item) => sum + item.totalResponses, 0),
            to: '/app/context/results'
          });
        }

        setMetrics(nextMetrics);
        setState('ready');
      } catch (loadError) {
        if (abortController.signal.aborted) {
          return;
        }

        setError('No fue posible cargar el resumen de la carrera.');
        setState('error');
      }
    }

    void loadOverview();
    return () => abortController.abort();
  }, [
    accessToken,
    auth.logout,
    canManageAssignments,
    canManageSessions,
    canReadCatalog,
    canReadResults,
    context.academicCycleId,
    context.careerId
  ]);

  const guard = useContextGuard();

  if (guard) {
    return guard;
  }

  return (
    <section className="app-content dashboard-page">
      <ContextHeader eyebrow="Contexto de carrera" title="Resumen" />
      {state === 'loading' ? <p aria-live="polite">Cargando resumen...</p> : null}
      {state === 'error' ? <p className="submit-error">{error}</p> : null}
      {state === 'ready' && metrics.length > 0 ? (
        <div className="dashboard-metrics">
          {metrics.map((metric) => (
            <Link className="dashboard-metric-card" key={metric.label} to={metric.to}>
              <span>{metric.label}</span>
              <strong>{metric.value}</strong>
              <small>{context.selectedAcademicCycle ? formatAcademicCycle(context.selectedAcademicCycle) : 'Todos los ciclos'}</small>
            </Link>
          ))}
        </div>
      ) : null}
      {state === 'ready' && metrics.length === 0 ? (
        <div className="empty-detail">
          <h3>No hay métricas disponibles</h3>
          <p>El resumen sólo muestra información de módulos habilitados para tu perfil.</p>
        </div>
      ) : null}
    </section>
  );
}

export function ContextSubjectsPage() {
  const auth = useAuth();
  const context = useAcademicContext();
  const [subjects, setSubjects] = useState<SubjectDto[]>([]);
  const [state, setState] = useState<LoadState>('idle');
  const [error, setError] = useState<string | null>(null);
  const [yearFilter, setYearFilter] = useState('');
  const [form, setForm] = useState<CreateSubjectRequest>({
    careerId: '',
    code: '',
    name: '',
    year: 1,
    period: 'Annual'
  });
  const [formError, setFormError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const accessToken = auth.accessToken;
  const canReadCatalog = auth.hasPermission(READ_CATALOG) || auth.hasPermission(MANAGE_CATALOG);
  const canManageCatalog = auth.hasPermission(MANAGE_CATALOG);

  useEffect(() => {
    if (!accessToken || !context.careerId || !canReadCatalog) {
      setSubjects([]);
      setState('ready');
      return;
    }

    const abortController = new AbortController();
    void loadSubjects(abortController.signal);
    return () => abortController.abort();

    async function loadSubjects(signal: AbortSignal) {
      if (!accessToken) {
        return;
      }

      setState('loading');
      setError(null);

      try {
        setSubjects(await getSubjects(context.careerId, {
          accessToken,
          includeInactive: false,
          onUnauthorized: auth.logout,
          signal
        }));
        setState('ready');
      } catch (loadError) {
        if (signal.aborted) {
          return;
        }

        setError(getFriendlyCatalogError(loadError, 'No fue posible cargar materias.'));
        setState('error');
      }
    }
  }, [accessToken, auth.logout, canReadCatalog, context.careerId]);

  useEffect(() => {
    setYearFilter('');
    setForm((current) => ({ ...current, careerId: context.careerId }));
  }, [context.careerId]);

  const years = useMemo(
    () => Array.from(new Set(subjects.map((subject) => subject.year))).sort((left, right) => left - right),
    [subjects]
  );
  const filteredSubjects = yearFilter
    ? subjects.filter((subject) => subject.year === Number(yearFilter))
    : subjects;
  const groupedSubjects = groupSubjectsByYear(filteredSubjects);
  const guard = useContextGuard();

  if (guard) {
    return guard;
  }

  if (!canReadCatalog) {
    return <PermissionPanel message="No tenés permisos para consultar materias." />;
  }

  async function handleCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!accessToken || !context.careerId) {
      return;
    }

    setFormError(null);
    setSuccess(null);

    try {
      await createSubject(
        {
          ...form,
          careerId: context.careerId,
          code: form.code.trim(),
          name: form.name.trim()
        },
        { accessToken, onUnauthorized: auth.logout }
      );
      setForm({ careerId: context.careerId, code: '', name: '', year: 1, period: 'Annual' });
      setSubjects(await getSubjects(context.careerId, {
        accessToken,
        includeInactive: false,
        onUnauthorized: auth.logout
      }));
      setSuccess('Materia creada.');
    } catch (createError) {
      setFormError(getFriendlyCatalogError(createError, 'No fue posible crear la materia.'));
    }
  }

  return (
    <section className="app-content academic-page">
      <ContextHeader eyebrow="Carrera seleccionada" title="Materias" />
      <div className="surveys-filters">
        <label>
          <span>Año de carrera</span>
          <select className="text-input" onChange={(event) => setYearFilter(event.target.value)} value={yearFilter}>
            <option value="">Todos</option>
            {years.map((year) => (
              <option key={year} value={year}>
                {year}° año
              </option>
            ))}
          </select>
        </label>
      </div>

      {canManageCatalog ? (
        <form className="survey-admin-form assignment-form" onSubmit={handleCreate}>
          <h3>Nueva materia para {context.selectedCareer?.name}</h3>
          <label>
            <span>Código</span>
            <input className="text-input" required value={form.code} onChange={(event) => setForm((current) => ({ ...current, code: event.target.value }))} />
          </label>
          <label>
            <span>Nombre</span>
            <input className="text-input" required value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} />
          </label>
          <label>
            <span>Año de carrera</span>
            <input className="text-input" min={1} max={10} required type="number" value={form.year} onChange={(event) => setForm((current) => ({ ...current, year: Number(event.target.value) }))} />
          </label>
          <label>
            <span>Período</span>
            <select className="text-input" value={form.period} onChange={(event) => setForm((current) => ({ ...current, period: event.target.value as SubjectPeriod }))}>
              {SUBJECT_PERIODS.map((period) => (
                <option key={period} value={period}>{formatPeriod(period)}</option>
              ))}
            </select>
          </label>
          {formError ? <p className="submit-error assignment-form__wide">{formError}</p> : null}
          {success ? <div className="success-message assignment-form__wide">{success}</div> : null}
          <button className="primary-button assignment-form__wide" type="submit">Crear materia</button>
        </form>
      ) : null}

      {state === 'loading' ? <p>Cargando materias...</p> : null}
      {state === 'error' ? <p className="submit-error">{error}</p> : null}
      {state === 'ready' && filteredSubjects.length === 0 ? (
        <div className="empty-detail">
          <h3>No hay materias para esta carrera</h3>
          <p>No hay materias activas para los filtros seleccionados.</p>
        </div>
      ) : null}
      <div className="assignment-card-list">
        {groupedSubjects.map(([year, yearSubjects]) => (
          <section className="structure-card" key={year}>
            <header>
              <div>
                <p className="eyebrow">{year}° año</p>
                <h3>{yearSubjects.length} materias</h3>
              </div>
            </header>
            {yearSubjects.map((subject) => (
              <div className="readonly-line" key={subject.id}>
                <span>{subject.name}</span>
                <small>{subject.code} · {formatPeriod(subject.period)}</small>
              </div>
            ))}
          </section>
        ))}
      </div>
    </section>
  );
}

export function ContextTeachersPage() {
  const auth = useAuth();
  const context = useAcademicContext();
  const [teachers, setTeachers] = useState<TeacherDto[]>([]);
  const [assignments, setAssignments] = useState<TeacherSubjectAssignmentDto[]>([]);
  const [state, setState] = useState<LoadState>('idle');
  const [error, setError] = useState<string | null>(null);
  const accessToken = auth.accessToken;
  const canReadCatalog = auth.hasPermission(READ_CATALOG) || auth.hasPermission(MANAGE_CATALOG);

  useEffect(() => {
    if (!accessToken || !context.careerId || !canReadCatalog) {
      setTeachers([]);
      setAssignments([]);
      setState('ready');
      return;
    }

    const abortController = new AbortController();

    setState('loading');
    setError(null);

    Promise.all([
      getCareerTeachers(context.careerId, {
        accessToken,
        academicCycleId: context.academicCycleId,
        includeInactive: false,
        onUnauthorized: auth.logout,
        signal: abortController.signal
      }),
      getTeacherSubjectAssignments(
        {
          includeInactive: false,
          careerId: context.careerId,
          academicCycleId: context.academicCycleId
        },
        { accessToken, onUnauthorized: auth.logout, signal: abortController.signal }
      )
    ])
      .then(([nextTeachers, nextAssignments]) => {
        setTeachers(nextTeachers);
        setAssignments(nextAssignments);
        setState('ready');
      })
      .catch((loadError) => {
        if (abortController.signal.aborted) {
          return;
        }

        setError(getFriendlyCatalogError(loadError, 'No fue posible cargar docentes.'));
        setState('error');
      });

    return () => abortController.abort();
  }, [accessToken, auth.logout, canReadCatalog, context.academicCycleId, context.careerId]);

  const assignmentsByTeacher = useMemo(() => {
    const map = new Map<string, TeacherSubjectAssignmentDto[]>();
    assignments.forEach((assignment) => {
      map.set(assignment.teacherId, [...(map.get(assignment.teacherId) ?? []), assignment]);
    });
    return map;
  }, [assignments]);
  const guard = useContextGuard();

  if (guard) {
    return guard;
  }

  if (!canReadCatalog) {
    return <PermissionPanel message="No tenés permisos para consultar docentes." />;
  }

  return (
    <section className="app-content academic-page">
      <ContextHeader eyebrow="Carrera seleccionada" title="Docentes" />
      {state === 'loading' ? <p>Cargando docentes...</p> : null}
      {state === 'error' ? <p className="submit-error">{error}</p> : null}
      {state === 'ready' && teachers.length === 0 ? (
        <div className="empty-detail">
          <h3>No hay docentes asignados</h3>
          <p>
            {context.selectedAcademicCycle
              ? 'No hay docentes asignados a esta carrera en el ciclo seleccionado.'
              : 'No hay docentes asignados a esta carrera.'}
          </p>
        </div>
      ) : null}
      <div className="assignment-card-list">
        {teachers.map((teacher) => {
          const teacherAssignments = assignmentsByTeacher.get(teacher.id) ?? [];
          return (
            <article className="survey-list-card" key={teacher.id}>
              <header>
                <div>
                  <p className="eyebrow">Docente</p>
                  <h3>{teacher.lastName}, {teacher.firstName}</h3>
                  <p>{teacher.email ?? 'Sin email registrado'}</p>
                </div>
              </header>
              {teacherAssignments.length > 0 ? (
                <dl className="survey-card-meta">
                  {teacherAssignments.map((assignment) => (
                    <div key={assignment.id}>
                      <dt>{assignment.subjectName}</dt>
                      <dd>{assignment.teachingRole}</dd>
                    </div>
                  ))}
                </dl>
              ) : null}
            </article>
          );
        })}
      </div>
    </section>
  );
}

export function ContextTeacherAssignmentsPage() {
  const auth = useAuth();
  const context = useAcademicContext();
  const [assignments, setAssignments] = useState<TeacherSubjectAssignmentDto[]>([]);
  const [state, setState] = useState<LoadState>('idle');
  const [error, setError] = useState<string | null>(null);
  const accessToken = auth.accessToken;
  const canReadCatalog = auth.hasPermission(READ_CATALOG) || auth.hasPermission(MANAGE_CATALOG);

  useEffect(() => {
    if (!accessToken || !context.careerId || !canReadCatalog) {
      setAssignments([]);
      setState('ready');
      return;
    }

    const abortController = new AbortController();

    setState('loading');
    setError(null);

    getTeacherSubjectAssignments(
      {
        includeInactive: false,
        careerId: context.careerId,
        academicCycleId: context.academicCycleId
      },
      { accessToken, onUnauthorized: auth.logout, signal: abortController.signal }
    )
      .then((nextAssignments) => {
        setAssignments(nextAssignments);
        setState('ready');
      })
      .catch((loadError) => {
        if (abortController.signal.aborted) {
          return;
        }

        setError(getFriendlyCatalogError(loadError, 'No fue posible cargar asignaciones docentes.'));
        setState('error');
      });

    return () => abortController.abort();
  }, [accessToken, auth.logout, canReadCatalog, context.academicCycleId, context.careerId]);

  const guard = useContextGuard(true);

  if (guard) {
    return guard;
  }

  if (!canReadCatalog) {
    return <PermissionPanel message="No tenés permisos para consultar asignaciones docentes." />;
  }

  return (
    <section className="app-content academic-page">
      <ContextHeader eyebrow="Carrera seleccionada" title="Asignaciones docentes" />
      {state === 'loading' ? <p>Cargando asignaciones docentes...</p> : null}
      {state === 'error' ? <p className="submit-error">{error}</p> : null}
      {state === 'ready' && assignments.length === 0 ? (
        <div className="empty-detail">
          <h3>No hay asignaciones docentes</h3>
          <p>No hay asignaciones docentes para esta carrera y ciclo lectivo.</p>
        </div>
      ) : null}
      <div className="assignment-card-list">
        {assignments.map((assignment) => (
          <article className="survey-list-card" key={assignment.id}>
            <header>
              <div>
                <p className="eyebrow">{assignment.subjectName}</p>
                <h3>{assignment.teacherFullName}</h3>
                <p>{assignment.teachingRole}</p>
              </div>
              <span className={`status-badge ${assignment.isActive ? 'status-badge--open' : 'status-badge--closed'}`}>
                {assignment.isActive ? 'Activa' : 'Inactiva'}
              </span>
            </header>
            <dl className="survey-card-meta">
              <div>
                <dt>Carrera</dt>
                <dd>{assignment.careerName}</dd>
              </div>
              <div>
                <dt>Ciclo</dt>
                <dd>{assignment.academicCycleYear} · {formatPeriod(assignment.academicCyclePeriod)}</dd>
              </div>
            </dl>
          </article>
        ))}
      </div>
    </section>
  );
}

function ContextHeader({ eyebrow, title }: { eyebrow: string; title: string }) {
  const context = useAcademicContext();

  return (
    <header className="surveys-header">
      <div>
        <p className="eyebrow">{eyebrow}</p>
        <h2>{title}</h2>
        <p>
          {context.selectedAcademicUnit?.name} · {context.selectedCareer?.name}
          {context.selectedAcademicCycle ? ` · ${formatAcademicCycle(context.selectedAcademicCycle)}` : ''}
        </p>
      </div>
    </header>
  );
}

function PermissionPanel({ message }: { message: string }) {
  return (
    <section className="app-content access-denied-panel">
      <p className="eyebrow">Sin acceso</p>
      <h2>{message}</h2>
      <p>El contexto académico no reemplaza los permisos asignados a tu usuario.</p>
    </section>
  );
}

function useContextGuard(requireCycle = false) {
  const context = useAcademicContext();

  if (!context.selectedAcademicUnit) {
    return (
      <section className="app-content empty-detail">
        <h3>Seleccioná una unidad académica.</h3>
        <p>Elegí una unidad académica en el panel lateral para comenzar.</p>
      </section>
    );
  }

  if (!context.selectedCareer) {
    return (
      <section className="app-content empty-detail">
        <h3>Seleccioná una carrera.</h3>
        <p>Elegí una carrera para ver sus materias, docentes, sesiones y resultados.</p>
      </section>
    );
  }

  if (requireCycle && !context.selectedAcademicCycle) {
    return (
      <section className="app-content empty-detail">
        <h3>Seleccioná un ciclo lectivo.</h3>
        <p>Elegí un ciclo lectivo para trabajar con información académica del período correcto.</p>
      </section>
    );
  }

  return null;
}

function groupSubjectsByYear(subjects: SubjectDto[]): Array<[number, SubjectDto[]]> {
  const grouped = new Map<number, SubjectDto[]>();

  subjects.forEach((subject) => {
    grouped.set(subject.year, [...(grouped.get(subject.year) ?? []), subject]);
  });

  return [...grouped.entries()]
    .sort(([left], [right]) => left - right)
    .map(([year, yearSubjects]) => [
      year,
      [...yearSubjects].sort((left, right) => left.name.localeCompare(right.name))
    ]);
}
