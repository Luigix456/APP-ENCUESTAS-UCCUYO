import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { getAcademicUnits, getCareers, getTeachers } from '../../api/academicCatalogApi';
import { getResultAssignments } from '../../api/resultsApi';
import { getSurveyAssignments } from '../../api/surveyAssignmentsApi';
import { getSurveySessions } from '../../api/surveySessionsApi';
import { getSurveys } from '../../api/surveysApi';
import { getUsers } from '../../api/usersApi';
import { useAuth } from '../../auth/AuthProvider';
import type { AcademicUnitDto, CareerDto } from '../../types/academicCatalog';
import { useAcademicContext } from '../academic-context/AcademicContextProvider';
import { formatAcademicCycle, formatCareerType } from '../academic-catalog/academicCatalogUi';

type DashboardState = 'loading' | 'ready' | 'error';

interface DashboardMetric {
  key: string;
  label: string;
  value: number;
  helper: string;
  to: string;
}

export function DashboardPage() {
  const auth = useAuth();
  const academicContext = useAcademicContext();
  const navigate = useNavigate();
  const [metrics, setMetrics] = useState<DashboardMetric[]>([]);
  const [academicUnits, setAcademicUnits] = useState<AcademicUnitDto[]>([]);
  const [allCareers, setAllCareers] = useState<CareerDto[]>([]);
  const [state, setState] = useState<DashboardState>('loading');
  const [error, setError] = useState<string | null>(null);
  const accessToken = auth.accessToken;
  const hasContext = Boolean(academicContext.selectedCareer && academicContext.selectedAcademicCycle);

  const permissions = useMemo(
    () => ({
      surveys: auth.hasPermission('surveys.templates.read') || auth.hasPermission('surveys.templates.manage'),
      surveyAssignments: auth.hasPermission('surveys.templates.manage'),
      sessions: auth.hasPermission('surveys.sessions.manage'),
      results: auth.hasPermission('results.read_all') || auth.hasPermission('results.read_career'),
      catalog: auth.hasPermission('academic.catalog.read') || auth.hasPermission('academic.catalog.manage'),
      users: auth.hasPermission('identity.users.read')
    }),
    [auth]
  );

  useEffect(() => {
    if (!accessToken) {
      setState('ready');
      return;
    }

    const abortController = new AbortController();
    setState('loading');
    setError(null);

    async function loadDashboard() {
      if (!accessToken) return;
      const nextMetrics: DashboardMetric[] = [];

      try {
        if (permissions.surveys) {
          const surveys = await getSurveys({ includeInactive: true, status: '', target: '' }, accessToken, auth.logout);
          nextMetrics.push({
            key: 'surveys',
            label: 'Plantillas',
            value: surveys.length,
            helper: `${surveys.filter((survey) => survey.status === 'Published').length} publicadas`,
            to: '/app/surveys'
          });
        }

        if (permissions.surveyAssignments && hasContext) {
          const assignments = await getSurveyAssignments(accessToken, auth.logout, {
            includeInactive: true,
            careerId: academicContext.careerId,
            academicCycleId: academicContext.academicCycleId
          });
          nextMetrics.push({
            key: 'assignments',
            label: 'Encuestas asignadas',
            value: assignments.length,
            helper: `${assignments.filter((assignment) => assignment.isActive).length} activas`,
            to: '/app/context/surveys'
          });
        }

        if (permissions.sessions && hasContext) {
          const sessions = await getSurveySessions(accessToken, auth.logout, {
            careerId: academicContext.careerId,
            academicCycleId: academicContext.academicCycleId
          });
          nextMetrics.push({
            key: 'sessions',
            label: 'Sesiones',
            value: sessions.length,
            helper: `${sessions.filter((session) => session.status === 'Open').length} abiertas`,
            to: '/app/context/sessions'
          });
        }

        if (permissions.results && hasContext) {
          const resultAssignments = await getResultAssignments(
            accessToken,
            auth.logout,
            { careerId: academicContext.careerId, academicCycleId: academicContext.academicCycleId },
            abortController.signal
          );
          const totalResponses = resultAssignments.reduce((sum, assignment) => sum + assignment.totalResponses, 0);
          nextMetrics.push({
            key: 'responses',
            label: 'Respuestas',
            value: totalResponses,
            helper: `${resultAssignments.length} evaluaciones con resultados`,
            to: '/app/context/results'
          });
        }

        if (permissions.catalog) {
          const [units, careers, teachers] = await Promise.all([
            getAcademicUnits({ accessToken, onUnauthorized: auth.logout, includeInactive: false, signal: abortController.signal }),
            getCareers({ accessToken, onUnauthorized: auth.logout, includeInactive: false, signal: abortController.signal }),
            getTeachers({ accessToken, onUnauthorized: auth.logout, signal: abortController.signal })
          ]);
          setAcademicUnits(units);
          setAllCareers(careers);
          nextMetrics.push({
            key: 'catalog',
            label: 'Estructura académica',
            value: careers.length,
            helper: `${units.length} unidades · ${teachers.length} docentes`,
            to: '/app/academic'
          });
        } else {
          setAcademicUnits([]);
          setAllCareers([]);
        }

        if (permissions.users) {
          const users = await getUsers({
            accessToken,
            onUnauthorized: auth.logout,
            includeInactive: true,
            signal: abortController.signal
          });
          nextMetrics.push({
            key: 'users',
            label: 'Usuarios',
            value: users.length,
            helper: `${users.filter((user) => user.status === 'Active').length} activos`,
            to: '/app/users'
          });
        }

        setMetrics(nextMetrics);
        setState('ready');
      } catch {
        if (abortController.signal.aborted) return;
        setError('No fue posible cargar toda la información del inicio. Podés seguir usando el menú lateral.');
        setState('error');
      }
    }

    void loadDashboard();
    return () => abortController.abort();
  }, [
    accessToken,
    academicContext.academicCycleId,
    academicContext.careerId,
    auth.logout,
    hasContext,
    permissions
  ]);

  const careersByUnit = useMemo(() => {
    const grouped = new Map<string, CareerDto[]>();
    for (const unit of academicUnits) grouped.set(unit.id, []);
    for (const career of allCareers) grouped.get(career.academicUnitId)?.push(career);
    for (const careers of grouped.values()) careers.sort((a, b) => a.name.localeCompare(b.name));
    return grouped;
  }, [academicUnits, allCareers]);

  const quickLinks = [
    permissions.surveys ? { to: '/app/surveys', label: 'Plantillas de encuestas' } : null,
    permissions.surveyAssignments && hasContext ? { to: '/app/context/surveys/new', label: 'Nueva asignación' } : null,
    permissions.sessions && hasContext ? { to: '/app/context/sessions', label: 'Gestionar sesiones' } : null,
    permissions.results && hasContext ? { to: '/app/context/results', label: 'Consultar resultados' } : null,
    permissions.users ? { to: '/app/users', label: 'Administrar usuarios' } : null
  ].filter((link): link is { to: string; label: string } => link !== null);

  function chooseCareer(unitId: string, careerId: string) {
    academicContext.selectCareerContext(unitId, careerId);
    navigate('/app/context');
  }

  return (
    <section className="app-content dashboard-page">
      <header className="dashboard-hero">
        <div>
          <p className="eyebrow">Inicio</p>
          <h2>Sistema Web de Gestión de Encuestas Académicas</h2>
          <p>Elegí una unidad académica y una carrera para comenzar. El sistema organizará las opciones según ese contexto.</p>
        </div>
      </header>

      {permissions.catalog ? (
        <section className="academic-directory" aria-labelledby="academic-directory-title">
          <div className="section-heading-row">
            <div>
              <p className="eyebrow">Estructura académica</p>
              <h3 id="academic-directory-title">Unidades académicas y carreras</h3>
              <p>Seleccioná la carrera sobre la que querés trabajar.</p>
            </div>
          </div>

          {state === 'loading' && academicUnits.length === 0 ? <p aria-live="polite">Cargando estructura académica...</p> : null}

          <div className="academic-unit-grid">
            {academicUnits.map((unit) => {
              const careers = careersByUnit.get(unit.id) ?? [];
              return (
                <article className="academic-unit-card" key={unit.id}>
                  <header>
                    <div className="academic-unit-mark" aria-hidden="true">UA</div>
                    <div>
                      <span>{unit.code}</span>
                      <h4>{unit.name}</h4>
                      <small>{careers.length} {careers.length === 1 ? 'carrera' : 'carreras'}</small>
                    </div>
                  </header>
                  {careers.length > 0 ? (
                    <div className="career-choice-list">
                      {careers.map((career) => (
                        <button
                          className={`career-choice ${academicContext.careerId === career.id ? 'career-choice--selected' : ''}`}
                          key={career.id}
                          onClick={() => chooseCareer(unit.id, career.id)}
                          type="button"
                        >
                          <span>
                            <strong>{career.name}</strong>
                            <small>{career.code} · {formatCareerType(career.type)}</small>
                          </span>
                          <span className="career-choice__arrow" aria-hidden="true">→</span>
                        </button>
                      ))}
                    </div>
                  ) : (
                    <p className="empty-inline">Esta unidad académica todavía no tiene carreras activas.</p>
                  )}
                </article>
              );
            })}
          </div>
        </section>
      ) : null}

      <section className="getting-started-panel" aria-label="Guía rápida">
        <div>
          <p className="eyebrow">Guía rápida</p>
          <h3>Cómo empezar</h3>
          <ol>
            <li>Elegí la carrera desde las tarjetas de arriba o desde el menú lateral.</li>
            <li>Seleccioná el ciclo lectivo en el menú lateral.</li>
            <li>Revisá materias y docentes antes de asignar una encuesta.</li>
            <li>Creá una sesión QR y, al finalizar, consultá sus resultados.</li>
          </ol>
        </div>
        <div className="current-context-card">
          <span>Contexto actual</span>
          <strong>{academicContext.selectedCareer?.name ?? 'Sin carrera seleccionada'}</strong>
          <small>
            {academicContext.selectedAcademicUnit?.name ?? 'Elegí una unidad académica'}
            {academicContext.selectedAcademicCycle ? ` · ${formatAcademicCycle(academicContext.selectedAcademicCycle)}` : ''}
          </small>
        </div>
      </section>

      {state === 'loading' ? <p aria-live="polite">Cargando información...</p> : null}
      {state === 'error' ? <p className="submit-error" role="alert">{error}</p> : null}

      {state === 'ready' && metrics.length > 0 ? (
        <section aria-labelledby="dashboard-summary-title">
          <div className="section-heading-row">
            <div>
              <p className="eyebrow">Resumen</p>
              <h3 id="dashboard-summary-title">Información disponible</h3>
            </div>
          </div>
          <div className="dashboard-metrics" role="list">
            {metrics.map((metric) => (
              <Link className="dashboard-metric-card" key={metric.key} role="listitem" to={metric.to}>
                <span>{metric.label}</span>
                <strong>{metric.value}</strong>
                <small>{metric.helper}</small>
              </Link>
            ))}
          </div>
        </section>
      ) : null}

      {state === 'ready' && metrics.length === 0 ? (
        <div className="empty-detail">
          <h3>No hay módulos disponibles para tu perfil</h3>
          <p>Solicitá permisos de acceso a la administración del sistema.</p>
        </div>
      ) : null}

      {quickLinks.length > 0 ? (
        <section aria-labelledby="quick-links-title">
          <div className="section-heading-row"><h3 id="quick-links-title">Accesos rápidos</h3></div>
          <nav className="quick-link-grid" aria-label="Accesos rápidos">
            {quickLinks.map((link) => (
              <Link className="catalog-home-card" key={link.to} to={link.to}><h3>{link.label}</h3></Link>
            ))}
          </nav>
        </section>
      ) : null}
    </section>
  );
}
