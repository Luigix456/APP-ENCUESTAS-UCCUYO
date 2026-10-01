import { useEffect, useMemo, useState, type ReactNode } from 'react';
import { NavLink, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../auth/AuthProvider';

interface NavItem {
  label: string;
  to: string;
  end?: boolean;
  visible: boolean;
}

const pageTitles: Array<{ match: (pathname: string) => boolean; title: string }> = [
  { match: (pathname) => pathname === '/app', title: 'Inicio' },
  { match: (pathname) => pathname.startsWith('/app/users'), title: 'Usuarios' },
  { match: (pathname) => pathname.startsWith('/app/surveys'), title: 'Encuestas' },
  { match: (pathname) => pathname.startsWith('/app/survey-assignments'), title: 'Asignaciones' },
  { match: (pathname) => pathname.startsWith('/app/academic'), title: 'Catálogo Académico' },
  { match: (pathname) => pathname.startsWith('/app/results'), title: 'Resultados' },
  { match: (pathname) => pathname.startsWith('/app/sessions'), title: 'Sesiones' }
];

export function AppShell({ children }: { children?: ReactNode }) {
  const auth = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const user = auth.user;
  const fullName = user ? `${user.firstName} ${user.lastName}` : '';
  const pageTitle = pageTitles.find((item) => item.match(location.pathname))?.title ?? 'Panel';

  const navItems = useMemo<NavItem[]>(
    () => [
      { label: 'Inicio', to: '/app', end: true, visible: true },
      {
        label: 'Encuestas',
        to: '/app/surveys',
        visible: auth.hasPermission('surveys.templates.read') || auth.hasPermission('surveys.templates.manage')
      },
      {
        label: 'Asignaciones',
        to: '/app/survey-assignments',
        visible: auth.hasPermission('surveys.templates.manage')
      },
      {
        label: 'Catálogo Académico',
        to: '/app/academic',
        visible: auth.hasPermission('academic.catalog.read') || auth.hasPermission('academic.catalog.manage')
      },
      {
        label: 'Resultados',
        to: '/app/results',
        visible: auth.hasPermission('results.read_all') || auth.hasPermission('results.read_career')
      },
      {
        label: 'Sesiones',
        to: '/app/sessions',
        visible: auth.hasPermission('surveys.sessions.manage')
      },
      {
        label: 'Usuarios',
        to: '/app/users',
        visible: auth.hasPermission('identity.users.read') || auth.hasPermission('identity.users.manage')
      }
    ],
    [auth]
  );

  useEffect(() => {
    setIsDrawerOpen(false);
  }, [location.pathname]);

  function handleLogout() {
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
      <aside className={`app-sidebar ${isDrawerOpen ? 'app-sidebar--open' : ''}`}>
        <div className="brand-block">
          <span aria-hidden="true">AS</span>
          <div>
            <strong>AcademicSurveySystem</strong>
            <small>Encuestas académicas</small>
          </div>
        </div>

        <nav className="app-nav" aria-label="Navegación privada">
          {navItems
            .filter((item) => item.visible)
            .map((item) => (
              <NavLink end={item.end} key={item.to} to={item.to}>
                {item.label}
              </NavLink>
            ))}
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
          <button className="menu-button" onClick={() => setIsDrawerOpen(true)} type="button">
            <span>Menu</span>
          </button>
          <div>
            <p className="eyebrow">Panel administrativo</p>
            <h1>{pageTitle}</h1>
          </div>
        </header>

        {children}
      </section>
    </main>
  );
}

function formatRoles(roles: string[]): string {
  return roles.length > 0 ? roles.join(', ') : 'Sin roles asignados';
}

function getUserInitials(firstName: string, lastName: string, email: string): string {
  const first = firstName.trim()[0];
  const last = lastName.trim()[0];
  return `${first ?? email[0] ?? 'U'}${last ?? ''}`.toUpperCase();
}
