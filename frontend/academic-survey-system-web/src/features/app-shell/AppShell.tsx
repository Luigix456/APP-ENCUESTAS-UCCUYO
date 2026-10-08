import { useEffect, useMemo, useState, type ReactNode } from 'react';
import { NavLink, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../auth/AuthProvider';
import { InstitutionBrand } from '../../components/InstitutionBrand';

interface NavItem {
  label: string;
  to: string;
  end?: boolean;
  visible: boolean;
}

const pageTitles: Array<{ match: (pathname: string) => boolean; title: string }> = [
  { match: (pathname) => pathname === '/app', title: 'Inicio' },
  { match: (pathname) => pathname.startsWith('/app/audit'), title: 'Auditoría' },
  { match: (pathname) => pathname.startsWith('/app/users'), title: 'Usuarios' },
  { match: (pathname) => pathname.startsWith('/app/surveys'), title: 'Encuestas' },
  { match: (pathname) => pathname.startsWith('/app/survey-assignments'), title: 'Asignaciones' },
  { match: (pathname) => pathname.startsWith('/app/academic'), title: 'Estructura académica' },
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

  const canReadTemplates = auth.hasPermission('surveys.templates.read') || auth.hasPermission('surveys.templates.manage');
  const canReadCatalog = auth.hasPermission('academic.catalog.read') || auth.hasPermission('academic.catalog.manage');
  const canManageSessions = auth.hasPermission('surveys.sessions.manage');

  const navItems = useMemo<NavItem[]>(
    () => [
      {
        label: 'Unidades académicas',
        to: '/app/academic/units',
        visible: canReadCatalog
      },
      {
        label: 'Plantillas de encuestas',
        to: '/app/surveys',
        visible: canReadTemplates
      },
      {
        label: 'Usuarios',
        to: '/app/users',
        visible: auth.hasPermission('identity.users.read')
      },
      {
        label: 'Auditoría',
        to: '/app/audit',
        visible: auth.hasPermission('audit.read')
      },
      {
        label: 'Sesiones',
        to: '/app/sessions',
        visible: canManageSessions
      }
    ],
    [auth, canManageSessions, canReadCatalog, canReadTemplates]
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
      <aside className={`app-sidebar ${isDrawerOpen ? 'app-sidebar--open' : ''}`} id="app-sidebar">
        <div className="brand-block">
          <InstitutionBrand compact subtitle="Encuestas Académicas" />
        </div>

        <nav className="app-nav" aria-label="Navegación privada">
          {navItems.some((item) => item.visible) ? (
            <div className="nav-section">
              <p>Módulos</p>
              {navItems
                .filter((item) => item.visible)
                .map((item) => (
                  <NavLink end={item.end} key={item.to} to={item.to}>
                    {item.label}
                  </NavLink>
                ))}
            </div>
          ) : null}
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
          </div>
        </header>

        {children}
      </section>
    </main>
  );
}

function formatRoles(roles: string[]): string {
  if (roles.length === 0) {
    return 'Sin roles asignados';
  }

  const labels: Record<string, string> = {
    administrator: 'Administrador',
    surveyor: 'Encuestadora',
    dean: 'Decana',
    career_director: 'Director/a de carrera'
  };

  return roles.map((role) => labels[role] ?? role).join(', ');
}

function getUserInitials(firstName: string, lastName: string, email: string): string {
  const first = firstName.trim()[0];
  const last = lastName.trim()[0];
  return `${first ?? email[0] ?? 'U'}${last ?? ''}`.toUpperCase();
}
