import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
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
  const [metrics, setMetrics] = useState<DashboardMetric[]>([]);
  const [academicUnits, setAcademicUnits] = useState<AcademicUnitDto[]>([]);
  const [allCareers, setAllCareers] = useState<CareerDto[]>([]);
  const [state, setState] = useState<DashboardState>('loading');
  const [error, setError] = useState<string | null>(null);
  const [expandedUnitId, setExpandedUnitId] = useState<string | null>(null);
  const accessToken = auth.accessToken;
  const hasContext = Boolean(academicContext.selectedCareer && academicContext.selectedAcademicCycle);

  const permissions = useMemo(
    () => ({
      surveysRead: auth.hasPermission('surveys.templates.read') || auth.hasPermission('surveys.templates.manage'),
      surveysManage: auth.hasPermission('surveys.templates.manage'),
      surveyAssignmentsRead: auth.hasPermission('surveys.templates.read') || auth.hasPermission('surveys.templates.manage'),
      sessions: auth.hasPermission('surveys.sessions.manage'),
      results: auth.hasPermission('results.read_all') || auth.hasPermission('results.read_career'),
      catalogRead: auth.hasPermission('academic.catalog.read') || auth.hasPermission('academic.catalog.manage'),
      catalogManage: auth.hasPermission('academic.catalog.manage'),
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
        if (permissions.surveysRead) {
          const surveys = await getSurveys({ includeInactive: true, status: '', target: '' }, accessToken, auth.logout);
          nextMetrics.push({
            key: 'surveys',
            label: 'Plantillas',
            value: surveys.length,
            helper: `${surveys.filter((survey) => survey.status === 'Published').length} publicadas`,
            to: '/app/surveys'
          });
        }

        if (permissions.surveyAssignmentsRead && hasContext) {
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

        if (permissions.sessions) {
          const sessions = await getSurveySessions(
            accessToken,
            auth.logout,
            hasContext
              ? {
                  careerId: academicContext.careerId,
                  academicCycleId: academicContext.academicCycleId
                }
              : {},
            abortController.signal
          );
          nextMetrics.push({
            key: 'sessions',
            label: 'Sesiones',
            value: sessions.length,
            helper: `${sessions.filter((session) => session.status === 'Open').length} abiertas`,
            to: hasContext ? '/app/context/sessions' : '/app/sessions'
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

        if (permissions.catalogRead) {
          const [units, careers, teachers] = await Promise.all([
            getAcademicUnits({ accessToken, onUnauthorized: auth.logout, includeInactive: false, signal: abortController.signal }),
            getCareers({ accessToken, onUnauthorized: auth.logout, includeInactive: false, signal: abortController.signal }),
            getTeachers({ accessToken, onUnauthorized: auth.logout, signal: abortController.signal })
          ]);
          setAcademicUnits(units);
          setAllCareers(careers);
          if (permissions.catalogManage) {
            nextMetrics.push({
              key: 'catalog',
              label: 'Estructura académica',
              value: careers.length,
              helper: `${units.length} unidades · ${teachers.length} docentes`,
              to: '/app/academic'
            });
          }
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
    permissions.surveysRead
      ? { to: '/app/surveys', label: permissions.surveysManage ? 'Administrar plantillas' : 'Consultar plantillas' }
      : null,
    permissions.surveysManage && hasContext ? { to: '/app/context/surveys/new', label: 'Nueva asignación' } : null,
    permissions.sessions
      ? { to: hasContext ? '/app/context/sessions' : '/app/sessions', label: 'Gestionar sesiones' }
      : null,
    permissions.results && hasContext ? { to: '/app/context/results', label: 'Consultar resultados' } : null,
    permissions.users ? { to: '/app/users', label: 'Administrar usuarios' } : null
  ].filter((link): link is { to: string; label: string } => link !== null);

  const gettingStartedSteps = useMemo(() => {
    const steps: string[] = [];

    if (permissions.sessions) {
      steps.push('Abrí Sesiones desde Operación para ver o crear sesiones sin necesidad de configurar primero un contexto.');
    }

    if (permissions.catalogRead) {
      steps.push('Para trabajar sobre una carrera concreta, elegí unidad académica, carrera y ciclo lectivo.');
    }

    if (permissions.results) {
      steps.push('Ingresá a Resultados para consultar las evaluaciones disponibles para tu perfil.');
    } else if (permissions.surveysRead) {
      steps.push('Podés consultar las encuestas asignadas y las plantillas habilitadas para tu perfil.');
    }

    return steps;
  }, [permissions]);

  const dashboardIntro = 'Seleccioná una unidad académica para ver sus carreras y comenzar a trabajar.';

  function handleCareerClick(unitId: string, careerId: string) {
    academicContext.selectCareerContext(unitId, careerId);
  }

  function toggleUnit(unitId: string) {
    setExpandedUnitId((current) => (current === unitId ? null : unitId));
  }


  return (
    <section className="app-content dashboard-page">
      <header className="dashboard-hero">
        <div>
          <p className="eyebrow">Inicio</p>
          <h2>{auth.user?.firstName ? `Hola, ${auth.user.firstName}` : 'Sistema de Encuestas Académicas'}</h2>
          <p>{dashboardIntro}</p>
        </div>
        <div className="dashboard-hero__institution">
          <span>UCCuyo</span>
          <small>Gestión académica</small>
        </div>
      </header>

      {permissions.catalogRead ? (
        <section className="academic-directory" aria-label="Estructura académica">
          <div className="section-heading-row">
            <div>
              <p className="eyebrow">Estructura académica</p>
              <p>Seleccioná una unidad académica para ver sus carreras y comenzar a trabajar.</p>
            </div>
          </div>

          {state === 'loading' && academicUnits.length === 0 ? <p aria-live="polite">Cargando estructura académica...</p> : null}

          <div className="home-academic-structure">
            {academicUnits.map((unit) => {
              const careers = careersByUnit.get(unit.id) ?? [];
              const isExpanded = expandedUnitId === unit.id;
              const contentId = 'academic-unit-careers-' + unit.id;

              return (
                <article className={['academic-unit-accordion', isExpanded ? 'academic-unit-accordion--open' : ''].filter(Boolean).join(' ')} key={unit.id}>
                  <button
                    aria-controls={contentId}
                    aria-expanded={isExpanded}
                    className="academic-unit-accordion__trigger"
                    onClick={() => toggleUnit(unit.id)}
                    type="button"
                  >
                    <span className="academic-unit-accordion__identity">
                      <span className="academic-unit-mark" aria-hidden="true">UA</span>
                      <span>
                        <span className="academic-unit-accordion__name">{unit.name}</span>
                        <span className="academic-unit-accordion__count">{formatCareerCount(careers.length)}</span>
                      </span>
                    </span>
                    <span className="academic-unit-accordion__chevron" aria-hidden="true">{isExpanded ? '▲' : '▼'}</span>
                  </button>

                  {isExpanded ? (
                    <div className="academic-unit-accordion__content" id={contentId}>
                      {careers.length > 0 ? (
                        <div className="academic-unit-accordion__careers">
                          {careers.map((career) => (
                            <Link
                              className={['career-quick-link', academicContext.careerId === career.id ? 'career-quick-link--selected' : ''].filter(Boolean).join(' ')}
                              key={career.id}
                              onClick={() => handleCareerClick(unit.id, career.id)}
                              to={'/app/academic/units/' + unit.id + '/careers/' + career.id}
                            >
                              <span>
                                <span className="career-quick-link__name">{career.name}</span>
                                <span className="career-quick-link__type">{formatCareerType(career.type)}</span>
                              </span>
                              <span className="career-quick-link__arrow" aria-hidden="true">→</span>
                            </Link>
                          ))}
                        </div>
                      ) : (
                        <p className="empty-inline">Esta unidad académica todavía no tiene carreras activas.</p>
                      )}
                    </div>
                  ) : null}
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
            {gettingStartedSteps.map((step) => <li key={step}>{step}</li>)}
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

function formatCareerCount(count: number): string {
  return count === 1 ? '1 carrera' : count + ' carreras';
}
