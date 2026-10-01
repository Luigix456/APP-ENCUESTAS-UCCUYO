import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { getCareers, getTeachers } from '../../api/academicCatalogApi';
import { getResultAssignments } from '../../api/resultsApi';
import { getSurveyAssignments } from '../../api/surveyAssignmentsApi';
import { getSurveySessions } from '../../api/surveySessionsApi';
import { getSurveys } from '../../api/surveysApi';
import { getUsers } from '../../api/usersApi';
import { useAuth } from '../../auth/AuthProvider';

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
  const [metrics, setMetrics] = useState<DashboardMetric[]>([]);
  const [state, setState] = useState<DashboardState>('loading');
  const [error, setError] = useState<string | null>(null);
  const accessToken = auth.accessToken;

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

    async function loadMetrics() {
      if (!accessToken) {
        return;
      }

      const nextMetrics: DashboardMetric[] = [];

      try {
        if (permissions.surveys) {
          const surveys = await getSurveys({ includeInactive: true, status: '', target: '' }, accessToken, auth.logout);
          nextMetrics.push({
            key: 'surveys',
            label: 'Encuestas',
            value: surveys.length,
            helper: `${surveys.filter((survey) => survey.status === 'Published').length} publicadas`,
            to: '/app/surveys'
          });
        }

        if (permissions.surveyAssignments) {
          const assignments = await getSurveyAssignments(accessToken, auth.logout, { includeInactive: true });
          nextMetrics.push({
            key: 'assignments',
            label: 'Asignaciones',
            value: assignments.length,
            helper: `${assignments.filter((assignment) => assignment.isActive).length} activas`,
            to: '/app/survey-assignments'
          });
        }

        if (permissions.sessions) {
          const sessions = await getSurveySessions(accessToken, auth.logout);
          nextMetrics.push({
            key: 'sessions',
            label: 'Sesiones',
            value: sessions.length,
            helper: `${sessions.filter((session) => session.status === 'Open').length} abiertas`,
            to: '/app/sessions'
          });
        }

        if (permissions.results) {
          const resultAssignments = await getResultAssignments(accessToken, auth.logout, {}, abortController.signal);
          const totalResponses = resultAssignments.reduce(
            (sum, assignment) => sum + assignment.totalResponses,
            0
          );
          nextMetrics.push({
            key: 'responses',
            label: 'Respuestas',
            value: totalResponses,
            helper: `${resultAssignments.length} contextos con resultados`,
            to: '/app/results'
          });
        }

        if (permissions.catalog) {
          const [careers, teachers] = await Promise.all([
            getCareers({ accessToken, onUnauthorized: auth.logout, signal: abortController.signal }),
            getTeachers({ accessToken, onUnauthorized: auth.logout, signal: abortController.signal })
          ]);
          nextMetrics.push({
            key: 'catalog',
            label: 'Catálogo',
            value: careers.length + teachers.length,
            helper: `${careers.length} carreras · ${teachers.length} docentes`,
            to: '/app/academic'
          });
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
        if (abortController.signal.aborted) {
          return;
        }

        setError('No fue posible cargar todas las métricas del inicio.');
        setState('error');
      }
    }

    void loadMetrics();

    return () => {
      abortController.abort();
    };
  }, [accessToken, auth.logout, permissions]);

  const quickLinks = [
    permissions.surveys ? { to: '/app/surveys', label: 'Gestionar encuestas' } : null,
    permissions.surveyAssignments ? { to: '/app/survey-assignments/new', label: 'Crear asignación' } : null,
    permissions.sessions ? { to: '/app/sessions', label: 'Abrir sesión QR' } : null,
    permissions.results ? { to: '/app/results', label: 'Consultar resultados' } : null,
    permissions.users ? { to: '/app/users', label: 'Administrar usuarios' } : null
  ].filter((link): link is { to: string; label: string } => link !== null);

  return (
    <section className="app-content dashboard-page">
      <header className="dashboard-hero">
        <div>
          <p className="eyebrow">Inicio</p>
          <h2>Sistema Web de Gestión de Encuestas Académicas</h2>
          <p>Panel operativo con accesos y métricas disponibles para tu perfil.</p>
        </div>
      </header>

      {state === 'loading' ? <p aria-live="polite">Cargando métricas...</p> : null}

      {state === 'error' ? (
        <p className="submit-error" role="alert">
          {error}
        </p>
      ) : null}

      {state === 'ready' && metrics.length > 0 ? (
        <div className="dashboard-metrics" role="list">
          {metrics.map((metric) => (
            <Link className="dashboard-metric-card" key={metric.key} role="listitem" to={metric.to}>
              <span>{metric.label}</span>
              <strong>{metric.value}</strong>
              <small>{metric.helper}</small>
            </Link>
          ))}
        </div>
      ) : null}

      {state === 'ready' && metrics.length === 0 ? (
        <div className="empty-detail">
          <h3>No hay módulos disponibles para tu perfil</h3>
          <p>Solicitá permisos de acceso a la administración del sistema.</p>
        </div>
      ) : null}

      {quickLinks.length > 0 ? (
        <nav className="quick-link-grid" aria-label="Accesos rápidos">
          {quickLinks.map((link) => (
            <Link className="catalog-home-card" key={link.to} to={link.to}>
              <h3>{link.label}</h3>
            </Link>
          ))}
        </nav>
      ) : null}
    </section>
  );
}
