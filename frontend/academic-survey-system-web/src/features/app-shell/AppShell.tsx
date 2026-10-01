import { useEffect, useMemo, useState, type ReactNode } from 'react';
import { NavLink, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../auth/AuthProvider';
import { useAcademicContext } from '../academic-context/AcademicContextProvider';
import { formatAcademicCycle } from '../academic-catalog/academicCatalogUi';

interface NavItem {
  label: string;
  to: string;
  end?: boolean;
  visible: boolean;
  requiresCycle?: boolean;
}

const pageTitles: Array<{ match: (pathname: string) => boolean; title: string }> = [
  { match: (pathname) => pathname === '/app', title: 'Inicio' },
  { match: (pathname) => pathname.startsWith('/app/users'), title: 'Usuarios' },
  { match: (pathname) => pathname.startsWith('/app/surveys'), title: 'Encuestas' },
  { match: (pathname) => pathname.startsWith('/app/context/results'), title: 'Resultados' },
  { match: (pathname) => pathname.startsWith('/app/context/sessions'), title: 'Sesiones' },
  { match: (pathname) => pathname.startsWith('/app/context/surveys'), title: 'Encuestas' },
  { match: (pathname) => pathname.startsWith('/app/context/teacher-assignments'), title: 'Asignaciones docentes' },
  { match: (pathname) => pathname.startsWith('/app/context/teachers'), title: 'Docentes' },
  { match: (pathname) => pathname.startsWith('/app/context/subjects'), title: 'Materias' },
  { match: (pathname) => pathname.startsWith('/app/context'), title: 'Resumen de carrera' },
  { match: (pathname) => pathname.startsWith('/app/survey-assignments'), title: 'Asignaciones' },
  { match: (pathname) => pathname.startsWith('/app/academic'), title: 'Estructura académica' },
  { match: (pathname) => pathname.startsWith('/app/results'), title: 'Resultados' },
  { match: (pathname) => pathname.startsWith('/app/sessions'), title: 'Sesiones' }
];

export function AppShell({ children }: { children?: ReactNode }) {
  const auth = useAuth();
  const academicContext = useAcademicContext();
  const navigate = useNavigate();
  const location = useLocation();
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const user = auth.user;
  const fullName = user ? `${user.firstName} ${user.lastName}` : '';
  const pageTitle = pageTitles.find((item) => item.match(location.pathname))?.title ?? 'Panel';

  const administrationNavItems = useMemo<NavItem[]>(
    () => [
      {
        label: 'Plantillas de encuestas',
        to: '/app/surveys',
        visible: auth.hasPermission('surveys.templates.read') || auth.hasPermission('surveys.templates.manage')
      },
      {
        label: 'Estructura académica',
        to: '/app/academic',
        visible: auth.hasPermission('academic.catalog.read') || auth.hasPermission('academic.catalog.manage')
      },
      {
        label: 'Usuarios',
        to: '/app/users',
        visible: auth.hasPermission('identity.users.read')
      }
    ],
    [auth]
  );
  const contextualNavItems = useMemo<NavItem[]>(
    () => [
      { label: 'Resumen', to: '/app/context', end: true, visible: true },
      {
        label: 'Materias',
        to: '/app/context/subjects',
        visible: auth.hasPermission('academic.catalog.read') || auth.hasPermission('academic.catalog.manage')
      },
      {
        label: 'Docentes',
        to: '/app/context/teachers',
        visible: auth.hasPermission('academic.catalog.read') || auth.hasPermission('academic.catalog.manage')
      },
      {
        label: 'Asignaciones docentes',
        to: '/app/context/teacher-assignments',
        visible: auth.hasPermission('academic.catalog.read') || auth.hasPermission('academic.catalog.manage'),
        requiresCycle: true
      },
      {
        label: 'Encuestas',
        to: '/app/context/surveys',
        visible: auth.hasPermission('surveys.templates.manage'),
        requiresCycle: true
      },
      {
        label: 'Sesiones',
        to: '/app/context/sessions',
        visible: auth.hasPermission('surveys.sessions.manage'),
        requiresCycle: true
      },
      {
        label: 'Resultados',
        to: '/app/context/results',
        visible: auth.hasPermission('results.read_all') || auth.hasPermission('results.read_career'),
        requiresCycle: true
      }
    ],
    [auth]
  );

  useEffect(() => {
    setIsDrawerOpen(false);
  }, [location.pathname]);

  function handleLogout() {
    academicContext.clearContext();
    auth.logout();
    navigate('/login', { replace: true });
  }

  return (
    <main className="private-shell">
      <button
        className="mobile-overlay"
        aria-label="Cerrar navegación"
        hidden={!isDrawerOpen}
        onClick={() => setIsDrawerOpen(false)}
        type="button"
      />
      <aside className={`app-sidebar ${isDrawerOpen ? 'app-sidebar--open' : ''}`} id="app-sidebar">
        <div className="brand-block">
          <span aria-hidden="true">AS</span>
          <div>
            <strong>Encuestas Académicas</strong>
            <small>Encuestas académicas</small>
          </div>
        </div>

        <nav className="app-nav" aria-label="Navegación privada">
          <div className="nav-section nav-section--home">
            <NavLink end to="/app">
              Inicio
            </NavLink>
          </div>

          <ContextSelectors />

          {academicContext.selectedCareer ? (
            <div className="nav-section">
              <p>Trabajo actual</p>
              <strong className="nav-context-title">{academicContext.selectedCareer.name}</strong>
              {contextualNavItems
                .filter(
                  (item) =>
                    item.visible &&
                    (!item.requiresCycle || Boolean(academicContext.selectedAcademicCycle))
                )
                .map((item) => (
                  <NavLink end={item.end} key={item.to} to={item.to}>
                    {item.label}
                  </NavLink>
                ))}
              {!academicContext.selectedAcademicCycle ? (
                <span className="nav-context-hint">
                  Elegí un ciclo lectivo para habilitar asignaciones docentes, encuestas, sesiones y resultados.
                </span>
              ) : null}
            </div>
          ) : (
            <div className="context-empty-hint">
              <strong>Para empezar</strong>
              <span>
                {academicContext.selectedAcademicUnit
                  ? 'Elegí una carrera para ver materias, docentes, encuestas y resultados.'
                  : 'Elegí una unidad académica, luego una carrera y un ciclo lectivo.'}
              </span>
            </div>
          )}

          <div className="nav-section">
            <p>Administración</p>
            {administrationNavItems
              .filter((item) => item.visible)
              .map((item) => (
                <NavLink end={item.end} key={item.to} to={item.to}>
                  {item.label}
                </NavLink>
              ))}
          </div>
        </nav>

        <div className="user-summary">
          <div className="avatar-token" aria-hidden="true">
            {user ? getUserInitials(user.firstName, user.lastName, user.email) : 'U'}
          </div>
          <div>
            <strong>{fullName}</strong>
            <span>{user?.email}</span>
            <span>{formatRoles(user?.roles ?? [])}</span>
          </div>
          <button className="secondary-button" onClick={handleLogout} type="button">
            Cerrar sesión
          </button>
        </div>
      </aside>

      <section className="app-main">
        <header className="app-topbar">
          <button
            aria-controls="app-sidebar"
            aria-expanded={isDrawerOpen}
            className="menu-button"
            onClick={() => setIsDrawerOpen(true)}
            type="button"
          >
            <span>Menú</span>
          </button>
          <div>
            <p className="eyebrow">Panel de gestión</p>
            <h1>{pageTitle}</h1>
            <ContextBreadcrumb />
          </div>
        </header>

        {children}
      </section>
    </main>
  );
}

function ContextSelectors() {
  const context = useAcademicContext();

  return (
    <div className="academic-context-panel" aria-label="Contexto académico">
      <div className="context-panel-header">
        <div>
          <p>Contexto de trabajo</p>
          <span>Seleccioná dónde querés trabajar.</span>
        </div>
        {context.academicUnitId || context.careerId || context.academicCycleId ? (
          <button className="link-button" onClick={context.clearContext} type="button">
            Limpiar
          </button>
        ) : null}
      </div>
      <label>
        <span>1. Unidad académica</span>
        <select
          className="text-input"
          disabled={context.isLoading}
          onChange={(event) => context.setAcademicUnit(event.target.value)}
          value={context.academicUnitId}
        >
          <option value="">Seleccionar...</option>
          {context.academicUnits.map((unit) => (
            <option key={unit.id} value={unit.id}>
              {unit.name}
            </option>
          ))}
        </select>
      </label>

      <label>
        <span>2. Carrera</span>
        <select
          className="text-input"
          disabled={!context.academicUnitId || context.isLoading}
          onChange={(event) => context.setCareer(event.target.value)}
          value={context.careerId}
        >
          <option value="">Seleccionar...</option>
          {context.careers.map((career) => (
            <option key={career.id} value={career.id}>
              {career.code ? `${career.code} · ${career.name}` : career.name}
            </option>
          ))}
        </select>
      </label>

      <label>
        <span>3. Ciclo lectivo</span>
        <select
          className="text-input"
          disabled={!context.careerId || context.isLoading}
          onChange={(event) => context.setAcademicCycle(event.target.value)}
          value={context.academicCycleId}
        >
          <option value="">Seleccionar...</option>
          {context.academicCycles.map((cycle) => (
            <option key={cycle.id} value={cycle.id}>
              {formatAcademicCycle(cycle)}
            </option>
          ))}
        </select>
      </label>

      {context.error ? <p className="context-error">{context.error}</p> : null}
    </div>
  );
}

function ContextBreadcrumb() {
  const context = useAcademicContext();
  const parts = [
    context.selectedAcademicUnit?.name,
    context.selectedCareer?.name,
    context.selectedAcademicCycle ? formatAcademicCycle(context.selectedAcademicCycle) : null
  ].filter((part): part is string => Boolean(part));

  if (parts.length === 0) {
    return null;
  }

  return <p className="context-breadcrumb">{parts.join(' / ')}</p>;
}

function formatRoles(roles: string[]): string {
  return roles.length > 0 ? roles.join(', ') : 'Sin roles asignados';
}

function getUserInitials(firstName: string, lastName: string, email: string): string {
  const first = firstName.trim()[0];
  const last = lastName.trim()[0];
  return `${first ?? email[0] ?? 'U'}${last ?? ''}`.toUpperCase();
}
